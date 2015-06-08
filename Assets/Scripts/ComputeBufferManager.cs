using System;
using UnityEngine;

[ExecuteInEditMode]
public class ComputeBufferManager : MonoBehaviour
{
    //public int NumProteinAtomMax = 1000000;        
    //public int NumIngredientsMax = 1000;    
    //public int NumProteinInstancesMax = 25000000;      
    //public int NumProteinSphereBatchesMax = 25000000;  
    //public int NumLipidAtomMax = 8000000;    
    //public int NumLipidInstancesMax = 25000000;
    //public int NumDnaAtomsMax = 1000;
    //public int NumDnaControlPointsMax = 1000000;

    [NonSerialized]
    public int NumProteinAtomMax = 0;

    [NonSerialized]
    public int NumIngredientsMax = 0;

    [NonSerialized]
    public int NumProteinInstancesMax = 0;

    [NonSerialized]
    public int NumProteinSphereBatchesMax = 0;

    [NonSerialized]
    public int NumLipidAtomMax = 0;

    [NonSerialized]
    public int NumLipidInstancesMax = 0;

    [NonSerialized]
    public int NumDnaAtomsMax = 0;

    [NonSerialized]
    public int NumDnaControlPointsMax = 0;


    public ComputeBuffer UnitInstancePosition;

    public ComputeBuffer ProteinInstanceInfos;
    public ComputeBuffer ProteinInstanceCullFlags;
    public ComputeBuffer ProteinInstancePositions;
    public ComputeBuffer ProteinInstanceRotations;
    public ComputeBuffer InstanceDisplayPositions;
    public ComputeBuffer InstanceDisplayRotations;
    
    public ComputeBuffer ProteinColors;
    public ComputeBuffer ProteinVisibilityFlags;

    public ComputeBuffer ProteinAtomPositions;
    public ComputeBuffer ProteinClusterPositions;
    public ComputeBuffer ProteinSphereBatchInfos;

    public ComputeBuffer ProteinAtomCount;
    public ComputeBuffer ProteinAtomStart;
    public ComputeBuffer ProteinClusterCount;
    public ComputeBuffer ProteinClusterStart;

    public ComputeBuffer LipidAtomPositions;		
    public ComputeBuffer LipidSphereBatchInfos;
    public ComputeBuffer LipidInstancePositions;
    public ComputeBuffer LipidInstanceCullFlags;

    public ComputeBuffer DnaAtoms;
    public ComputeBuffer DnaControlPoints;

    //*****//

