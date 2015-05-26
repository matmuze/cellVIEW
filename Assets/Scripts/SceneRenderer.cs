using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

[ExecuteInEditMode]
public class SceneRenderer : MonoBehaviour
{
    public Shader FetchDepthShader;
    public Shader RenderSceneShader;
    public Shader ObjectContourShader;

    public ComputeShader ClearBufferCS;
    public ComputeShader CrossSectionCS;
    public ComputeShader BatchInstancesCS;
    public ComputeShader BrownianMotionCS;
    public ComputeShader FrustrumCullingCS;
    
    public Camera DebugCamera;
    public Camera ShadowCamera;
    public RenderTexture ShadowMap;

    [HideInInspector]
    public Material RenderSceneMaterial;

    /*****/

    private Camera _camera;
    private ComputeBuffer _argBuffer;
    private RenderTexture _depthBuffer;

    private Material _contourMaterial;
    private Material _fetchDepthMaterial;

    /*****/

    private bool _leftMouseDown = false;
    private Vector2 _mousePos = new Vector2();

    /*****/

    void OnEnable()
    {
        this.hideFlags = HideFlags.None;

        _camera = GetComponent<Camera>();
        _camera.depthTextureMode |= DepthTextureMode.Depth;
        _camera.depthTextureMode |= DepthTextureMode.DepthNormals;

        if (RenderSceneMaterial == null) RenderSceneMaterial = new Material(RenderSceneShader) { hideFlags = HideFlags.HideAndDontSave };
        if (_contourMaterial == null) _contourMaterial = new Material(ObjectContourShader) { hideFlags = HideFlags.HideAndDontSave };
        if (_fetchDepthMaterial == null) _fetchDepthMaterial = new Material(FetchDepthShader) { hideFlags = HideFlags.HideAndDontSave };

        if (_argBuffer == null)
        {
            _argBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.DrawIndirect);
            _argBuffer.SetData( new [] { 0, 1, 0, 0 });
        }
    }

    void OnDisable()
    {
        if (RenderSceneMaterial != null) DestroyImmediate(RenderSceneMaterial); 
        if (_contourMaterial != null) DestroyImmediate(_contourMaterial); 
        if (_fetchDepthMaterial != null) DestroyImmediate(_fetchDepthMaterial);
        
        if (_depthBuffer != null)
        {
            _depthBuffer.Release();
            DestroyImmediate(_depthBuffer);
            _depthBuffer = null;
        }

        if (_argBuffer != null)
        {
            _argBuffer.Release();
            _argBuffer = null;
        }
    }

    private void OnGUI()
    {
        // Listen mouse click events
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            _leftMouseDown = true;
            _mousePos = Event.current.mousePosition;
        }
    }

    void SetRenderSceneShaderParams()
    {
        RenderSceneMaterial.SetFloat("_Scale", DisplaySettings.Instance.Scale);
        RenderSceneMaterial.SetVector("_CameraForward", _camera.transform.forward);
        RenderSceneMaterial.SetFloat("_FirstLevelBeingRange", DisplaySettings.Instance.FirstLevelOffset);
        RenderSceneMaterial.SetMatrix("_LodLevelsInfos", Helper.FloatArrayToMatrix4X4(DisplaySettings.Instance.LodLevels));

        // Instances data
        RenderSceneMaterial.SetBuffer("_InstanceTypes", ComputeBufferManager.Instance.InstanceTypes);
        RenderSceneMaterial.SetBuffer("_InstanceStates", ComputeBufferManager.Instance.InstanceStates);
        RenderSceneMaterial.SetBuffer("_InstancePositions", 
            (DisplaySettings.Instance.EnableBrownianMotion) ?
            ComputeBufferManager.Instance.InstanceDisplayPositions : ComputeBufferManager.Instance.InstancePositions);
        
        RenderSceneMaterial.SetBuffer("_InstanceRotations",
            (DisplaySettings.Instance.EnableBrownianMotion) ?
            ComputeBufferManager.Instance.InstanceDisplayRotations : ComputeBufferManager.Instance.InstanceRotations);
        
        RenderSceneMaterial.SetBuffer("_IngredientColors", ComputeBufferManager.Instance.IngredientColors);
        RenderSceneMaterial.SetBuffer("_ProteinAtomPositions", ComputeBufferManager.Instance.ProteinAtomPositions);
        RenderSceneMaterial.SetBuffer("_ProteinClusterPositions", ComputeBufferManager.Instance.ProteinClusterPositions);
        RenderSceneMaterial.SetBuffer("_ProteinSphereBatchInfos", ComputeBufferManager.Instance.ProteinSphereBatchInfos);
        
        // Lipid data
        RenderSceneMaterial.SetBuffer("_LipidAtomPositions", ComputeBufferManager.Instance.LipidAtomPositions);
        RenderSceneMaterial.SetBuffer("_LipidSphereBatchInfos", ComputeBufferManager.Instance.LipidSphereBatchInfos);
        RenderSceneMaterial.SetBuffer("_LipidInstancePositions", ComputeBufferManager.Instance.LipidInstancePositions);

        // Shadow data
        //RenderSceneMaterial.SetInt("_EnableShadows", Convert.ToInt32(DisplaySettings.Instance.EnableShadows));
        //RenderSceneMaterial.SetTexture("_ShadowMap", ShadowMap);
        //RenderSceneMaterial.SetVector("_ShadowCameraForward", ShadowCamera.transform.forward);
        //RenderSceneMaterial.SetVector("_ShadowCameraWorldPos", ShadowCamera.transform.position);
        //RenderSceneMaterial.SetMatrix("_ShadowCameraViewMatrix", ShadowCamera.worldToCameraMatrix);
        //RenderSceneMaterial.SetMatrix("_ShadowCameraProjMatrix", GL.GetGPUProjectionMatrix(ShadowCamera.projectionMatrix, false));
        //RenderSceneMaterial.SetMatrix("_ShadowCameraViewProjMatrix", GL.GetGPUProjectionMatrix(ShadowCamera.projectionMatrix, false) * ShadowCamera.worldToCameraMatrix);
    }

    private void OnPostRender()
    {
        
    }

    [ImageEffectOpaque]
    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        // Return if no instances to draw
        if (SceneManager.Instance.NumProteinInstances == 0 && SceneManager.Instance.NumLipidInstances == 0) { Graphics.Blit(src, dst); return; }
        
        if (_depthBuffer == null || _depthBuffer.width != Screen.width || _depthBuffer.height != Screen.height)
        {
            if (_depthBuffer != null)
            {
                _depthBuffer.Release();
                DestroyImmediate(_depthBuffer);
            }

            _depthBuffer = new RenderTexture(src.width, src.height, 24, RenderTextureFormat.Depth);
            _depthBuffer.Create();
        }

        // Maybe this can go with brownian motion, must compute cost before...
        // Do cross section + filtered visibility
        CrossSectionCS.SetFloat("_Scale", DisplaySettings.Instance.Scale);
        CrossSectionCS.SetInt("_NumLipidInstances", SceneManager.Instance.NumLipidInstances);
        CrossSectionCS.SetInt("_NumProteinInstances", SceneManager.Instance.NumProteinInstances);
        CrossSectionCS.SetInt("_EnableCrossSection", Convert.ToInt32(DisplaySettings.Instance.EnableCrossSection));
        CrossSectionCS.SetVector("_CrossSectionPlane", new Vector4(DisplaySettings.Instance.CrossSectionPlaneNormal.x, DisplaySettings.Instance.CrossSectionPlaneNormal.y, DisplaySettings.Instance.CrossSectionPlaneNormal.z, DisplaySettings.Instance.CrossSectionPlaneDistance));
        CrossSectionCS.SetBuffer(0, "_InstanceStates", ComputeBufferManager.Instance.InstanceStates);
        CrossSectionCS.SetBuffer(0, "_InstancePositions", ComputeBufferManager.Instance.InstancePositions);
        CrossSectionCS.SetBuffer(0, "_IngredientVisibilityFlags", ComputeBufferManager.Instance.IngredientToggleFlags);
        CrossSectionCS.Dispatch(0, (int)Mathf.Ceil(SceneManager.Instance.NumProteinInstances / 8.0f), 1, 1);
        
        // Do lipid cross section
        CrossSectionCS.SetBuffer(1, "_LipidSphereBatchInfos", ComputeBufferManager.Instance.LipidSphereBatchInfos);
        CrossSectionCS.SetBuffer(1, "_LipidInstancePositions", ComputeBufferManager.Instance.LipidInstancePositions);
        CrossSectionCS.Dispatch(1, (int)Mathf.Ceil(SceneManager.Instance.NumLipidInstances / 8.0f), 1, 1);

        // Do Brownian motion
        if (DisplaySettings.Instance.EnableBrownianMotion)
        {
            // Do proteins
            //BrownianMotionCS.SetFloat("_Time", Time.time);
            //BrownianMotionCS.SetBuffer(0, "_InstancePositions", ComputeBufferManager.Instance.InstancePositions);
            //BrownianMotionCS.SetBuffer(0, "_InstanceRotations", ComputeBufferManager.Instance.InstanceRotations);
            //BrownianMotionCS.SetBuffer(0, "_InstanceDisplayPositions", ComputeBufferManager.Instance.InstanceDisplayPositions);
            //BrownianMotionCS.SetBuffer(0, "_InstanceDisplayRotations", ComputeBufferManager.Instance.InstanceDisplayRotations);
            //BrownianMotionCS.Dispatch(0, (int)Mathf.Ceil(SceneManager.Instance.NumInstances / 8.0f), 1, 1);

            // Do lipids
            //BrownianMotionCS.Dispatch(0, (int)Mathf.Ceil(SceneManager.Instance.NumInstances / 8.0f), 1, 1);
        }

        // Frustrum cull proteins
        FrustrumCullingCS.SetFloat("_Scale", DisplaySettings.Instance.Scale);
        FrustrumCullingCS.SetInt("_NumLipidInstances", SceneManager.Instance.NumLipidInstances);
        FrustrumCullingCS.SetInt("_NumProteinInstances", SceneManager.Instance.NumProteinInstances);
        FrustrumCullingCS.SetFloats("_FrustrumPlanes", Helper.FrustrumPlanesAsFloats(_camera));
        FrustrumCullingCS.SetBuffer(0, "_InstanceTypes", ComputeBufferManager.Instance.InstanceTypes);
        FrustrumCullingCS.SetBuffer(0, "_InstanceStates", ComputeBufferManager.Instance.InstanceStates);
        FrustrumCullingCS.SetBuffer(0, "_InstanceCullFlags", ComputeBufferManager.Instance.InstanceCullFlags);
        FrustrumCullingCS.SetBuffer(0, "_InstancePositions", ComputeBufferManager.Instance.InstancePositions);
        FrustrumCullingCS.SetBuffer(0, "_IngredientBoundingSpheres", ComputeBufferManager.Instance.IngredientBoundingSpheres);
        FrustrumCullingCS.Dispatch(0, (int)Mathf.Ceil(SceneManager.Instance.NumProteinInstances / 8.0f), 1, 1);

        // Frustrum cull lipids
        FrustrumCullingCS.SetBuffer(1, "_LipidSphereBatchInfos", ComputeBufferManager.Instance.LipidSphereBatchInfos);
        FrustrumCullingCS.SetBuffer(1, "_LipidInstancePositions", ComputeBufferManager.Instance.LipidInstancePositions);
        FrustrumCullingCS.Dispatch(1, (int)Mathf.Ceil(SceneManager.Instance.NumLipidInstances / 8.0f), 1, 1);

        // Do frustrum culling + occlusion culling // Do not use instance states as this wont be usable by the shadow camera
        if (DisplaySettings.Instance.EnableOcclusionCulling)
        {
            //if (!DisplaySettings.Instance.DebugObjectCulling)
            //    //{
            //    //    Graphics.SetRandomWriteTarget(1, ComputeBufferManager.Instance.SubInstanceCullFlags);
            //    //    Graphics.SetRenderTarget(src.colorBuffer, _depthBuffer.depthBuffer);
            //    //    _renderSceneMaterial.SetInt("_SubInstanceStart", SceneManager.Instance.ProteinsSubInstanceStart);
            //    //    _renderSceneMaterial.SetPass(1);
            //    //    Graphics.DrawProcedural(MeshTopology.Points, SceneManager.Instance.ProteinsSubInstanceCount);
            //    //    Graphics.ClearRandomWriteTargets();
            //    //}
            //}
            //else
            //{
            //    // Clear cull flags buffer
            //    ClearBufferCS.SetInt("_ClearValue", 1);
            //    ClearBufferCS.SetBuffer(0, "_SubInstanceCullFlags", ComputeBufferManager.Instance.SubInstanceCullFlags);
            //    ClearBufferCS.Dispatch(0, (int)Mathf.Ceil((float)SceneManager.Instance.NumSubInstances / 8.0f), 1, 1);
            //}
        }
        
        // Do sphere batching
        BatchInstancesCS.SetFloat("_Scale", DisplaySettings.Instance.Scale);
        BatchInstancesCS.SetInt("_EnableLod", Convert.ToInt32(DisplaySettings.Instance.EnableLod));
        BatchInstancesCS.SetInt("_NumLevels", SceneManager.NumLodLevels);
        BatchInstancesCS.SetInt("_NumInstances", SceneManager.Instance.NumProteinInstances);
        BatchInstancesCS.SetVector("_CameraForward", _camera.transform.forward);
        BatchInstancesCS.SetVector("_CameraPosition", _camera.transform.position);
        BatchInstancesCS.SetFloats("_LodLevelsInfos", DisplaySettings.Instance.LodLevels);
        BatchInstancesCS.SetBuffer(0, "_InstanceTypes", ComputeBufferManager.Instance.InstanceTypes);
        BatchInstancesCS.SetBuffer(0, "_InstanceStates", ComputeBufferManager.Instance.InstanceStates);
        BatchInstancesCS.SetBuffer(0, "_InstanceCullFlags", ComputeBufferManager.Instance.InstanceCullFlags);
        BatchInstancesCS.SetBuffer(0, "_InstancePositions", ComputeBufferManager.Instance.InstancePositions);
        BatchInstancesCS.SetBuffer(0, "_IngredientAtomCount", ComputeBufferManager.Instance.IngredientAtomCount);
        BatchInstancesCS.SetBuffer(0, "_IngredientAtomStart", ComputeBufferManager.Instance.IngredientAtomStart);
        BatchInstancesCS.SetBuffer(0, "_IngredientClusterCount", ComputeBufferManager.Instance.IngredientClusterCount);
        BatchInstancesCS.SetBuffer(0, "_IngredientClusterStart", ComputeBufferManager.Instance.IngredientClusterStart);
        BatchInstancesCS.SetBuffer(0, "_SubInstanceInformations", ComputeBufferManager.Instance.ProteinSphereBatchInfos);
        BatchInstancesCS.Dispatch(0, (int)Mathf.Ceil((float)SceneManager.Instance.NumProteinInstances / 8.0f), 1, 1);
        
        // Count sphere batches
        ComputeBuffer.CopyCount(ComputeBufferManager.Instance.ProteinSphereBatchInfos, _argBuffer, 0);

        // Debug batched instances
        //int[] args = new int[] { 0, 1, 0, 0 };
        //argBuffer.GetData(args);
        //Debug.Log("num batches " + args[0]);

        // This resets the append buffer buffer to 0
        Graphics.SetRandomWriteTarget(1, ComputeBufferManager.Instance.ProteinSphereBatchInfos);
        Graphics.Blit(src, dst);
        Graphics.ClearRandomWriteTargets();
        
        /*** Start rendering routine ***/

        SetRenderSceneShaderParams();

        // Declare temp buffers
        var idBuffer = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
        var colorBuffer = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
        var depthNormalsBuffer = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
        var colorCompositeBuffer = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
        var depthCompositeBuffer = RenderTexture.GetTemporary(src.width, src.height, 24, RenderTextureFormat.Depth);

        Graphics.SetRenderTarget(idBuffer);
        GL.Clear(false, true, new Color(1, 1, 1, 1));
        
        Graphics.SetRenderTarget(colorBuffer.colorBuffer, _depthBuffer.depthBuffer);
        GL.Clear(true, true, new Color(1, 1, 1, 1));

        Graphics.SetRenderTarget(depthNormalsBuffer);
        GL.Clear(true, true, new Color(0.5f, 0.5f, 0, 0));

        // Render scene 
        Graphics.SetRenderTarget(new[] { colorBuffer.colorBuffer, idBuffer.colorBuffer }, _depthBuffer.depthBuffer);
     
        // Draw lipids
        RenderSceneMaterial.SetPass(1);
        Graphics.DrawProcedural(MeshTopology.Points, SceneManager.Instance.NumLipidInstances);

        // Draw proteins
        RenderSceneMaterial.SetPass(0);
        Graphics.DrawProceduralIndirect(MeshTopology.Points, _argBuffer);
        
        // Do edge detection
        _contourMaterial.SetInt("_ContourOptions", DisplaySettings.Instance.ContourOptions);
        _contourMaterial.SetFloat("_ContourStrength", DisplaySettings.Instance.ContourStrength);
        _contourMaterial.SetTexture("_IdTexture", idBuffer);
        Graphics.Blit(colorBuffer, colorCompositeBuffer, _contourMaterial, 0);
        Graphics.Blit(colorCompositeBuffer, colorBuffer);

        // Do final compositing with current camera textures
        _fetchDepthMaterial.SetTexture("_ColorTexture", colorBuffer);
        _fetchDepthMaterial.SetTexture("_DepthTexture", _depthBuffer);
        Graphics.SetRenderTarget(colorCompositeBuffer.colorBuffer, depthCompositeBuffer.depthBuffer);
        GL.Clear(true, true, new Color(1, 1, 1, 1));
        Graphics.Blit(src, _fetchDepthMaterial, 1);

        // Blit final color buffer to dst buffer
        Graphics.Blit(colorCompositeBuffer, dst);
        
        // Set final depth buffer to global depth
        Shader.SetGlobalTexture("_CameraDepthTexture", depthCompositeBuffer);
        Shader.SetGlobalTexture("_CameraDepthNormalsTexture ", depthNormalsBuffer); // It is important to set this otherwise AO will show ghosts

        // Do object picking from IdBuffer
        if (_leftMouseDown)
        {
            var idTexture2D = new Texture2D(src.width, src.height, TextureFormat.ARGB32, false);

            RenderTexture.active = idBuffer;
            idTexture2D.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            idTexture2D.Apply();

            SceneManager.Instance.SetSelectedInstance(Helper.GetIdFromColor(idTexture2D.GetPixel((int)_mousePos.x, src.height - (int)_mousePos.y)));

            DestroyImmediate(idTexture2D);
            _leftMouseDown = false;
        }

        //// Do occlusion culling 
        //if (DisplaySettings.Instance.EnableOcclusionCulling)
        //{
        //    //if (!DisplaySettings.Instance.DebugObjectCulling)
        //    //{
        //    //    // Clear cull flags buffer
        //    //    ClearBufferCS.SetInt("_ClearValue", 0);
        //    //    ClearBufferCS.SetBuffer(0, "_SubInstanceCullFlags", ComputeBufferManager.Instance.SubInstanceCullFlags);
        //    //    ClearBufferCS.Dispatch(0, (int)Mathf.Ceil((float)SceneManager.Instance.NumSubInstances / 8.0f), 1, 1);

        //    //    Graphics.SetRandomWriteTarget(1, ComputeBufferManager.Instance.SubInstanceCullFlags);
        //    //    Graphics.SetRenderTarget(src.colorBuffer, _depthBuffer.depthBuffer);
        //    //    _renderSceneMaterial.SetInt("_SubInstanceStart", SceneManager.Instance.ProteinsSubInstanceStart);
        //    //    _renderSceneMaterial.SetPass(1);
        //    //    Graphics.DrawProcedural(MeshTopology.Points, SceneManager.Instance.ProteinsSubInstanceCount);
        //    //    Graphics.ClearRandomWriteTargets();
        //    //}
        //}

        // Release temp buffers
        RenderTexture.ReleaseTemporary(idBuffer);
        RenderTexture.ReleaseTemporary(colorBuffer);
        RenderTexture.ReleaseTemporary(depthNormalsBuffer);
        RenderTexture.ReleaseTemporary(colorCompositeBuffer);
        RenderTexture.ReleaseTemporary(depthCompositeBuffer);
    }
}

