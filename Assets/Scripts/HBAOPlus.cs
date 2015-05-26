using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class HBAOPlus : MonoBehaviour
{
    public enum BlurRadiusMode
    {
        BLUR_RADIUS_2,
        BLUR_RADIUS_4,
        BLUR_RADIUS_8,
    }

    [SerializeField]
    private Shader fetchDepthShader; // Use a dummy shader to explicitly binds the _CameraDepthTexture SRV to a register.
    [SerializeField]
    private Shader renderAoShader;

    public float radius = 0.2f;
    public float bias;
    public float powerExponent = 1.0f;
    public bool enableBlur = true;
    public float blurSharpness = 1.0f;
    public BlurRadiusMode blurRadiusMode;

    private static Material fetchDepthMaterial;
    private static Material renderAoMaterial;

    // The block of code below is a neat trick to allow for calling into the debug console from C++
    [DllImport("HBAO_Plugin")]
    private static extern void LinkDebug([MarshalAs(UnmanagedType.FunctionPtr)]IntPtr debugCal);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void DebugLog(string log);

    private static readonly DebugLog debugLog = DebugWrapper;
    private static readonly IntPtr functionPointer = Marshal.GetFunctionPointerForDelegate(debugLog);

    private static void DebugWrapper(string log) { Debug.Log(log); }

    [DllImport("HBAO_Plugin", CallingConvention = CallingConvention.StdCall)]
    private static extern int GetEventID();

    [DllImport("HBAO_Plugin", CallingConvention = CallingConvention.StdCall)]
    private static extern void SetAoParameters(float Radius,
                                                float Bias,
                                                float PowerExponent,
                                                bool EnableBlur,
                                                int BlurRadiusMode,
                                                float BlurSharpness,
                                                int BlendMode);

    [DllImport("HBAO_Plugin", CallingConvention = CallingConvention.StdCall)]
    private static extern void SetInputData(float MetersToViewSpaceUnits,
                                                float[] pProjectionMatrix,
                                                float height,
                                                float width,
                                                float topLeftX,
                                                float topLeftY,
                                                float minDepth,
                                                float maxDepth);

    [DllImport("HBAO_Plugin", CallingConvention = CallingConvention.StdCall)]
    private static extern void SetOutputData(IntPtr pOutputTexture);

    [DllImport("HBAO_Plugin", CallingConvention = CallingConvention.StdCall)]
    private static extern void SetDepthTexture(IntPtr pDepthTexture);

    private void Start()
    {
        LinkDebug(functionPointer); // Hook our c++ plugin into Unitys console log.

        GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
        GetComponent<Camera>().depthTextureMode |= DepthTextureMode.DepthNormals;
    }

    private void OnEnable()
    {
        if (renderAoMaterial == null) renderAoMaterial = new Material(renderAoShader) { hideFlags = HideFlags.DontSave };
        if (fetchDepthMaterial == null) fetchDepthMaterial = new Material(fetchDepthShader) { hideFlags = HideFlags.DontSave };
    }

    private void OnDisable()
    {
        if (renderAoMaterial != null) DestroyImmediate(renderAoMaterial);
        if (fetchDepthMaterial != null) DestroyImmediate(fetchDepthMaterial);

        if (output != null)
        {
            output.Release();
            DestroyImmediate(output);
        }

        if (depth != null)
        {
            depth.Release();
            DestroyImmediate(depth);
        }
    }

    private RenderTexture depth;
    private RenderTexture output;

    // Perform AO immediately after opaque rendering.
    [ImageEffectOpaque]
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (output == null || output.width != Screen.width || output.height != Screen.height)
        {
            if (output != null)
            {
                output.Release();
                DestroyImmediate(output);
            }

            output = new RenderTexture(source.width, source.height, 0, RenderTextureFormat.ARGB32);

            output.Create(); // Must call create or ptr will be null.
            SetOutputData(output.GetNativeTexturePtr());
        }

        if (depth == null || depth.width != Screen.width || depth.height != Screen.height)
        {
            if (depth != null)
            {
                depth.Release();
                DestroyImmediate(depth);
            }

            depth = new RenderTexture(source.width, source.height, 24, RenderTextureFormat.Depth);
            depth.Create(); // Must call create or ptr will be null.
            SetDepthTexture(depth.GetNativeTexturePtr());
        }

        // Fetch depth
        Graphics.SetRenderTarget(output.colorBuffer, depth.depthBuffer);
        Graphics.Blit(source, fetchDepthMaterial);

        SetAoParameters(radius, bias, powerExponent, enableBlur, (int)blurRadiusMode, blurSharpness, 0);

        Matrix4x4 unityProjMatrix = GL.GetGPUProjectionMatrix(GetComponent<Camera>().projectionMatrix, false);
        float[] projMatrix = new float[16];
        for (int i = 0; i < 16; i++) projMatrix[i] = unityProjMatrix[i];

        SetInputData(1.0f, projMatrix, (float)Screen.height, (float)Screen.width, 0, 0, 0.0f, 1.0f);

        Graphics.SetRenderTarget(null);

        // Call our render method from the AO plugin.
        GL.IssuePluginEvent(GetEventID());

        renderAoMaterial.SetTexture("_AoResult", output);
        renderAoMaterial.SetTexture("_MainTex", source);
        
        Graphics.Blit(null, destination, renderAoMaterial);
    }
}
