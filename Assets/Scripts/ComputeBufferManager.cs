using System;
using UnityEngine;

[ExecuteInEditMode]
public class ComputeBufferManager : MonoBehaviour
{
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
        // Dna control points
        if (DnaAtoms == null) DnaAtoms = new ComputeBuffer(SceneManager.NumDnaAtomsMax, 16);
        if (DnaControlPoints == null) DnaControlPoints = new ComputeBuffer(SceneManager.NumDnaControlPointsMax, 16);
	}
	
	// Update is called once per frame
	void ReleaseBuffers ()
    {
        if (DnaAtoms != null) { DnaAtoms.Release(); DnaAtoms = null; }
        if (DnaControlPoints != null) { DnaControlPoints.Release(); DnaControlPoints = null; }
	}
}
