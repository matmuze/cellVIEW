using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

[ExecuteInEditMode]
public class SceneRenderer : MonoBehaviour
{
    public Shader MoleculeShader;
    public Shader ContourShader;
    public Shader GetUnityBuffersShader;

    /*****/

    private Camera _camera;

    private Material _moleculeMaterial;
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

        if (_moleculeMaterial == null) _moleculeMaterial = new Material(MoleculeShader) { hideFlags = HideFlags.HideAndDontSave };
        if (_contourMaterial == null) _contourMaterial = new Material(ContourShader) { hideFlags = HideFlags.HideAndDontSave };
        if (_getUnityBuffersMaterial == null) _getUnityBuffersMaterial = new Material(GetUnityBuffersShader) { hideFlags = HideFlags.HideAndDontSave };
    }

    void OnDisable()
    {
        if (_moleculeMaterial != null) { DestroyImmediate(_moleculeMaterial); _moleculeMaterial = null; }
        if (_contourMaterial != null) { DestroyImmediate(_contourMaterial); _contourMaterial = null; }
        if (_getUnityBuffersMaterial != null) { DestroyImmediate(_getUnityBuffersMaterial); _getUnityBuffersMaterial = null; }
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
        // Set display settings
        _moleculeMaterial.SetFloat("_Scale", DisplaySettings.Instance.Scale);

        _moleculeMaterial.SetInt("_EnableBrownianMotion", Convert.ToInt32(DisplaySettings.Instance.EnableBrownianMotion));

        _moleculeMaterial.SetInt("_EnableCrossSection", Convert.ToInt32(DisplaySettings.Instance.EnableCrossSection));
        _moleculeMaterial.SetVector("_CrossSectionPlane", new Vector4(DisplaySettings.Instance.CrossSectionPlaneNormal.x,
            DisplaySettings.Instance.CrossSectionPlaneNormal.y, DisplaySettings.Instance.CrossSectionPlaneNormal.z, DisplaySettings.Instance.CrossSectionPlaneDistance));

        _moleculeMaterial.SetInt("_EnableLod", Convert.ToInt32(DisplaySettings.Instance.EnableLod));
        _moleculeMaterial.SetFloat("_DistanceLod0", DisplaySettings.Instance.DistanceLod0);
        _moleculeMaterial.SetFloat("_DistanceLod1", DisplaySettings.Instance.DistanceLod1);
        _moleculeMaterial.SetFloat("_MaxAtomRadiusLod0", DisplaySettings.Instance.MaxAtomRadiusLod0);
        _moleculeMaterial.SetFloat("_MinAtomRadiusLod1", DisplaySettings.Instance.MinAtomRadiusLod1);
        _moleculeMaterial.SetFloat("_DecimationFactorLod0", DisplaySettings.Instance.DecimationFactorLod0);
        _moleculeMaterial.SetFloat("_DecimationFactorLod1", DisplaySettings.Instance.DecimationFactorLod1);

        // Set frustrum planes 
        var planes = GeometryUtility.CalculateFrustumPlanes(this.GetComponent<Camera>());
        for (int i = 0; i < planes.Length; i++) _moleculeMaterial.SetVector("_FrustrumPlane_" + i, new Vector4(planes[i].normal.x, planes[i].normal.y, planes[i].normal.z, planes[i].distance));

        // Set buffers
        _moleculeMaterial.SetBuffer("molTypes", ComputeBufferManager.Instance.InstancesTypes);
        _moleculeMaterial.SetBuffer("molStates", ComputeBufferManager.Instance.InstancesStates);
        _moleculeMaterial.SetBuffer("atomRadii", ComputeBufferManager.Instance.AtomRadiiBuffer);
        _moleculeMaterial.SetBuffer("molColors", ComputeBufferManager.Instance.IngredientsColors);
        _moleculeMaterial.SetBuffer("molPositions", ComputeBufferManager.Instance.InstancesPositions);
        _moleculeMaterial.SetBuffer("molRotations", ComputeBufferManager.Instance.InstancesRotations);
        _moleculeMaterial.SetBuffer("molAtomCountBuffer", ComputeBufferManager.Instance.IngredientsAtomCount);
        _moleculeMaterial.SetBuffer("molAtomStartBuffer", ComputeBufferManager.Instance.IngredientsAtomStart);
        _moleculeMaterial.SetBuffer("atomDataPDBBuffer", ComputeBufferManager.Instance.IngredientsAtomPdbData);
        _moleculeMaterial.SetBuffer("_SubInstancesInfo", ComputeBufferManager.Instance.SubInstancesInformations);
        _moleculeMaterial.SetBuffer("_ToggleIngredientsBuffer", ComputeBufferManager.Instance.IngredientsToggle);
        _moleculeMaterial.SetBuffer("_SubInstancesPositions", ComputeBufferManager.Instance.SubInstancesPositions);
        _moleculeMaterial.SetBuffer("_IngredientsBoundingSphereRadius", ComputeBufferManager.Instance.IngredientsBoundingSphereRadius);

        _contourMaterial.SetInt("_ContourOptions", DisplaySettings.Instance.ContourOptions);
        _contourMaterial.SetFloat("_ContourStrength", DisplaySettings.Instance.ContourStrength);
    }

    [ImageEffectOpaque]
    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        // Return if no instances to draw
        if (SceneManager.Instance.NumInstances == 0) { Graphics.Blit(src, dst); return; }

        SetShaderParams();
        
        // Declare temp buffers
        var idBuffer = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
        var colorBuffer = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
        var depthBuffer = RenderTexture.GetTemporary(src.width, src.height, 32, RenderTextureFormat.Depth, RenderTextureReadWrite.Default, 1);
        var depthNormalsBuffer = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
        
        Graphics.SetRenderTarget(idBuffer);
        GL.Clear(true, true, new Color(1, 1, 1, 1));

        Graphics.SetRenderTarget(new[] { colorBuffer.colorBuffer, depthNormalsBuffer.colorBuffer }, depthBuffer.depthBuffer); // Fetch unity buffers
        Graphics.Blit(src, _getUnityBuffersMaterial);

        // Render scene
        Graphics.SetRenderTarget(new[] { colorBuffer.colorBuffer, depthNormalsBuffer.colorBuffer, idBuffer.colorBuffer }, depthBuffer.depthBuffer);
        _moleculeMaterial.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Points, SceneManager.Instance.NumSubInstances);

        // Do edge detection
        _contourMaterial.SetTexture("_IdTexture", idBuffer);
        Graphics.Blit(colorBuffer, dst, _contourMaterial, 0);

        // Set new unity depth & normal buffer for post-processing
        Shader.SetGlobalTexture("_CameraDepthTexture", depthBuffer);
        Shader.SetGlobalTexture("_CameraDepthNormalsTexture", depthNormalsBuffer);

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
        
        // Release temp buffers
        RenderTexture.ReleaseTemporary(idBuffer);
        RenderTexture.ReleaseTemporary(colorBuffer);
        RenderTexture.ReleaseTemporary(depthBuffer);
        RenderTexture.ReleaseTemporary(depthNormalsBuffer);
        
        //Graphics.Blit(src,dst);
    }
}

