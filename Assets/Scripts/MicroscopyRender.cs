using System;
using UnityEngine;

[ExecuteInEditMode]
public class MicroscopyRender : MonoBehaviour
{
    public Camera MainCamera;
    public RenderTexture MicroscopyTexture;

    /*****/

    private Camera _camera;
    private Material _renderProteinsMaterial;

    /*****/

    void OnEnable()
    {
        this.hideFlags = HideFlags.None;

        _camera = GetComponent<Camera>();
        _camera.depthTextureMode |= DepthTextureMode.Depth;
        _camera.depthTextureMode |= DepthTextureMode.DepthNormals;

        _renderProteinsMaterial = MainCamera.GetComponent<SceneRenderer>().RenderProteinsMaterial;
    }

    

    void SetShaderParams()
    {
        // Protein params
        //_renderProteinsMaterial.SetInt("_EnableLod", Convert.ToInt32(DisplaySettings.Instance.EnableLod));
        //_renderProteinsMaterial.SetFloat("_Scale", DisplaySettings.Instance.Scale);
        //_renderProteinsMaterial.SetFloat("_FirstLevelBeingRange", DisplaySettings.Instance.FirstLevelOffset);
        //_renderProteinsMaterial.SetVector("_CameraForward", _camera.transform.forward);
        //_renderProteinsMaterial.SetMatrix("_LodLevelsInfos", Helper.FloatArrayToMatrix4X4(DisplaySettings.Instance.LodLevels));

        //_renderProteinsMaterial.SetBuffer("_ProteinInstanceInfo", ComputeBufferManager.Instance.ProteinInstanceInfos);
        //_renderProteinsMaterial.SetBuffer("_ProteinInstancePositions",
        //    (DisplaySettings.Instance.EnableBrownianMotion) ?
        //    ComputeBufferManager.Instance.InstanceDisplayPositions : ComputeBufferManager.Instance.ProteinInstancePositions);

        //_renderProteinsMaterial.SetBuffer("_ProteinInstanceRotations",
        //    (DisplaySettings.Instance.EnableBrownianMotion) ?
        //    ComputeBufferManager.Instance.InstanceDisplayRotations : ComputeBufferManager.Instance.ProteinInstanceRotations);

        //_renderProteinsMaterial.SetBuffer("_IngredientColors", ComputeBufferManager.Instance.ProteinColors);
        //_renderProteinsMaterial.SetBuffer("_ProteinAtomPositions", ComputeBufferManager.Instance.ProteinAtomPositions);
        //_renderProteinsMaterial.SetBuffer("_ProteinClusterPositions", ComputeBufferManager.Instance.ProteinClusterPositions);
        //_renderProteinsMaterial.SetBuffer("_ProteinSphereBatchInfos", ComputeBufferManager.Instance.ProteinSphereBatchInfos);
    }

    void Update()
    {
        _camera.transform.position = MainCamera.transform.position;
        _camera.transform.rotation = MainCamera.transform.rotation;
    }

    void OnPostRender()
    {
        if (_renderProteinsMaterial == null) return;

        // Clear shadow map
        Graphics.SetRenderTarget(MicroscopyTexture);
        GL.Clear(true, true, new Color(0, 0, 0, 0));

        // Render shadow map
        Graphics.SetRenderTarget(MicroscopyTexture);
        _renderProteinsMaterial.SetPass(1);
        
        Graphics.DrawProcedural(MeshTopology.Points, SceneManager.Instance.NumProteinInstances);
    }
}

