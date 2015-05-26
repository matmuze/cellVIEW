using System;
using UnityEngine;

[ExecuteInEditMode]
public class ComputeBufferManager : MonoBehaviour
{
    public ComputeBuffer InstanceTypes;
    public ComputeBuffer InstanceStates;
    public ComputeBuffer InstanceCullFlags;
    public ComputeBuffer InstancePositions;
    public ComputeBuffer InstanceRotations;
    public ComputeBuffer InstanceDisplayPositions;
    public ComputeBuffer InstanceDisplayRotations;
    
    public ComputeBuffer IngredientColors;
    public ComputeBuffer IngredientToggleFlags;
    public ComputeBuffer IngredientBoundingSpheres;

    public ComputeBuffer ProteinAtomPositions;
    public ComputeBuffer ProteinClusterPositions;
    public ComputeBuffer ProteinSphereBatchInfos;

    public ComputeBuffer IngredientAtomCount;
    public ComputeBuffer IngredientAtomStart;
    public ComputeBuffer IngredientClusterCount;
    public ComputeBuffer IngredientClusterStart;

    public ComputeBuffer LipidAtomPositions;		
    public ComputeBuffer LipidSphereBatchInfos;
    public ComputeBuffer LipidInstancePositions;	

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

    void OnEnable()
    {
        InitBuffers();
    }

    void OnDisable()
    {
        ReleaseBuffers();
    }

    void InitBuffers ()
    {
        // Instance data
        if (InstanceTypes == null) InstanceTypes = new ComputeBuffer(SceneManager.NumInstancesMax, 4);
        if (InstanceStates == null) InstanceStates = new ComputeBuffer(SceneManager.NumInstancesMax, 4);
        if (InstanceCullFlags == null) InstanceCullFlags = new ComputeBuffer(SceneManager.NumInstancesMax, 4);
        if (InstancePositions == null) InstancePositions = new ComputeBuffer(SceneManager.NumInstancesMax, 16);
        if (InstanceRotations == null) InstanceRotations = new ComputeBuffer(SceneManager.NumInstancesMax, 16);
        if (InstanceDisplayPositions == null) InstanceDisplayPositions = new ComputeBuffer(SceneManager.NumInstancesMax, 16);
        if (InstanceDisplayRotations == null) InstanceDisplayRotations = new ComputeBuffer(SceneManager.NumInstancesMax, 16);

        if (ProteinSphereBatchInfos == null) ProteinSphereBatchInfos = new ComputeBuffer(SceneManager.NumProteinSphereBatchesMax, 16, ComputeBufferType.Append);

        // Ingredient data
        if (IngredientColors == null) IngredientColors = new ComputeBuffer(SceneManager.NumIngredientsMax, 16);
        if (IngredientToggleFlags == null) IngredientToggleFlags = new ComputeBuffer(SceneManager.NumIngredientsMax, 4);
        if (IngredientBoundingSpheres == null) IngredientBoundingSpheres = new ComputeBuffer(SceneManager.NumIngredientsMax, 4);

        // Atom data
        if (ProteinAtomPositions == null) ProteinAtomPositions = new ComputeBuffer(SceneManager.NumAtomMax, 16);
        if (IngredientAtomCount == null) IngredientAtomCount = new ComputeBuffer(SceneManager.NumIngredientsMax, 4);
        if (IngredientAtomStart == null) IngredientAtomStart = new ComputeBuffer(SceneManager.NumIngredientsMax, 4);
        
        // Cluster data
        if (ProteinClusterPositions == null) ProteinClusterPositions = new ComputeBuffer(SceneManager.NumAtomMax, 16);
        if (IngredientClusterCount == null) IngredientClusterCount = new ComputeBuffer(SceneManager.NumIngredientsMax, 16);
        if (IngredientClusterStart == null) IngredientClusterStart = new ComputeBuffer(SceneManager.NumIngredientsMax, 16);

        // Cluster data
        if (LipidAtomPositions == null) LipidAtomPositions = new ComputeBuffer(SceneManager.NumLipidAtomMax, 16);
        if (LipidSphereBatchInfos == null) LipidSphereBatchInfos = new ComputeBuffer(SceneManager.NumLipidInstancesMax, 16);
        if (LipidInstancePositions == null) LipidInstancePositions = new ComputeBuffer(SceneManager.NumLipidInstancesMax, 16);
	}
	
	// Update is called once per frame
	void ReleaseBuffers ()
    {
        if (InstanceTypes != null) { InstanceTypes.Release(); InstanceTypes = null; }
	    if (InstanceStates != null) { InstanceStates.Release(); InstanceStates = null; }
        if (InstanceCullFlags != null) { InstanceCullFlags.Release(); InstanceCullFlags = null; }
	    if (InstancePositions != null) { InstancePositions.Release(); InstancePositions = null; }
	    if (InstanceRotations != null) { InstanceRotations.Release(); InstanceRotations = null; }
	    if (InstanceDisplayPositions != null) { InstanceDisplayPositions.Release(); InstanceDisplayPositions = null; }
	    if (InstanceDisplayRotations != null) { InstanceDisplayRotations.Release(); InstanceDisplayRotations = null; }
	    
        if (IngredientColors != null) { IngredientColors.Release(); IngredientColors = null; }
	    if (IngredientToggleFlags != null) { IngredientToggleFlags.Release(); IngredientToggleFlags = null; }
	    if (IngredientBoundingSpheres != null) { IngredientBoundingSpheres.Release(); IngredientBoundingSpheres = null; }

        if (ProteinSphereBatchInfos != null) { ProteinSphereBatchInfos.Release(); ProteinSphereBatchInfos = null; }
        if (ProteinAtomPositions != null) { ProteinAtomPositions.Release(); ProteinAtomPositions = null; }
	    if (IngredientAtomCount != null) { IngredientAtomCount.Release(); IngredientAtomCount = null; }
	    if (IngredientAtomStart != null) { IngredientAtomStart.Release(); IngredientAtomStart = null; }
        if (ProteinClusterPositions != null) { ProteinClusterPositions.Release(); ProteinClusterPositions = null; }
	    if (IngredientClusterCount != null) { IngredientClusterCount.Release(); IngredientClusterCount = null; }
	    if (IngredientClusterStart != null) { IngredientClusterStart.Release(); IngredientClusterStart = null; }

        if (LipidAtomPositions != null) { LipidAtomPositions.Release(); LipidAtomPositions = null; }
        if (LipidSphereBatchInfos != null) { LipidSphereBatchInfos.Release(); LipidSphereBatchInfos = null; }
        if (LipidInstancePositions != null) { LipidInstancePositions.Release(); LipidInstancePositions = null; }
	}
}
