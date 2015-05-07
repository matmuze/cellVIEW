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
    public ComputeShader BrownianMotionCS;

    //*****//

    public const int NumAtomMax = 10000000;         // Used for GPU buffer memory allocation
    public const int NumInstancesMax = 300000;      // Used for GPU buffer memory allocation
    public const int NumSubInstancesMax = 400000;   // Used for GPU buffer memory allocation
    public const int NumIngredientsMax = 1000;      // Used for GPU buffer memory allocation
    
    public const int SubInstanceAtomSizeMax = 4096;

    //*****//

    [HideInInspector] public int NumInstances;
    [HideInInspector] public int NumSubInstances;
    [HideInInspector] public int MembraneSubInstanceCount;
    [HideInInspector] public int MembraneSubInstanceStart;
    [HideInInspector] public int ProteinsSubInstanceCount;
    [HideInInspector] public int ProteinsSubInstanceStart;

    // Atom data
    [HideInInspector] public List<Vector4> AtomPositions = new List<Vector4>();

    // Scene data, maybe move this to list for faster serialization or resize array to fit data set
    [HideInInspector] public List<int> InstanceTypes = new List<int>();
    [HideInInspector] public List<int> InstanceStates = new List<int>();
    [HideInInspector] public List<Vector4> InstancePositions = new List<Vector4>();
    [HideInInspector] public List<Vector4> InstanceRotations = new List<Vector4>();
    [HideInInspector] public List<Vector4> SubInstanceInformations = new List<Vector4>();

    // Ingredient data
    [HideInInspector] public List<string> IngredientNames = new List<string>();
    [HideInInspector] public List<Vector4> IngredientsColors = new List<Vector4>();

    //this could fit in a float4...
    [HideInInspector] public List<int> IngredientAtomCount = new List<int>();
    [HideInInspector] public List<int> IngredientAtomStart = new List<int>();
    [HideInInspector] public List<int> IngredientToggleFlags = new List<int>();
    [HideInInspector] public List<float> IngredientBoundingSpheres = new List<float>();

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

                go = new GameObject("_SceneManager") {hideFlags = HideFlags.None};
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

    public void AddIngredient(string name, ICollection<Vector4> atoms, Bounds bounds)
    {
        if (IngredientNames.Contains(name)) return;

        IngredientToggleFlags.Add(1);
        IngredientNames.Add(name);
        IngredientsColors.Add(Helper.GetRandomColor());
        IngredientBoundingSpheres.Add(Vector3.Magnitude(bounds.extents));

        IngredientAtomCount.Add(atoms.Count);
        IngredientAtomStart.Add(AtomPositions.Count);
        AtomPositions.AddRange(atoms);
    }

    public void AddIngredientInstance(string name, Vector3 position, Quaternion rotation)
    {
        if (!IngredientNames.Contains(name))
        {
            throw new Exception("Ingredient type do not exists");
        }

        var ingredientId = IngredientNames.IndexOf(name);

        InstanceTypes.Add(ingredientId);
        InstanceStates.Add((int)InstanceState.Normal);
        InstancePositions.Add(position);
        InstanceRotations.Add(Helper.QuanternionToVector4(rotation));

        var numSubInstances = (int)Mathf.Ceil((float)IngredientAtomCount[ingredientId] / (float)SubInstanceAtomSizeMax);

        var subInstanceAtomStart = 0;
        var subInstanceAtomCount = (int)Mathf.Ceil((float)IngredientAtomCount[ingredientId] / (float)numSubInstances);
        
        for (int i = 0; i < numSubInstances; i++)
        {
            SubInstanceInformations.Add(new Vector4(NumInstances, subInstanceAtomCount, subInstanceAtomStart,
                IngredientBoundingSpheres[ingredientId]));

            NumSubInstances++;
            subInstanceAtomStart += subInstanceAtomCount;
        }

        NumInstances++;

        ProteinsSubInstanceCount = NumSubInstances;
    }

    public void AddMembrane(string filePath, Vector3 position, Quaternion rotation)
    {
        if (IngredientNames.Contains("membrane"))
        {
            throw new Exception("Membrane already added");
        }

        var lipidIndex = 1;
        var lipidAtomStart = 0;
        var lipidAtoms = new List<Vector4>();
        int ingredientId = IngredientNames.Count;
        var membraneAtoms = new List<Vector4>();
        var membraneData = Helper.ReadBytesAsFloats(filePath);

        MembraneSubInstanceStart = ProteinsSubInstanceCount;

        for (var i = 0; i < membraneData.Length; i += 5)
        {
            if ((int)membraneData[i + 4] != lipidIndex)
            {
                var bounds = PdbLoader.GetBounds(lipidAtoms);
                var center = new Vector4(bounds.center.x, bounds.center.y, bounds.center.z, 0);
                for (var j = 0; j < lipidAtoms.Count; j++) lipidAtoms[j] -= center;

                InstanceTypes.Add(ingredientId);
                InstanceStates.Add((int)InstanceState.Normal);
                InstancePositions.Add(position + bounds.center);
                InstanceRotations.Add(Helper.QuanternionToVector4(rotation));
                SubInstanceInformations.Add(new Vector4(NumInstances, lipidAtoms.Count, lipidAtomStart, Vector3.Magnitude(bounds.extents)));
                    
                NumInstances++;
                NumSubInstances ++;
                lipidAtomStart += lipidAtoms.Count;
                lipidIndex = (int)membraneData[i + 4];

                membraneAtoms.AddRange(lipidAtoms);
                lipidAtoms.Clear();
            }

            lipidAtoms.Add(new Vector4(membraneData[i], membraneData[i + 1], membraneData[i + 2], membraneData[i + 3]));
        }

        MembraneSubInstanceCount = NumSubInstances - ProteinsSubInstanceCount;
        AddIngredient("membrane", membraneAtoms, new Bounds());
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
        if (!_enableSelection) return;

        var uploadData = false;

        // Reset current selected instance
        if (_selectedInstance != -1)
        {
            InstanceStates[_selectedInstance] = (int)InstanceState.Normal;
            _selectedInstance = -1;

            uploadData = true;
        }

        // If selected instance is valid
        if (selectedInstance != 16777215)
        {
            _selectedInstance = selectedInstance;
            InstanceStates[selectedInstance] = (int)InstanceState.Highlighted;

            _selectedGameObject = GameObject.Find("Selected Element");
            if (_selectedGameObject == null) _selectedGameObject = new GameObject("Selected Element");

            _savedPosition = _selectedGameObject.transform.position;
            _savedRotation = _selectedGameObject.transform.rotation;

            _selectedGameObject.transform.position = InstancePositions[selectedInstance];
            _selectedGameObject.transform.rotation = Helper.Vector4ToQuaternion(InstanceRotations[selectedInstance]);

            uploadData = true;
        }

        if (uploadData)
        {
            ComputeBufferManager.Instance.InstanceStates.SetData(InstanceStates.ToArray());
        }
    }

    private void UpdateSelectedInstance()
    {
        if (_selectedInstance == -1) return;

        if (_savedPosition != _selectedGameObject.transform.position || _savedRotation != _selectedGameObject.transform.rotation)
        {
            Debug.Log("Selected instance transform changed");

            InstancePositions[_selectedInstance] = _selectedGameObject.transform.position;
            InstanceRotations[_selectedInstance] = Helper.QuanternionToVector4(_selectedGameObject.transform.rotation);

            ComputeBufferManager.Instance.InstancePositions.SetData(InstancePositions.ToArray());
            ComputeBufferManager.Instance.InstanceRotations.SetData(InstanceRotations.ToArray());

            _savedPosition = _selectedGameObject.transform.position;
            _savedRotation = _selectedGameObject.transform.rotation;
        }
    }

    //--------------------------------------------------------------------------------------
    // Brownian Motion
    private void SimulateBrownianMotion()
    {
        if (!DisplaySettings.Instance.EnableBrownianMotion) return;
        
        BrownianMotionCS.SetFloat("_Time", Time.time);
        BrownianMotionCS.SetBuffer(0, "_InstancePositions", ComputeBufferManager.Instance.InstancePositions);
        BrownianMotionCS.SetBuffer(0, "_InstanceRotations", ComputeBufferManager.Instance.InstanceRotations);

        BrownianMotionCS.SetBuffer(0, "_InstanceDisplayPositions", ComputeBufferManager.Instance.InstanceDisplayPositions);
        BrownianMotionCS.SetBuffer(0, "_InstanceDisplayRotations", ComputeBufferManager.Instance.InstanceDisplayRotations);
        
        BrownianMotionCS.Dispatch(0, NumInstances, 1, 1);
    }

    //--------------------------------------------------------------------------------------

    void Update()
    {
        UpdateSelectedInstance();
        SimulateBrownianMotion();
    }

    //--------------------------------------------------------------------------------------
    // Misc functions


    // Scene data gets serialized on each reload, to clear the scene call this function
    public void ClearScene()
    {
        Debug.Log("Clear scene");

        NumInstances = 0;
        NumSubInstances = 0;

        // Clear atom data
        AtomPositions.Clear();

        // Clear ingredient data
        IngredientNames.Clear();
        IngredientToggleFlags.Clear();
        IngredientsColors.Clear();
        IngredientAtomCount.Clear();
        IngredientAtomStart.Clear();
        
        // Clear scene data
        InstanceTypes.Clear();
        InstanceStates.Clear();
        InstancePositions.Clear();
        InstanceRotations.Clear();
        SubInstanceInformations.Clear();
    }

    public void UploadAllData()
    {
        UploadSceneData();
        UploadIngredientData();
        UploadIngredientColorData();
        UploadIngredientToggleData();
    }

    public void UploadSceneData()
    {
        ComputeBufferManager.Instance.InstanceTypes.SetData(InstanceTypes.ToArray());
        ComputeBufferManager.Instance.InstanceStates.SetData(InstanceStates.ToArray());
        ComputeBufferManager.Instance.InstancePositions.SetData(InstancePositions.ToArray());
        ComputeBufferManager.Instance.InstanceRotations.SetData(InstanceRotations.ToArray());
        ComputeBufferManager.Instance.SubInstanceInformations.SetData(SubInstanceInformations.ToArray());
    }

    public void UploadIngredientData()
    {
        ComputeBufferManager.Instance.AtomPositions.SetData(AtomPositions.ToArray());
        ComputeBufferManager.Instance.IngredientAtomCount.SetData(IngredientAtomCount.ToArray());
        ComputeBufferManager.Instance.IngredientAtomStart.SetData(IngredientAtomStart.ToArray());
        ComputeBufferManager.Instance.IngredientBoundingSpheres.SetData(IngredientBoundingSpheres.ToArray());
    }

    public void UploadIngredientColorData()
    {
        ComputeBufferManager.Instance.IngredientColors.SetData(IngredientsColors.ToArray());
    }

    public void UploadIngredientToggleData()
    {
        ComputeBufferManager.Instance.IngredientToggleFlags.SetData(IngredientToggleFlags.ToArray());
    }
}
