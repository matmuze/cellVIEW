using System;
using System.IO;
using UnityEngine;

public static class Helper
{
    public static Quaternion RotationMatrixToQuaternion(Matrix4x4 m)
    {
        return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
    }

    public static Vector4 QuanternionToVector4(Quaternion q)
    {
        return new Vector4(q.x, q.y, q.z, q.w);
    }

    public static Quaternion Vector4ToQuaternion(Vector4 v)
    {
        return new Quaternion(v.x, v.y, v.z, v.w);
    }

    public static Color GetRandomColor()
    {
        return new Color(UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f));
    }

    public static float[] ReadBytesAsFloats(string filePath)
    {
        if (!File.Exists(filePath)) throw new Exception("File not found: " + filePath);

        var bytes = File.ReadAllBytes(filePath);
        var floats = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);

        return floats;
    }

    public static int GetIdFromColor(Color color)
    {
        int b = (int)(color.b * 255.0f);
        int g = (int)(color.g * 255.0f) << 8;
        int r = (int)(color.r * 255.0f) << 16;

        //Debug.Log("r: " + r + " g: " + g + " b:" + b);
        //Debug.Log("id: " + (r + g + b));
        //Debug.Log("color: " + color);

        return r + g + b;
    }

    public static Matrix4x4 GetProjectionMatrix(Camera camera)
    {
        bool d3d = SystemInfo.graphicsDeviceVersion.IndexOf("Direct3D") > -1;
        Matrix4x4 P = camera.projectionMatrix;
        if (d3d)
        {
            // Invert Y for rendering to a render texture
            for (int i = 0; i < 4; i++)
            {
                P[1, i] = -P[1, i];
            }
            // Scale and bias from OpenGL -> D3D depth range
            for (int i = 0; i < 4; i++)
            {
                P[2, i] = P[2, i] * 0.5f + P[3, i] * 0.5f;
            }
        }

        return P;
    }
}

