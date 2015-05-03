using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

// All elements must be either private or set to nonserialze otherwise the value will be carried after each reload
// Eventually the scene data could be serialized and automatically reloaded, must check the serialization cost first

enum InstanceState
{
    Null = -1,           // Molecule will not be displayed
    Normal = 0,          // Molecule will be displayed with normal color
    Highlighted = 1      // Molecule will be displayed with highlighted color
};


[ExecuteInEditMode]
public class SceneManager : MonoBehaviour
{
    public const int NumInstancesMax = 25000;
    public const int NumSubInstancesMax = 250000;
    
    public const int NumIngredientsMax = 10000;
    public const int NumIngredientsAtomMax = 15000000;
    
    public const int SubInstanceAtomSizeMax = 4096;

    //*****//

    public int NumInstances;
    public int NumSubInstances;

    // Scene data
    public int[] InstancesTypes = new int[NumInstancesMax];
    public int[] InstancesStates = new int[NumInstancesMax];
    public Vector4[] InstancesPositions = new Vector4[NumInstancesMax];
    public Vector4[] InstancesRotations = new Vector4[NumInstancesMax];
    public Vector4[] SubInstancesPositions = new Vector4[NumSubInstancesMax];
    public Vector4[] SubInstancesInformations = new Vector4[NumSubInstancesMax];

    // Ingredients data
    public List<int> IngredientsToggle = new List<int>();
    public List<int> IngredientsAtomCount = new List<int>();
    public List<int> IngredientsAtomStart = new List<int>();
    public List<string> IngredientsNames = new List<string>();
    public List<Vector4> IngredientsColors = new List<Vector4>();
    public List<Vector4> IngredientsAtomPdbData = new List<Vector4>();
    public List<float> IngredientsBoundingSphereRadius = new List<float>();

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

    private void AddIngredientAtomData(string name, ICollection<Vector4> atoms, float boundingSphereRadius)
    {
        if (IngredientsNames.Contains(name)) return;

        IngredientsToggle.Add(1);
        IngredientsNames.Add(name);
        IngredientsColors.Add(Helper.GetRandomColor());
        IngredientsBoundingSphereRadius.Add(boundingSphereRadius);

        IngredientsAtomCount.Add(atoms.Count);
        IngredientsAtomStart.Add(IngredientsAtomPdbData.Count);
        IngredientsAtomPdbData.AddRange(atoms);
    }

    public void AddIngredientPdb(string pdbName, bool center = true)
    {
        if (IngredientsNames.Contains(pdbName)) return;

        var atoms = PdbLoader.ReadPdbFile(pdbName, center);
        var bounds = PdbLoader.GetBounds(atoms);
        var boundingSphereRadius = Vector3.Magnitude(bounds.extents);

        AddIngredientAtomData(pdbName, atoms, boundingSphereRadius);
    }

    // Load cached binary version of pdb files
    //public void AddIngredientBin(string fileName)
    //{
    //    if (IngredientsNames.Contains(fileName)) return;

    //    var atoms = PdbLoader.ReadBinFile(fileName);
    //    var bounds = PdbLoader.GetBounds(atoms);
    //    var boundingSphereRadius = Vector3.Magnitude(bounds.extents);

    //    AddIngredientAtomData(fileName, atoms, boundingSphereRadius);
    //}

    public void AddIngredientInstance(string name, Vector3 position, Quaternion rotation)
    {
        if (!IngredientsNames.Contains(name))
        {
            AddIngredientPdb(name);
            //throw new Exception("Ingredient type do not exists");
        }

        var type = IngredientsNames.IndexOf(name);

        InstancesTypes[NumInstances] = type;
        InstancesStates[NumInstances] = (int)InstanceState.Normal;
        InstancesPositions[NumInstances] = position;
        InstancesRotations[NumInstances] = Helper.QuanternionToVector4(rotation);

        var numSubInstances = (int)Mathf.Ceil((float)IngredientsAtomCount[type] / (float)SubInstanceAtomSizeMax);

        var subInstanceAtomStart = 0;
        var subInstanceAtomCount = (int)Mathf.Ceil((float)IngredientsAtomCount[type] / (float)numSubInstances);
        
        for (int i = 0; i < numSubInstances; i++)
        {
            SubInstancesPositions[NumSubInstances] = Vector3.zero;
            SubInstancesInformations[NumSubInstances] = new Vector4(i, NumInstances, subInstanceAtomCount, subInstanceAtomStart);

            NumSubInstances++;
            subInstanceAtomStart += subInstanceAtomCount;
        }

        NumInstances++;
    }
    
