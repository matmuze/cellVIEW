using System;
using System.Collections.Generic;
using UnityEngine;

enum InstanceState
{
    Null = -1,           // Instance will not be displayed
    Normal = 0,          // Instance will be displayed with normal color
    Highlighted = 1      // Instance will be displayed with highlighted color
};

[ExecuteInEditMode]
public class SceneManager : MonoBehaviour
{
    //*****//

    public const int NumLodLevels = 2;
    public const int NumAtomMax = 1000000;          // Used for GPU buffer memory allocation
    public const int NumIngredientsMax = 1000;      // Used for GPU buffer memory allocation

    public const int NumProteinInstancesMax = 15000;       // Used for GPU buffer memory allocation
    public const int NumProteinSphereBatchesMax = 30000;   // Used for GPU buffer memory allocation

    public const int NumLipidAtomMax = 8000000;     // Used for GPU buffer memory allocation
    public const int NumLipidInstancesMax = 300000; // Used for GPU buffer memory allocation

    public const int NumDnaControlPointsMax = 1000000;
    public const int NumDnaAtomsMax = 1000;

    // Scene data
    public List<int> InstanceTypes = new List<int>();
    public List<int> InstanceStates = new List<int>();
    public List<Vector4> InstancePositions = new List<Vector4>();
    public List<Vector4> InstanceRotations = new List<Vector4>();

    // Ingredient data
    public List<int> IngredientToggleFlags = new List<int>();
    public List<string> IngredientNames = new List<string>();
    public List<Vector4> IngredientsColors = new List<Vector4>();
    public List<float> IngredientBoundingSpheres = new List<float>();
    
    // Atom data
    public List<Vector4> AtomPositions = new List<Vector4>();
    public List<int> IngredientAtomCount = new List<int>();
    public List<int> IngredientAtomStart = new List<int>();
    
    // Cluster data 
    public List<Vector4> ClusterPositions = new List<Vector4>();
    public List<Vector4> IngredientClusterCount = new List<Vector4>();
    public List<Vector4> IngredientClusterStart = new List<Vector4>();
    
    // Lipid data 
    public List<Vector4> LipidAtomPositions = new List<Vector4>();
    public List<Vector4> LipidSphereBatchInfos = new List<Vector4>();
    public List<Vector4> LipidInstancePositions = new List<Vector4>();

    // Dna data
    public List<Vector4> DnaAtoms = new List<Vector4>();
    public List<Vector4> DnaControlPoints = new List<Vector4>();

    public int NumProteinInstances
    {
        get { return InstancePositions.Count; }
    }

    public int NumLipidInstances
    {
        get { return LipidInstancePositions.Count; }
    }

    public int NumDnaControlPoints
    {
        get { return DnaControlPoints.Count; }
    }

    public int NumDnaSegments
    {
        get { return DnaControlPoints.Count - 1; }
    }

    //*****//
    
    // Declare the scene manager as a singleton
    private static SceneManager _instance = null;
    public static SceneManager Instance
    {
        get
        {
            if (_instance != null) return _instance;

            _instance = FindObjectOfType<SceneManager>();
            if (_instance == null)
            {
                var go = GameObject.Find("_SceneManager");
                if (go != null) DestroyImmediate(go);

                go = new GameObject("_SceneManager") {hideFlags = HideFlags.HideInInspector};
                _instance = go.AddComponent<SceneManager>();
            }

            _instance.OnUnityReload();
            return _instance;
        }
    }
    
    private void OnUnityReload()
    {
        Debug.Log("Unity reload");

        //_instance.ClearScene();
        _instance.UploadAllData();
    }

    public void AddIngredient(string ingredientName, Bounds bounds, List<Vector4> atoms, List<List<Vector4>> clusters = null) 
    {
        if (IngredientNames.Contains(ingredientName)) return;

        //IngredientToggleFlags.Add(1);
        IngredientNames.Add(ingredientName);
        IngredientsColors.Add(Helper.GetRandomColor());
        IngredientBoundingSpheres.Add(Vector3.Magnitude(bounds.extents));

        IngredientAtomCount.Add(atoms.Count);
        IngredientAtomStart.Add(AtomPositions.Count);
        AtomPositions.AddRange(atoms);

        var clusterCount = new Vector4(0,0,0,0);
        var clusterStart = new Vector4(0,0,0,0);

        if (clusters != null)
        {
            for (int i = 0; i < Mathf.Min(clusters.Count, 4); i++)
            {
                clusterCount[i] = clusters[i].Count;
                clusterStart[i] = ClusterPositions.Count;
                ClusterPositions.AddRange(clusters[i]);
            }
        }

        IngredientClusterCount.Add(clusterCount);
        IngredientClusterStart.Add(clusterStart);
    }

    public void AddIngredientInstance(string ingredientName, Vector3 position, Quaternion rotation)
    {
        if (!IngredientNames.Contains(ingredientName))
        {
            throw new Exception("Ingredient type do not exists");
        }

        var ingredientId = IngredientNames.IndexOf(ingredientName);

        InstanceTypes.Add(ingredientId);
        InstanceStates.Add((int)InstanceState.Normal);
        InstancePositions.Add(position);
        InstanceRotations.Add(Helper.QuanternionToVector4(rotation));
    }