    // Declare the buffer manager as a singleton
    private static ComputeBufferManager _instance = null;
    public static ComputeBufferManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ComputeBufferManager>();
                if (_instance == null)
                {
                    var go = GameObject.Find("_ComputeBufferManager");
                    if (go != null)
                        DestroyImmediate(go);

                    go = new GameObject("_ComputeBufferManager") {hideFlags = HideFlags.HideInInspector};
                    _instance = go.AddComponent<ComputeBufferManager>();
                }
            }

            return _instance;
        }
    }

    // Hack to clear append buffer
    public static void ClearAppendBuffer(ComputeBuffer appendBuffer)
    {
        // This resets the append buffer buffer to 0
        var dummy1 = RenderTexture.GetTemporary(8, 8, 24, RenderTextureFormat.ARGB32);
        var dummy2 = RenderTexture.GetTemporary(8, 8, 24, RenderTextureFormat.ARGB32);
        var active = RenderTexture.active;

        Graphics.SetRandomWriteTarget(1, appendBuffer);
        Graphics.Blit(dummy1, dummy2);
        Graphics.ClearRandomWriteTargets();

        RenderTexture.active = active;

        dummy1.Release();
        dummy2.Release();
    }

    void OnEnable()
    {
        InitBuffers();
    }

    void OnDisable()
    {
        ReleaseBuffers();
    }

    public void InitBuffers ()
    {
        if (NumProteinInstancesMax == 0 || NumLipidInstancesMax == 0 || NumLipidInstancesMax == 0) return;

        // Before declaring new buffers make sure that all the previous buffers are cleared
        ReleaseBuffers();
        
        if (UnitInstancePosition == null) UnitInstancePosition = new ComputeBuffer(100, 16);

        // Instance data
        if (ProteinInstanceInfos == null) ProteinInstanceInfos = new ComputeBuffer(NumProteinInstancesMax, 16);
        if (ProteinInstanceCullFlags == null) ProteinInstanceCullFlags = new ComputeBuffer(NumProteinInstancesMax, 16);
        if (ProteinInstancePositions == null) ProteinInstancePositions = new ComputeBuffer(NumProteinInstancesMax, 16);
        if (ProteinInstanceRotations == null) ProteinInstanceRotations = new ComputeBuffer(NumProteinInstancesMax, 16);
        if (InstanceDisplayPositions == null) InstanceDisplayPositions = new ComputeBuffer(NumProteinInstancesMax, 16);
        if (InstanceDisplayRotations == null) InstanceDisplayRotations = new ComputeBuffer(NumProteinInstancesMax, 16);
        if (ProteinSphereBatchInfos == null) ProteinSphereBatchInfos = new ComputeBuffer(NumProteinSphereBatchesMax, 16, ComputeBufferType.Append);

        // Ingredient data
        if (ProteinColors == null) ProteinColors = new ComputeBuffer(NumIngredientsMax, 16);
        if (ProteinVisibilityFlags == null) ProteinVisibilityFlags = new ComputeBuffer(NumIngredientsMax, 4);

        // Atom data
        if (ProteinAtomPositions == null) ProteinAtomPositions = new ComputeBuffer(NumProteinAtomMax, 16);
        if (ProteinAtomCount == null) ProteinAtomCount = new ComputeBuffer(NumIngredientsMax, 4);
        if (ProteinAtomStart == null) ProteinAtomStart = new ComputeBuffer(NumIngredientsMax, 4);
        
        // Cluster data
        if (ProteinClusterPositions == null) ProteinClusterPositions = new ComputeBuffer(NumProteinAtomMax, 16);
        if (ProteinClusterCount == null) ProteinClusterCount = new ComputeBuffer(NumIngredientsMax, 16);
        if (ProteinClusterStart == null) ProteinClusterStart = new ComputeBuffer(NumIngredientsMax, 16);

        // Lipid data
        if (LipidAtomPositions == null) LipidAtomPositions = new ComputeBuffer(NumLipidAtomMax, 16);
        if (LipidSphereBatchInfos == null) LipidSphereBatchInfos = new ComputeBuffer(NumLipidInstancesMax, 16);
        if (LipidInstanceCullFlags == null) LipidInstanceCullFlags = new ComputeBuffer(NumLipidInstancesMax, 16);
        if (LipidInstancePositions == null) LipidInstancePositions = new ComputeBuffer(NumLipidInstancesMax, 16);

        // Dna data
        if (DnaAtoms == null) DnaAtoms = new ComputeBuffer(NumDnaAtomsMax, 16);
        if (DnaControlPoints == null) DnaControlPoints = new ComputeBuffer(NumDnaControlPointsMax, 16);
	}
	
	// Update is called once per frame
	void ReleaseBuffers ()
    {
        if (UnitInstancePosition != null) { UnitInstancePosition.Release(); UnitInstancePosition = null; }

        if (ProteinInstanceInfos != null) { ProteinInstanceInfos.Release(); ProteinInstanceInfos = null; }
        if (ProteinInstanceCullFlags != null) { ProteinInstanceCullFlags.Release(); ProteinInstanceCullFlags = null; }
	    if (ProteinInstancePositions != null) { ProteinInstancePositions.Release(); ProteinInstancePositions = null; }
	    if (ProteinInstanceRotations != null) { ProteinInstanceRotations.Release(); ProteinInstanceRotations = null; }
	    if (InstanceDisplayPositions != null) { InstanceDisplayPositions.Release(); InstanceDisplayPositions = null; }
	    if (InstanceDisplayRotations != null) { InstanceDisplayRotations.Release(); InstanceDisplayRotations = null; }
	    
        if (ProteinColors != null) { ProteinColors.Release(); ProteinColors = null; }
	    if (ProteinVisibilityFlags != null) { ProteinVisibilityFlags.Release(); ProteinVisibilityFlags = null; }
        if (ProteinSphereBatchInfos != null) { ProteinSphereBatchInfos.Release(); ProteinSphereBatchInfos = null; }
        if (ProteinAtomPositions != null) { ProteinAtomPositions.Release(); ProteinAtomPositions = null; }
	    if (ProteinAtomCount != null) { ProteinAtomCount.Release(); ProteinAtomCount = null; }
	    if (ProteinAtomStart != null) { ProteinAtomStart.Release(); ProteinAtomStart = null; }
        if (ProteinClusterPositions != null) { ProteinClusterPositions.Release(); ProteinClusterPositions = null; }
	    if (ProteinClusterCount != null) { ProteinClusterCount.Release(); ProteinClusterCount = null; }
	    if (ProteinClusterStart != null) { ProteinClusterStart.Release(); ProteinClusterStart = null; }

        if (LipidAtomPositions != null) { LipidAtomPositions.Release(); LipidAtomPositions = null; }
        if (LipidSphereBatchInfos != null) { LipidSphereBatchInfos.Release(); LipidSphereBatchInfos = null; }
        if (LipidInstancePositions != null) { LipidInstancePositions.Release(); LipidInstancePositions = null; }
        if (LipidInstanceCullFlags != null) { LipidInstanceCullFlags.Release(); LipidInstanceCullFlags = null; }

        if (DnaAtoms != null) { DnaAtoms.Release(); DnaAtoms = null; }
        if (DnaControlPoints != null) { DnaControlPoints.Release(); DnaControlPoints = null; }
	}
}