    public void AddMembrane(string filePath, Vector3 position, Quaternion rotation)
    {
        var lipidCount = 0;
        var lipidIndex = 1;
        var lipidAtomStart = 0;
        
        var lipidAtoms = new List<Vector4>();
        var membraneAtoms = new List<Vector4>();

        var membraneData = Helper.ReadBytesAsFloats(filePath);
        
        for (var i = 0; i < membraneData.Length; i += 5)
        {
            if ((int)membraneData[i + 4] != lipidIndex)
            {
                var bounds = PdbLoader.GetBounds(lipidAtoms);
                var center = new Vector4(bounds.center.x, bounds.center.y, bounds.center.z, 0);
                for (var j = 0; j < lipidAtoms.Count; j++) lipidAtoms[j] -= center;

                SubInstancesPositions[NumSubInstances] = bounds.center;
                SubInstancesInformations[NumSubInstances] = new Vector4(lipidCount, NumInstances, lipidAtoms.Count, lipidAtomStart);
                NumSubInstances++;

                lipidAtomStart += lipidAtoms.Count;

                lipidCount ++;
                lipidIndex = (int)membraneData[i + 4];

                membraneAtoms.AddRange(lipidAtoms);
                lipidAtoms.Clear();
            }

            lipidAtoms.Add(new Vector4(membraneData[i], membraneData[i + 1], membraneData[i + 2], membraneData[i + 3]));
        }
        
        AddIngredientAtomData("membrane", membraneAtoms, 0);

        int type = IngredientsNames.IndexOf("membrane");
        InstancesTypes[NumInstances] = type;
        InstancesStates[NumInstances] = (int)InstanceState.Normal;
        InstancesPositions[NumInstances] = position;
        InstancesRotations[NumInstances] = Helper.QuanternionToVector4(rotation);
        
        NumInstances ++;
    }

    //public void AddBioAssemblyInstance(string pdbName, Vector3 position, Quaternion rotation)
    //{
    //    // If molecule type is not present => add new type to the system
    //    //if (!HasBioAssemblyType(pdbName))
    //    //    AddBioAssemblyType(pdbName);

    //    int bioAssemblyType = BioAssemblyNames.IndexOf(pdbName);

    //    for (int i = BioUnitStart[bioAssemblyType]; i < (BioUnitStart[bioAssemblyType] + BioUnitCount[bioAssemblyType]); i++)
    //    {
    //        var matrix = BioUnitData[i];
    //        Vector3 p = new Vector3(393.230f, 1016.300f, 385.017f);
    //        p = matrix.MultiplyPoint(p);

    //        AddMoleculeInstance(pdbName, p, Quaternion.identity);
    //    }
    //}

    //public void AddBioAssemblyType(string pdbName)
    //{
    //    // If molecule type is not present => add new type to the system
    //    //if (HasMoleculeType(pdbName))
    //        //throw new Exception("Biological Assembly type already exists");

    //    var atoms = new List<Vector4>();
    //    var matrices = new List<Matrix4x4>();
    //    PdbLoader.ReadBioAssemblyFile(pdbName, out atoms, out matrices);

    //    var bioAssemblyAtoms = new List<Vector4>();

    //    foreach (var matrix in matrices)
    //    {
    //        foreach (var atom in atoms)
    //        {
    //            Vector3 pos = atom;
    //            pos = matrix.MultiplyPoint(pos);
    //            bioAssemblyAtoms.Add(new Vector4(pos.x, pos.y, pos.z, atom.w));
    //        }
    //    }

    //    var color = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));

    //    MoleculeColors.Add(color);
    //    MoleculeNames.Add(pdbName);
    //    ToggleIngredients.Add(1);

    //    AtomCount.Add(bioAssemblyAtoms.Count);
    //    AtomStart.Add(AtomDataPdb.Count);
    //    AtomDataPdb.AddRange(bioAssemblyAtoms);
    //}

    private int _selectedInstance = -1;
    private Vector3 _savedPosition;
    private Quaternion _savedRotation;
    private GameObject _selectedGameObject = null;