    public void LoadMembrane(string filePath, Vector3 position, Quaternion rotation)
    {
        if (LipidAtomPositions.Count != 0)
        {
            throw new Exception("Membrane already added");
        }

        var lipidIndex = 1;
        var lipidAtomStart = 0;
        var lipidAtoms = new List<Vector4>();
        int ingredientId = IngredientNames.Count;
        var membraneAtoms = new List<Vector4>();
        var membraneData = Helper.ReadBytesAsFloats(filePath);

        for (var i = 0; i < membraneData.Length; i += 5)
        {
            if ((int)membraneData[i + 4] != lipidIndex)
            {
                var bounds = PdbLoader.GetBounds(lipidAtoms);
                var center = new Vector4(bounds.center.x, bounds.center.y, bounds.center.z, 0);
                for (var j = 0; j < lipidAtoms.Count; j++) lipidAtoms[j] -= center;

                LipidSphereBatchInfos.Add(new Vector4(lipidAtoms.Count, lipidAtomStart, Vector3.Magnitude(bounds.extents), 0));
                LipidInstancePositions.Add(position + bounds.center);

                lipidAtomStart += lipidAtoms.Count;
                lipidIndex = (int)membraneData[i + 4];

                membraneAtoms.AddRange(lipidAtoms);
                lipidAtoms.Clear();
            }

            lipidAtoms.Add(new Vector4(membraneData[i], membraneData[i + 1], membraneData[i + 2], PdbLoader.AtomRadii[(int)membraneData[i + 3]]));
        }

        LipidAtomPositions.AddRange(membraneAtoms);
    }
    
