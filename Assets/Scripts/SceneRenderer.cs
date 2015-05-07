using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

[ExecuteInEditMode]
public class SceneRenderer : MonoBehaviour
{
    public Shader RenderSceneShader;
    public Shader ContourShader;
    public Shader GetUnityBuffersShader;
    public ComputeShader ClearBuffer;

    public Camera ShadowCamera;
    public RenderTexture ShadowMap;

    /*****/

    private Camera _camera;

    private Material _renderSceneMaterial;
    private Material _contourMaterial;
    private Material _getUnityBuffersMaterial;

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

        if (_renderSceneMaterial == null) _renderSceneMaterial = new Material(RenderSceneShader) { hideFlags = HideFlags.HideAndDontSave };
        if (_contourMaterial == null) _contourMaterial = new Material(ContourShader) { hideFlags = HideFlags.HideAndDontSave };
        if (_getUnityBuffersMaterial == null) _getUnityBuffersMaterial = new Material(GetUnityBuffersShader) { hideFlags = HideFlags.HideAndDontSave };
    }

    void OnDisable()
    {
        if (_renderSceneMaterial != null) DestroyImmediate(_renderSceneMaterial); 
        if (_contourMaterial != null) DestroyImmediate(_contourMaterial); 
        if (_getUnityBuffersMaterial != null) DestroyImmediate(_getUnityBuffersMaterial); 
        if (_depthBuffer != null) _depthBuffer.Release();
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

    void SetShaderParams()
    {
        // Basic settings
        _renderSceneMaterial.SetFloat("_Scale", DisplaySettings.Instance.Scale);
        _renderSceneMaterial.SetInt("_EnableBrownianMotion", Convert.ToInt32(DisplaySettings.Instance.EnableBrownianMotion));

        // Contour data
        _contourMaterial.SetInt("_ContourOptions", DisplaySettings.Instance.ContourOptions);
        _contourMaterial.SetFloat("_ContourStrength", DisplaySettings.Instance.ContourStrength);

        // Cross section data
        _renderSceneMaterial.SetInt("_EnableCrossSection", Convert.ToInt32(DisplaySettings.Instance.EnableCrossSection));
        _renderSceneMaterial.SetVector("_CrossSectionPlane", new Vector4(DisplaySettings.Instance.CrossSectionPlaneNormal.x,
            DisplaySettings.Instance.CrossSectionPlaneNormal.y, DisplaySettings.Instance.CrossSectionPlaneNormal.z, DisplaySettings.Instance.CrossSectionPlaneDistance));

        // Lod data
        _renderSceneMaterial.SetInt("_EnableLod", Convert.ToInt32(DisplaySettings.Instance.EnableLod));
        _renderSceneMaterial.SetFloat("_DistanceLod0", DisplaySettings.Instance.DistanceLod0);
        _renderSceneMaterial.SetFloat("_DistanceLod1", DisplaySettings.Instance.DistanceLod1);
        _renderSceneMaterial.SetFloat("_MaxAtomRadiusLod0", DisplaySettings.Instance.MaxAtomRadiusLod0);
        _renderSceneMaterial.SetFloat("_MinAtomRadiusLod1", DisplaySettings.Instance.MinAtomRadiusLod1);
        _renderSceneMaterial.SetFloat("_DecimationFactorLod0", DisplaySettings.Instance.DecimationFactorLod0);
        _renderSceneMaterial.SetFloat("_DecimationFactorLod1", DisplaySettings.Instance.DecimationFactorLod1);

        // Set frustrum planes 
        var planes = GeometryUtility.CalculateFrustumPlanes(this.GetComponent<Camera>());
        for (int i = 0; i < planes.Length; i++) _renderSceneMaterial.SetVector("_FrustrumPlane_" + i, new Vector4(planes[i].normal.x, planes[i].normal.y, planes[i].normal.z, planes[i].distance));
        
        // Atom data
        _renderSceneMaterial.SetBuffer("_AtomRadii", ComputeBufferManager.Instance.AtomRadii);
        _renderSceneMaterial.SetBuffer("_AtomPositions", ComputeBufferManager.Instance.AtomPositions);// Instances data
        _renderSceneMaterial.SetBuffer("_InstanceTypes", ComputeBufferManager.Instance.InstanceTypes);
        _renderSceneMaterial.SetBuffer("_InstanceStates", ComputeBufferManager.Instance.InstanceStates);
        _renderSceneMaterial.SetBuffer("_SubInstanceCullFlags", ComputeBufferManager.Instance.SubInstanceCullFlags);
        _renderSceneMaterial.SetBuffer("_SubInstanceInformations", ComputeBufferManager.Instance.SubInstanceInformations);

        _renderSceneMaterial.SetBuffer("_InstancePositions", 
            (DisplaySettings.Instance.EnableBrownianMotion) ?
            ComputeBufferManager.Instance.InstanceDisplayPositions : ComputeBufferManager.Instance.InstancePositions);
        
        _renderSceneMaterial.SetBuffer("_InstanceRotations",
            (DisplaySettings.Instance.EnableBrownianMotion) ?
            ComputeBufferManager.Instance.InstanceDisplayRotations : ComputeBufferManager.Instance.InstanceRotations);

        // Ingredients data
        _renderSceneMaterial.SetBuffer("_IngredientColors", ComputeBufferManager.Instance.IngredientColors);
        _renderSceneMaterial.SetBuffer("_IngredientToggle", ComputeBufferManager.Instance.IngredientToggleFlags);
        _renderSceneMaterial.SetBuffer("_IngredientAtomCount", ComputeBufferManager.Instance.IngredientAtomCount);
        _renderSceneMaterial.SetBuffer("_IngredientAtomStart", ComputeBufferManager.Instance.IngredientAtomStart);
        _renderSceneMaterial.SetBuffer("_IngredientBoundingSphere", ComputeBufferManager.Instance.IngredientBoundingSpheres);
        
        // Shadow data
        _renderSceneMaterial.SetInt("_EnableShadows", Convert.ToInt32(DisplaySettings.Instance.EnableShadows));
        _renderSceneMaterial.SetTexture("_ShadowMap", ShadowMap);
        _renderSceneMaterial.SetVector("_ShadowCameraWorldPos", ShadowCamera.transform.position);
        _renderSceneMaterial.SetVector("_ShadowCameraForward", ShadowCamera.transform.forward);
        _renderSceneMaterial.SetMatrix("_ShadowCameraViewMatrix", ShadowCamera.worldToCameraMatrix);
        _renderSceneMaterial.SetMatrix("_ShadowCameraViewProjMatrix", GL.GetGPUProjectionMatrix(ShadowCamera.projectionMatrix, false) * ShadowCamera.worldToCameraMatrix);
    }

    private RenderTexture _depthBuffer;

    [ImageEffectOpaque]
    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        // Return if no instances to draw
        if (SceneManager.Instance.NumInstances == 0) { Graphics.Blit(src, dst); return; }
        
        if (_depthBuffer == null || _depthBuffer.width != Screen.width || _depthBuffer.height != Screen.height)
        {
            if (_depthBuffer != null)
            {
                _depthBuffer.Release();
                DestroyImmediate(_depthBuffer);
            }

            _depthBuffer = new RenderTexture(src.width, src.height, 32, RenderTextureFormat.Depth);
            _depthBuffer.Create(); 
        }

        SetShaderParams();

        // Declare temp buffers
        var idBuffer = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
        var colorBuffer = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
        //var depthBuffer = RenderTexture.GetTemporary(src.width, src.height, 32, RenderTextureFormat.Depth, RenderTextureReadWrite.Default, 1);

        Graphics.SetRenderTarget(idBuffer);
        GL.Clear(true, true, new Color(1, 1, 1, 1));

        Graphics.SetRenderTarget(colorBuffer);
        GL.Clear(true, true, new Color(0.15f, 0.15f, 0.15f, 1));
        
        Graphics.SetRenderTarget(src.colorBuffer, _depthBuffer.depthBuffer);
        GL.Clear(true, false, new Color(1, 1, 1, 1));

        // Render scene 
        Graphics.SetRenderTarget(new[] { colorBuffer.colorBuffer, idBuffer.colorBuffer }, _depthBuffer.depthBuffer);
        
        // Draw membrane
        _renderSceneMaterial.SetInt("_EnableObjectCulling", 0); // Do not cull the membrane, its too big and artifacts a simply too visible
        _renderSceneMaterial.SetInt("_SubInstanceStart", SceneManager.Instance.MembraneSubInstanceStart);
        _renderSceneMaterial.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Points, SceneManager.Instance.MembraneSubInstanceCount);

        // Draw proteins
        _renderSceneMaterial.SetInt("_EnableObjectCulling", 1);
        _renderSceneMaterial.SetInt("_SubInstanceStart", SceneManager.Instance.ProteinsSubInstanceStart);
        _renderSceneMaterial.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Points, SceneManager.Instance.ProteinsSubInstanceCount);
        
        // Do edge detection
        _contourMaterial.SetTexture("_IdTexture", idBuffer);
        Graphics.Blit(colorBuffer, dst, _contourMaterial, 0);

        // Set unity depth & normal buffer for post-processing
        Shader.SetGlobalTexture("_CameraDepthTexture", _depthBuffer);
        
        // Do Object picking from IdBuffer
        //if (_leftMouseDown)
        //{
        //    var idTexture2D = new Texture2D(src.width, src.height, TextureFormat.ARGB32, false);

        //    RenderTexture.active = idBuffer;
        //    idTexture2D.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
        //    idTexture2D.Apply();

        //    SceneManager.Instance.SetSelectedInstance(Helper.GetIdFromColor(idTexture2D.GetPixel((int)_mousePos.x, src.height - (int)_mousePos.y)));

        //    DestroyImmediate(idTexture2D);
        //    _leftMouseDown = false;
        //}
        
        // Release temp buffers
        RenderTexture.ReleaseTemporary(idBuffer);
        RenderTexture.ReleaseTemporary(colorBuffer);
        //RenderTexture.ReleaseTemporary(_depthBuffer);

        // ***** Do object culling ***** //
        if (DisplaySettings.Instance.EnableObjectCulling)
        {
            if (!DisplaySettings.Instance.DebugObjectCulling)
            {
                // Clear cull flags buffer
                ClearBuffer.SetInt("_ClearValue", 0);
                ClearBuffer.SetBuffer(0, "_SubInstanceCullFlags", ComputeBufferManager.Instance.SubInstanceCullFlags);
                ClearBuffer.Dispatch(0, SceneManager.Instance.NumSubInstances, 1, 1);

                Graphics.SetRandomWriteTarget(1, ComputeBufferManager.Instance.SubInstanceCullFlags);
                Graphics.SetRenderTarget(src.colorBuffer, _depthBuffer.depthBuffer);
                _renderSceneMaterial.SetInt("_SubInstanceStart", SceneManager.Instance.ProteinsSubInstanceStart);
                _renderSceneMaterial.SetPass(1);
                Graphics.DrawProcedural(MeshTopology.Points, SceneManager.Instance.ProteinsSubInstanceCount);
                Graphics.ClearRandomWriteTargets();
            }
        }
        else
        {
            // Clear cull flags buffer
            ClearBuffer.SetInt("_ClearValue", 1);
            ClearBuffer.SetBuffer(0, "_SubInstanceCullFlags", ComputeBufferManager.Instance.SubInstanceCullFlags);
            ClearBuffer.Dispatch(0, SceneManager.Instance.NumSubInstances, 1, 1);
        }
    }
}