    public void SetSelectedInstance(int selectedInstance)
    {
        var uploadData = false;

        // Reset current selected instance
        if (_selectedInstance != -1)
        {
            InstancesStates[_selectedInstance] = (int)InstanceState.Normal;
            _selectedInstance = -1;

            uploadData = true;
        }

        // If selected instance is valid
        if (selectedInstance != 16777215)
        {
            _selectedInstance = selectedInstance;
            InstancesStates[selectedInstance] = (int)InstanceState.Highlighted;

            _selectedGameObject = GameObject.Find("Selected Element");
            if (_selectedGameObject == null) _selectedGameObject = new GameObject("Selected Element");

            _savedPosition = _selectedGameObject.transform.position;
            _savedRotation = _selectedGameObject.transform.rotation;

            _selectedGameObject.transform.position = InstancesPositions[selectedInstance];
            _selectedGameObject.transform.rotation = Helper.Vector4ToQuaternion(InstancesRotations[selectedInstance]);

            uploadData = true;
        }

        if (uploadData)
        {
            ComputeBufferManager.Instance.InstancesStates.SetData(InstancesStates);
        }
    }

    private void UpdateSelectedInstance()
    {
        if (_selectedInstance == -1) return;

        if (_savedPosition != _selectedGameObject.transform.position || _savedRotation != _selectedGameObject.transform.rotation)
        {
            Debug.Log("Selected instance transform changed");

            InstancesPositions[_selectedInstance] = _selectedGameObject.transform.position;
            InstancesRotations[_selectedInstance] = Helper.QuanternionToVector4(_selectedGameObject.transform.rotation);

            ComputeBufferManager.Instance.InstancesPositions.SetData(InstancesPositions);
            ComputeBufferManager.Instance.InstancesRotations.SetData(InstancesRotations);

            _savedPosition = _selectedGameObject.transform.position;
            _savedRotation = _selectedGameObject.transform.rotation;
        }
    }

    void Update()
    {
        UpdateSelectedInstance();
    }

    // Scene data gets serialized on each reload, to clear the scene call this function
    public void ClearScene()
    {
        Debug.Log("Clear scene");

        NumInstances = 0;
        NumSubInstances = 0;

        // Clear ingredient data
        IngredientsNames.Clear();
        IngredientsToggle.Clear();
        IngredientsColors.Clear();
        IngredientsAtomCount.Clear();
        IngredientsAtomStart.Clear();
        IngredientsAtomPdbData.Clear();
        
        // Clear scene data
        Array.Clear(InstancesTypes, 0, NumInstancesMax);
        Array.Clear(InstancesStates, 0, NumInstancesMax);
        Array.Clear(InstancesPositions, 0, NumInstancesMax);
        Array.Clear(InstancesRotations, 0, NumInstancesMax);
        Array.Clear(SubInstancesPositions, 0, NumSubInstancesMax);
        Array.Clear(SubInstancesInformations, 0, NumSubInstancesMax);
    }

    public void UploadAllData()
    {
        UploadSceneData();
        UploadIngredientData();
        UploadIngredientColorData();
        UploadIngredientToggleData();
    }

    // Todo: Only upload parts of the arrays that are filled, this should save bandwidth and loading time

    public void UploadSceneData()
    {
        ComputeBufferManager.Instance.InstancesTypes.SetData(InstancesTypes);
        ComputeBufferManager.Instance.InstancesStates.SetData(InstancesStates);
        ComputeBufferManager.Instance.InstancesPositions.SetData(InstancesPositions);
        ComputeBufferManager.Instance.InstancesRotations.SetData(InstancesRotations);
        ComputeBufferManager.Instance.SubInstancesPositions.SetData(SubInstancesPositions);
        ComputeBufferManager.Instance.SubInstancesInformations.SetData(SubInstancesInformations);
    }

    public void UploadIngredientData()
    {
        ComputeBufferManager.Instance.IngredientsAtomCount.SetData(IngredientsAtomCount.ToArray());
        ComputeBufferManager.Instance.IngredientsAtomStart.SetData(IngredientsAtomStart.ToArray());
        ComputeBufferManager.Instance.IngredientsAtomPdbData.SetData(IngredientsAtomPdbData.ToArray());
        ComputeBufferManager.Instance.IngredientsBoundingSphereRadius.SetData(IngredientsBoundingSphereRadius.ToArray());
    }

    public void UploadIngredientColorData()
    {
        ComputeBufferManager.Instance.IngredientsColors.SetData(IngredientsColors.ToArray());
    }

    public void UploadIngredientToggleData()
    {
        ComputeBufferManager.Instance.IngredientsToggle.SetData(IngredientsToggle.ToArray());
    }
}
