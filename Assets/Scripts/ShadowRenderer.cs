//using System;
//using System.IO;
//using UnityEngine;
//using System.Collections.Generic;
//using UnityEditor;

//[ExecuteInEditMode]
//public class ShadowRenderer : MonoBehaviour
//{
//    public Shader ShadowMapShader;

//    public RenderTexture ShadowMap;
//    public RenderTexture ShadowMapDebug;

//    /*****/

//    private Camera _camera;
//    private Material _shadowMapMaterial;

//    /*****/

//    void OnEnable()
//    {
//        this.hideFlags = HideFlags.None;

//        _camera = GetComponent<Camera>();
//        _camera.depthTextureMode |= DepthTextureMode.Depth;
//        _camera.depthTextureMode |= DepthTextureMode.DepthNormals;

//        if (_shadowMapMaterial == null) _shadowMapMaterial = new Material(ShadowMapShader) { hideFlags = HideFlags.HideAndDontSave };
//    }

//    void OnDisable()
//    {
//        if (_shadowMapMaterial != null) { DestroyImmediate(_shadowMapMaterial); _shadowMapMaterial = null; }
//    }

//    void Update()
//    {
//        _camera.enabled = DisplaySettings.Instance.EnableShadows;
//    }

//    void SetShaderParams()
//    {
//        // Set display settings
//        _shadowMapMaterial.SetFloat("_Scale", DisplaySettings.Instance.Scale);

//        _shadowMapMaterial.SetInt("_EnableBrownianMotion", Convert.ToInt32(DisplaySettings.Instance.EnableBrownianMotion));

//        _shadowMapMaterial.SetInt("_EnableCrossSection", Convert.ToInt32(DisplaySettings.Instance.EnableCrossSection));
//        _shadowMapMaterial.SetVector("_CrossSectionPlane", new Vector4(DisplaySettings.Instance.CrossSectionPlaneNormal.x,
//            DisplaySettings.Instance.CrossSectionPlaneNormal.y, DisplaySettings.Instance.CrossSectionPlaneNormal.z, DisplaySettings.Instance.CrossSectionPlaneDistance));

//        // Set buffers
//        _shadowMapMaterial.SetBuffer("molTypes", ComputeBufferManager.Instance.InstanceTypes);
//        _shadowMapMaterial.SetBuffer("molStates", ComputeBufferManager.Instance.InstanceStates);
//        _shadowMapMaterial.SetBuffer("atomRadii", ComputeBufferManager.Instance.AtomRadii);
//        _shadowMapMaterial.SetBuffer("molColors", ComputeBufferManager.Instance.IngredientColors);
//        _shadowMapMaterial.SetBuffer("molPositions", ComputeBufferManager.Instance.InstancePositions);
//        _shadowMapMaterial.SetBuffer("molRotations", ComputeBufferManager.Instance.InstanceRotations);
//        _shadowMapMaterial.SetBuffer("molAtomCountBuffer", ComputeBufferManager.Instance.IngredientAtomCount);
//        _shadowMapMaterial.SetBuffer("molAtomStartBuffer", ComputeBufferManager.Instance.IngredientAtomStart);
//        _shadowMapMaterial.SetBuffer("atomDataPDBBuffer", ComputeBufferManager.Instance.AtomPositions);
//        _shadowMapMaterial.SetBuffer("_SubInstancesInfo", ComputeBufferManager.Instance.SubInstanceInformations);
//        _shadowMapMaterial.SetBuffer("_ToggleIngredientsBuffer", ComputeBufferManager.Instance.IngredientToggle);
//        _shadowMapMaterial.SetBuffer("_SubInstancesPositions", ComputeBufferManager.Instance.SubInstancesPositions);
//        _shadowMapMaterial.SetBuffer("_IngredientsBoundingSphereRadius", ComputeBufferManager.Instance.IngredientBoundingSphere);
        
//        _shadowMapMaterial.SetMatrix("_ShadowCameraViewMatrix", _camera.worldToCameraMatrix);
//        _shadowMapMaterial.SetMatrix("_ShadowCameraViewProjMatrix", GL.GetGPUProjectionMatrix(_camera.projectionMatrix, false) * _camera.worldToCameraMatrix);
//    }
    
//    void OnPostRender()
//    {
//        SetShaderParams();

//        // Render shadow map
//        Graphics.SetRenderTarget(new[] { ShadowMap.colorBuffer, ShadowMapDebug.colorBuffer }, ShadowMap.depthBuffer);
//        GL.Clear(true, true, new Color(0, 0, 0, 0));

//        Graphics.SetRenderTarget(new[] { ShadowMap.colorBuffer, ShadowMapDebug.colorBuffer }, ShadowMap.depthBuffer);
//        _shadowMapMaterial.SetPass(0);
//        Graphics.DrawProcedural(MeshTopology.Points, SceneManager.Instance.NumSubInstances);
//    }
//}

