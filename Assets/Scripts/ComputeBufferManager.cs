using System;
using UnityEngine;

[ExecuteInEditMode]
public class ComputeBufferManager : MonoBehaviour
{
    public ComputeBuffer AtomRadii;
    public ComputeBuffer AtomPositions;

    public ComputeBuffer InstanceTypes;
    public ComputeBuffer InstanceStates; 
    public ComputeBuffer InstancePositions;
    public ComputeBuffer InstanceRotations;
    public ComputeBuffer InstanceDisplayPositions;
    public ComputeBuffer InstanceDisplayRotations;

    public ComputeBuffer SubInstanceCullFlags;
    public ComputeBuffer SubInstanceInformations;

    public ComputeBuffer IngredientColors;
    public ComputeBuffer IngredientAtomCount;
    public ComputeBuffer IngredientAtomStart;
    public ComputeBuffer IngredientToggleFlags;
    public ComputeBuffer IngredientBoundingSpheres;

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

                    go = new GameObject("_ComputeBufferManager");
                    go.hideFlags = HideFlags.HideInInspector;
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
        if (InstancePositions == null) InstancePositions = new ComputeBuffer(SceneManager.NumInstancesMax, 16);
        if (InstanceRotations == null) InstanceRotations = new ComputeBuffer(SceneManager.NumInstancesMax, 16);
        if (InstanceDisplayPositions == null) InstanceDisplayPositions = new ComputeBuffer(SceneManager.NumInstancesMax, 16);
        if (InstanceDisplayRotations == null) InstanceDisplayRotations = new ComputeBuffer(SceneManager.NumInstancesMax, 16);

        if (SubInstanceCullFlags == null) SubInstanceCullFlags = new ComputeBuffer(SceneManager.NumSubInstancesMax, 4);
        if (SubInstanceInformations == null) SubInstanceInformations = new ComputeBuffer(SceneManager.NumSubInstancesMax, 16);

        // Ingredient data
        if (IngredientColors == null) IngredientColors = new ComputeBuffer(SceneManager.NumIngredientsMax, 16);
        if (IngredientAtomCount == null) IngredientAtomCount = new ComputeBuffer(SceneManager.NumIngredientsMax, 4);
        if (IngredientAtomStart == null) IngredientAtomStart = new ComputeBuffer(SceneManager.NumIngredientsMax, 4);
        if (IngredientToggleFlags == null) IngredientToggleFlags = new ComputeBuffer(SceneManager.NumIngredientsMax, 4);
        if (IngredientBoundingSpheres == null) IngredientBoundingSpheres = new ComputeBuffer(SceneManager.NumIngredientsMax, 4);

        // Atom data
        if (AtomRadii == null) { AtomRadii = new ComputeBuffer(PdbLoader.AtomSymbols.Length, 4); AtomRadii.SetData(PdbLoader.AtomRadii); }
        if (AtomPositions == null) AtomPositions = new ComputeBuffer(SceneManager.NumAtomMax, 16);
	}
	
	// Update is called once per frame
	void ReleaseBuffers ()
    {
        if (InstanceTypes != null) InstanceTypes.Release();
        if (InstanceStates != null) InstanceStates.Release();
        if (SubInstanceCullFlags != null) SubInstanceCullFlags.Release();
        if (InstancePositions != null) InstancePositions.Release();
        if (InstanceRotations != null) InstanceRotations.Release();
        if (SubInstanceInformations != null) SubInstanceInformations.Release();
        if (InstanceDisplayPositions != null) InstanceDisplayPositions.Release();
        if (InstanceDisplayRotations != null) InstanceDisplayRotations.Release();
        
        if (IngredientColors != null) IngredientColors.Release();
        if (IngredientToggleFlags != null) IngredientToggleFlags.Release();
        if (IngredientAtomCount != null) IngredientAtomCount.Release();
        if (IngredientAtomStart != null) IngredientAtomStart.Release();
        if (IngredientBoundingSpheres != null) IngredientBoundingSpheres.Release();

        if (AtomRadii != null) AtomRadii.Release();
        if (AtomPositions != null) AtomPositions.Release();
	}
}
