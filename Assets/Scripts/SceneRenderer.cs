using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

[ExecuteInEditMode]
public class SceneRenderer : MonoBehaviour
{
    public Shader RenderDnaShader;
    public Shader FetchDepthShader;

    public ComputeShader DistanceConstraintsCS;
    /*****/

    private Camera _camera;
    private RenderTexture _depthBuffer;

    private Material _renderDnaMaterial;
    private Material _compositeMaterial;

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

        if (_renderDnaMaterial == null) _renderDnaMaterial = new Material(RenderDnaShader) { hideFlags = HideFlags.HideAndDontSave };
        if (_compositeMaterial == null) _compositeMaterial = new Material(FetchDepthShader) { hideFlags = HideFlags.HideAndDontSave };
    }

    void OnDisable()
    {
        if (_renderDnaMaterial != null) DestroyImmediate(_renderDnaMaterial); 
        if (_compositeMaterial != null) DestroyImmediate(_compositeMaterial);
        
        if (_depthBuffer != null)
        {
            _depthBuffer.Release();
            DestroyImmediate(_depthBuffer);
            _depthBuffer = null;
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

    private void OnPostRender()
    {
        if (!DisplaySettings.Instance.EnableDNAConstraints) return;

        int numSegments = SceneManager.Instance.NumDnaControlPoints - 1;
        int numSegmentPairs1 = (int)Mathf.Ceil(numSegments / 2.0f);
        int numSegmentPairs2 = (int)Mathf.Ceil(numSegments / 4.0f);

        DistanceConstraintsCS.SetFloat("_DistanceMin", DisplaySettings.Instance.AngularConstraint);
        DistanceConstraintsCS.SetFloat("_DistanceMax", DisplaySettings.Instance.DistanceContraint);
        DistanceConstraintsCS.SetInt("_NumControlPoints", SceneManager.Instance.NumDnaControlPoints);

        // Do distance constraints
        DistanceConstraintsCS.SetInt("_Offset", 0);
        DistanceConstraintsCS.SetBuffer(0, "_DnaControlPoints", ComputeBufferManager.Instance.DnaControlPoints);
        DistanceConstraintsCS.Dispatch(0, (int)Mathf.Ceil(numSegmentPairs1 / 16.0f), 1, 1);

        DistanceConstraintsCS.SetInt("_Offset", 1);
        DistanceConstraintsCS.SetBuffer(0, "_DnaControlPoints", ComputeBufferManager.Instance.DnaControlPoints);
        DistanceConstraintsCS.Dispatch(0, (int)Mathf.Ceil(numSegmentPairs1 / 16.0f), 1, 1);

        // Do angular constraints
        DistanceConstraintsCS.SetInt("_Offset", 0);
        DistanceConstraintsCS.SetBuffer(1, "_DnaControlPoints", ComputeBufferManager.Instance.DnaControlPoints);
        DistanceConstraintsCS.Dispatch(1, (int)Mathf.Ceil(numSegmentPairs2 / 16.0f), 1, 1);

        DistanceConstraintsCS.SetInt("_Offset", 1);
        DistanceConstraintsCS.SetBuffer(1, "_DnaControlPoints", ComputeBufferManager.Instance.DnaControlPoints);
        DistanceConstraintsCS.Dispatch(1, (int)Mathf.Ceil(numSegmentPairs2 / 16.0f), 1, 1);

        DistanceConstraintsCS.SetInt("_Offset", 2);
        DistanceConstraintsCS.SetBuffer(1, "_DnaControlPoints", ComputeBufferManager.Instance.DnaControlPoints);
        DistanceConstraintsCS.Dispatch(1, (int)Mathf.Ceil(numSegmentPairs2 / 16.0f), 1, 1);

        DistanceConstraintsCS.SetInt("_Offset", 3);
        DistanceConstraintsCS.SetBuffer(1, "_DnaControlPoints", ComputeBufferManager.Instance.DnaControlPoints);
        DistanceConstraintsCS.Dispatch(1, (int)Mathf.Ceil(numSegmentPairs2 / 16.0f), 1, 1);
    }

    [ImageEffectOpaque]
    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        // Return if no instances to draw
        if (SceneManager.Instance.NumDnaControlPoints == 0)
        {
            Graphics.Blit(src, dst); return;
        }
        
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
        
        ///*** Start rendering routine ***/

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

        // Draw DNA
        _renderDnaMaterial.SetInt("_NumSteps", DisplaySettings.Instance.NumStepsPerSegment);
        _renderDnaMaterial.SetInt("_NumSegments", SceneManager.Instance.NumDnaControlPoints - 1);
        _renderDnaMaterial.SetInt("_EnableTwist", Convert.ToInt32(DisplaySettings.Instance.EnableTwist));
        
        _renderDnaMaterial.SetFloat("_Scale", DisplaySettings.Instance.Scale);
        _renderDnaMaterial.SetFloat("_SegmentLength", DisplaySettings.Instance.DistanceContraint);
        _renderDnaMaterial.SetFloat("_TwistFactor", DisplaySettings.Instance.TwistFactor);
        _renderDnaMaterial.SetBuffer("_DnaAtoms", ComputeBufferManager.Instance.DnaAtoms);
        _renderDnaMaterial.SetBuffer("_DnaControlPoints", ComputeBufferManager.Instance.DnaControlPoints);
        _renderDnaMaterial.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Points, SceneManager.Instance.NumDnaSegments - 2); // Do not draw first and last segments

        // Do final compositing with current camera textures
        _compositeMaterial.SetTexture("_ColorTexture", colorBuffer);
        _compositeMaterial.SetTexture("_DepthTexture", _depthBuffer);
        Graphics.SetRenderTarget(colorCompositeBuffer.colorBuffer, depthCompositeBuffer.depthBuffer);
        GL.Clear(true, true, new Color(1, 1, 1, 1));
        Graphics.Blit(src, _compositeMaterial, 1);

        // Blit final color buffer to dst buffer
        Graphics.Blit(colorCompositeBuffer, dst);

        // Set final depth buffer to global depth
        Shader.SetGlobalTexture("_CameraDepthTexture", depthCompositeBuffer);
        Shader.SetGlobalTexture("_CameraDepthNormalsTexture ", depthNormalsBuffer); // It is important to set this otherwise AO will show ghosts

        //// Do object picking from IdBuffer
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
        RenderTexture.ReleaseTemporary(depthNormalsBuffer);
        RenderTexture.ReleaseTemporary(colorCompositeBuffer);
        RenderTexture.ReleaseTemporary(depthCompositeBuffer);
    }
}

