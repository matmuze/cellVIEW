using System;
using UnityEngine;

[ExecuteInEditMode]
public class ComputeBufferManager : MonoBehaviour
{
    public ComputeBuffer InstancesTypes;
    public ComputeBuffer AtomRadiiBuffer;
    public ComputeBuffer IngredientsColors;
    public ComputeBuffer InstancesStates;
    public ComputeBuffer AtomColorsBuffer;
    public ComputeBuffer IngredientsAtomPdbData;
    public ComputeBuffer IngredientsAtomCount;
    public ComputeBuffer IngredientsAtomStart;
    public ComputeBuffer InstancesPositions;
    public ComputeBuffer InstancesRotations;
    public ComputeBuffer SubInstancesInformations;
    public ComputeBuffer SubInstancesPositions;
    public ComputeBuffer IngredientsToggle;
    public ComputeBuffer IngredientsBoundingSphereRadius;

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
        // Ingredient data
        if (IngredientsAtomCount == null)
            IngredientsAtomCount = new ComputeBuffer(SceneManager.NumIngredientsMax, 4);

        if (IngredientsAtomStart == null)
            IngredientsAtomStart = new ComputeBuffer(SceneManager.NumIngredientsMax, 4);
        
        if (IngredientsColors == null)
            IngredientsColors = new ComputeBuffer(SceneManager.NumIngredientsMax, 16);

        if (IngredientsToggle == null)
            IngredientsToggle = new ComputeBuffer(SceneManager.NumIngredientsMax, 4);

        if (IngredientsAtomPdbData == null)
            IngredientsAtomPdbData = new ComputeBuffer(SceneManager.NumIngredientsAtomMax, 16);

        if (IngredientsBoundingSphereRadius == null)
            IngredientsBoundingSphereRadius = new ComputeBuffer(SceneManager.NumIngredientsMax, 4);
        
        // Scene data
        if (InstancesTypes == null)
            InstancesTypes = new ComputeBuffer(SceneManager.NumInstancesMax, 4);

        if (InstancesStates == null)
            InstancesStates = new ComputeBuffer(SceneManager.NumInstancesMax, 4);

        if (InstancesPositions == null)
            InstancesPositions = new ComputeBuffer(SceneManager.NumInstancesMax, 16);

        if (InstancesRotations == null)
            InstancesRotations = new ComputeBuffer(SceneManager.NumInstancesMax, 16);

        if (SubInstancesInformations == null)
            SubInstancesInformations = new ComputeBuffer(SceneManager.NumSubInstancesMax, 16);

        if (SubInstancesPositions == null)
            SubInstancesPositions = new ComputeBuffer(SceneManager.NumSubInstancesMax, 16);
        
        // Misc
        if (AtomRadiiBuffer == null)
        {
            AtomRadiiBuffer = new ComputeBuffer(PdbLoader.AtomSymbols.Length, 4);
            AtomRadiiBuffer.SetData(PdbLoader.AtomRadii);
        }

        if (AtomColorsBuffer == null)
        {
            AtomColorsBuffer = new ComputeBuffer(PdbLoader.AtomSymbols.Length, 16);
            AtomColorsBuffer.SetData(PdbLoader.AtomColors);
        }
	}
	
	// Update is called once per frame
	void ReleaseBuffers ()
    {
        if (InstancesTypes != null) 
            InstancesTypes.Release();

        if (InstancesStates != null) 
            InstancesStates.Release();

        if (IngredientsColors != null) 
            IngredientsColors.Release();

        if (AtomRadiiBuffer != null) 
            AtomRadiiBuffer.Release();

        if (AtomColorsBuffer != null) 
            AtomColorsBuffer.Release();

        if (IngredientsAtomPdbData != null) 
            IngredientsAtomPdbData.Release();

        if (IngredientsAtomCount != null) 
            IngredientsAtomCount.Release();

        if (IngredientsAtomStart != null)
            IngredientsAtomStart.Release();

        if (InstancesPositions != null) 
            InstancesPositions.Release();

        if (InstancesRotations != null) 
            InstancesRotations.Release();

        if (SubInstancesInformations != null) 
            SubInstancesInformations.Release();

        if (SubInstancesPositions != null)
            SubInstancesPositions.Release();

        if (IngredientsToggle != null)
            IngredientsToggle.Release();

        if (IngredientsBoundingSphereRadius != null)
            IngredientsBoundingSphereRadius.Release();
	}
}
