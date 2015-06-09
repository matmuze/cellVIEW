using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class DisplaySettings : MonoBehaviour
{
    // Base settings
    public float Scale = 0.065f;
    public int ContourOptions;
    public float ContourStrength;
    public bool EnableShadows;
    public bool EnableOcclusionCulling;
    public bool DebugObjectCulling;
    public bool ShowMembrane;
    public bool ShowRNA;

    //DNA/RNA settings
    public bool EnableDNAConstraints;
    public float DistanceContraint;
    public float AngularConstraint;
    public int NumStepsPerSegment;
    public bool EnableTwist;
    public float TwistFactor;

    // Brownian motion
    public bool EnableBrownianMotion;   

    // Cross section
    public bool EnableCrossSection;

    [RangeAttribute(-50, 50)]
    public float CrossSectionPlaneDistance = 0;

    public Vector3 CrossSectionPlaneNormal;

    // Lod infos
    public bool EnableLod;
    public float FirstLevelOffset = 0;
    public float[] LodLevels = new float[16];

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