    public void LoadRna(List<Vector4> controlPoints)
    {
        var normalizedCp = new List<Vector4>();
        normalizedCp.Add(controlPoints[0]);
        normalizedCp.Add(controlPoints[1]);

        var currentPointId = 1;
        var currentPosition = controlPoints[currentPointId];

        float distance = DisplaySettings.Instance.DistanceContraint;
        float lerpValue = 0.0f;

        // Normalize the distance between control points
        while (true)
        {
            if (currentPointId + 2 >= controlPoints.Count) break;

            var cp0 = controlPoints[currentPointId - 1];
            var cp1 = controlPoints[currentPointId];
            var cp2 = controlPoints[currentPointId + 1];
            var cp3 = controlPoints[currentPointId + 2];

            var found = false;

            for (; lerpValue <= 1; lerpValue += 0.01f)
            {
                var candidate = Helper.CubicInterpolate(cp0, cp1, cp2, cp3, lerpValue);
                var d = Vector3.Distance(currentPosition, candidate);

                if (d > distance)
                {
                    normalizedCp.Add(candidate);
                    currentPosition = candidate;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                lerpValue = 0;
                currentPointId++;
            }
        }

        DnaControlPoints.AddRange(normalizedCp);
        //DnaControlPoints.AddRange(controlPoints);

        Debug.Log(normalizedCp.Count);

        //var bounds = PdbLoader.GetBounds(DnaControlPoints);
        //PdbLoader.OffsetPoints(ref DnaControlPoints, bounds.center);

        var atoms = PdbLoader.ReadPdbFile(PdbLoader.GetPdbFilePath("basesingle"));
        //var atomBounds = PdbLoader.GetBounds(atoms);
        //PdbLoader.OffsetPoints(ref atoms, atomBounds.center);
        DnaAtoms.AddRange(atoms);
    }

    //--------------------------------------------------------------------------------------
    // Object picking

    private bool _enableSelection = false;
    private int _selectedInstance = -1;
    private Vector3 _savedPosition;
    private Quaternion _savedRotation;
    private GameObject _selectedGameObject = null;

    public void SetSelectedInstance(int selectedInstance)
    {
        //var uploadData = false;

        //// Reset current selected instance
        //if (_selectedInstance != -1)
        //{
        //    InstanceStates[_selectedInstance] = (int)InstanceState.Normal;
        //    _selectedInstance = -1;

        //    uploadData = true;
        //}

        //// If selected instance is valid
        //if (selectedInstance != 16777215)
        //{
        //    _selectedInstance = selectedInstance;
        //    InstanceStates[selectedInstance] = (int)InstanceState.Highlighted;

        //    _selectedGameObject = GameObject.Find("Selected Element");
        //    if (_selectedGameObject == null) _selectedGameObject = new GameObject("Selected Element");

        //    _selectedGameObject.transform.position = InstancePositions[selectedInstance] * DisplaySettings.Instance.Scale;
        //    _selectedGameObject.transform.rotation = Helper.Vector4ToQuaternion(InstanceRotations[selectedInstance]);
            
        //    _savedPosition = _selectedGameObject.transform.position;
        //    _savedRotation = _selectedGameObject.transform.rotation;

        //    uploadData = true;
        //}

        //if (uploadData)
        //{
        //    ComputeBufferManager.Instance.InstanceStates.SetData(InstanceStates.ToArray());
        //}
    }

    private void UpdateSelectedInstance()
    {
        //if (_selectedInstance == -1) return;

        //if (_savedPosition != _selectedGameObject.transform.position || _savedRotation != _selectedGameObject.transform.rotation)
        //{
        //    Debug.Log("Selected instance transform changed");

        //    InstancePositions[_selectedInstance] = _selectedGameObject.transform.position / DisplaySettings.Instance.Scale;
        //    InstanceRotations[_selectedInstance] = Helper.QuanternionToVector4(_selectedGameObject.transform.rotation);

        //    ComputeBufferManager.Instance.InstancePositions.SetData(InstancePositions.ToArray());
        //    ComputeBufferManager.Instance.InstanceRotations.SetData(InstanceRotations.ToArray());

        //    _savedPosition = _selectedGameObject.transform.position;
        //    _savedRotation = _selectedGameObject.transform.rotation;
        //}
    }
    
    //--------------------------------------------------------------------------------------

    void Update()
    {
        UpdateSelectedInstance();
    }

    //--------------------------------------------------------------------------------------
    // Misc functions

    // Scene data gets serialized on each reload, to clear the scene call this function
    public void ClearScene()
    {
        Debug.Log("Clear scene");

        // Clear scene data
        InstanceTypes.Clear();
        InstanceStates.Clear();
        InstancePositions.Clear();
        InstanceRotations.Clear();

        // Clear ingredient data
        IngredientNames.Clear();
        IngredientsColors.Clear();
        //IngredientToggleFlags.Clear();
        IngredientBoundingSpheres.Clear();
        
        // Clear atom data
        AtomPositions.Clear();
        IngredientAtomCount.Clear();
        IngredientAtomStart.Clear();

         // Clear cluster data
        ClusterPositions.Clear();
        IngredientClusterStart.Clear();
        IngredientClusterCount.Clear();

        // Clear lipid data
        LipidAtomPositions.Clear();
        LipidInstancePositions.Clear();
        LipidSphereBatchInfos.Clear();

        // Clear dna data
        DnaAtoms.Clear();
        DnaControlPoints.Clear();
    }

    public void UploadAllData()
    {
        // Upload scene data
        ComputeBufferManager.Instance.InstanceTypes.SetData(InstanceTypes.ToArray());
        ComputeBufferManager.Instance.InstanceStates.SetData(InstanceStates.ToArray());
        ComputeBufferManager.Instance.InstancePositions.SetData(InstancePositions.ToArray());
        ComputeBufferManager.Instance.InstanceRotations.SetData(InstanceRotations.ToArray());

        // Upload ingredient data
        ComputeBufferManager.Instance.IngredientColors.SetData(IngredientsColors.ToArray());
        ComputeBufferManager.Instance.IngredientToggleFlags.SetData(IngredientToggleFlags.ToArray());
        ComputeBufferManager.Instance.IngredientBoundingSpheres.SetData(IngredientBoundingSpheres.ToArray());

        // Upload atom data
        ComputeBufferManager.Instance.ProteinAtomPositions.SetData(AtomPositions.ToArray());
        ComputeBufferManager.Instance.IngredientAtomCount.SetData(IngredientAtomCount.ToArray());
        ComputeBufferManager.Instance.IngredientAtomStart.SetData(IngredientAtomStart.ToArray());

        // Upload cluster data
        ComputeBufferManager.Instance.ProteinClusterPositions.SetData(ClusterPositions.ToArray());
        ComputeBufferManager.Instance.IngredientClusterCount.SetData(IngredientClusterCount.ToArray());
        ComputeBufferManager.Instance.IngredientClusterStart.SetData(IngredientClusterStart.ToArray());

        // Upload lipid data
        ComputeBufferManager.Instance.LipidAtomPositions.SetData(LipidAtomPositions.ToArray());
        ComputeBufferManager.Instance.LipidInstancePositions.SetData(LipidInstancePositions.ToArray());
        ComputeBufferManager.Instance.LipidSphereBatchInfos.SetData(LipidSphereBatchInfos.ToArray());

        // Upload Dna data
        ComputeBufferManager.Instance.DnaAtoms.SetData(DnaAtoms.ToArray());
        ComputeBufferManager.Instance.DnaControlPoints.SetData(DnaControlPoints.ToArray());
    }
    
    public void UploadSceneData()
    {
        ComputeBufferManager.Instance.InstanceTypes.SetData(InstanceTypes.ToArray());
        ComputeBufferManager.Instance.InstanceStates.SetData(InstanceStates.ToArray());
        ComputeBufferManager.Instance.InstancePositions.SetData(InstancePositions.ToArray());
        ComputeBufferManager.Instance.InstanceRotations.SetData(InstanceRotations.ToArray());
    }

    public void UploadIngredientToggleData()
    {
        ComputeBufferManager.Instance.IngredientToggleFlags.SetData(IngredientToggleFlags.ToArray());
    }  

}
