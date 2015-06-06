using UnityEngine;

[ExecuteInEditMode]
public class ShadowRenderer : MonoBehaviour
{
    public Camera MainCamera;
    public RenderTexture ShadowMap;

    /*****/

    private Camera _camera;

    /*****/

    void OnEnable()
    {
        this.hideFlags = HideFlags.None;

        _camera = GetComponent<Camera>();
        _camera.depthTextureMode |= DepthTextureMode.Depth;
        _camera.depthTextureMode |= DepthTextureMode.DepthNormals;
    }

    void Update()
    {
        _camera.enabled = DisplaySettings.Instance.EnableShadows;
    }

    void OnPostRender()
    {
        //var material = MainCamera.GetComponent<SceneRenderer>().RenderSceneMaterial;
        //if (material == null) return;

        //// Set frustrum planes 
        //var planes = GeometryUtility.CalculateFrustumPlanes(_camera);
        //for (int i = 0; i < planes.Length; i++) material.SetVector("_FrustrumPlane_" + i, new Vector4(planes[i].normal.x, planes[i].normal.y, planes[i].normal.z, planes[i].distance));

        //// Clear shadow map
        //Graphics.SetRenderTarget(ShadowMap);
        //GL.Clear(true, true, new Color(0, 0, 0, 0));

        //// Render shadow map
        //Graphics.SetRenderTarget(ShadowMap);
        //material.SetPass(2);
        //Graphics.DrawProcedural(MeshTopology.Points, SceneManager.Instance.NumSubInstances);
    }
}

