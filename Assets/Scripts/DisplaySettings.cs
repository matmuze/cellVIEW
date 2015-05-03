using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class DisplaySettings : MonoBehaviour
{
    // Base settings
    public float Scale = 0.5f;
    public int ContourOptions;
    public float ContourStrength;

    // Brownian motion
    public bool EnableBrownianMotion;   

    // Cross section
    public bool EnableCrossSection;

    [RangeAttribute(-50, 50)]
    public float CrossSectionPlaneDistance = 0;

    public Vector3 CrossSectionPlaneNormal;

    // Lod
    public bool EnableLod;
    public float DistanceLod0 = 0;
    public float DistanceLod1 = 0;
    public float MaxAtomRadiusLod0 = 0;
    public float MinAtomRadiusLod1 = 0;
    public float DecimationFactorLod0 = 0;
    public float DecimationFactorLod1 = 0;

    // Declare the DisplaySettings as a singleton
    private static DisplaySettings _instance = null;
    public static DisplaySettings Instance
    {
        get
        {
            if (_instance != null) return _instance;

            _instance = FindObjectOfType<DisplaySettings>();
            if (_instance == null)
            {
                var go = GameObject.Find("_DisplaySettings");
                if (go != null) DestroyImmediate(go);

                go = new GameObject("_DisplaySettings") { hideFlags = HideFlags.HideInInspector };
                _instance = go.AddComponent<DisplaySettings>();
            }
            return _instance;
        }
    }
}
