using System;
using System.Collections.Generic;
using System.Linq;
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
    //public List<int> UnitInstanceStart = new List<int>();
    //public List<int> UnitInstanceCount = new List<int>();
    public List<Vector4> UnitInstancePositions = new List<Vector4>();

    // Scene data
    public List<Vector4> InstanceInfos = new List<Vector4>();
    public List<Vector4> ProteinInstancePositions = new List<Vector4>();
    public List<Vector4> InstanceRotations = new List<Vector4>();

    // Ingredient data
    public List<int> IngredientToggleFlags = new List<int>();
    public List<string> IngredientNames = new List<string>();
    public List<Vector4> IngredientsColors = new List<Vector4>();
    public List<float> IngredientBoundingSpheres = new List<float>();
    
    // Atom data
    public List<Vector4> ProteinAtomPositions = new List<Vector4>();
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
    public List<Vector4> DnaControlPointsPositions = new List<Vector4>();
    public List<Vector4> DnaControlPointsNormals = new List<Vector4>();
    
    public int NumProteinInstances
    {
        get { return ProteinInstancePositions.Count; }
    }

    public int NumLipidInstances
    {
        get { return LipidInstancePositions.Count; }
    }

    public int NumDnaControlPoints
    {
        get { return DnaControlPointsPositions.Count; }
    }

    public int NumDnaSegments
    {
        get { return Math.Max(DnaControlPointsPositions.Count - 1, 0); }
    }
    
    public int NumLodLevels = 0;

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

    public int UnitAtomCount = 0;
    public int UnitProteinInstanceCount = 0;
    public int UnitLipidInstanceCount = 0;
    public Int64 GlobalAtomCount = 0;

    public void SetUnitInstanceCount()
    {
        UnitLipidInstanceCount = LipidInstancePositions.Count;
        UnitProteinInstanceCount = ProteinInstancePositions.Count;
        
        GlobalAtomCount = UnitAtomCount;

        //Debug.Log(UnitLipidInstanceCount);
        //Debug.Log(UnitProteinInstanceCount);
    }

    public void AddUnitInstance(Vector3 offset)
    {
       //Debug.Log(offset);

       for (int i = 0; i < UnitProteinInstanceCount; i++)
       {
           var info = InstanceInfos[i];
           //info.w = UnitInstancePositions.Count;

           var position = ProteinInstancePositions[i];
           position += new Vector4(offset.x, offset.y, offset.z, 0);

           var rotation = InstanceRotations[i];

           InstanceInfos.Add(info);
           ProteinInstancePositions.Add(position);
           InstanceRotations.Add(rotation);
       }

       for (int i = 0; i < UnitLipidInstanceCount; i++)
       {
           var position = LipidInstancePositions[i];
           position += new Vector4(offset.x, offset.y, offset.z, 0);

           var batchInfo = LipidSphereBatchInfos[i];

           LipidInstancePositions.Add(position);
           LipidSphereBatchInfos.Add(batchInfo);
       }

       GlobalAtomCount += UnitAtomCount;
    }
    
    public void AddIngredient(string ingredientName, Bounds bounds, List<Vector4> atoms, List<List<Vector4>> clusters = null) 
    {
        if (IngredientNames.Contains(ingredientName)) return;

        if (NumLodLevels != 0 && NumLodLevels != clusters.Count)
            throw new Exception("Uneven cluster levels number: " + ingredientName);
        
        IngredientToggleFlags.Add(1);
        IngredientNames.Add(ingredientName);
        IngredientsColors.Add(Helper.GetRandomColor());
        IngredientBoundingSpheres.Add(Vector3.Magnitude(bounds.extents));

        IngredientAtomCount.Add(atoms.Count);
        IngredientAtomStart.Add(ProteinAtomPositions.Count);
        ProteinAtomPositions.AddRange(atoms);

        var clusterCount = new Vector4(0,0,0,0);
        var clusterStart = new Vector4(0,0,0,0);
        
        if (clusters != null)
        {
            NumLodLevels = clusters.Count;

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

    public void AddIngredientInstance(string ingredientName, Vector3 position, Quaternion rotation, int unitId = 0)
    {
        if (!IngredientNames.Contains(ingredientName))
        {
            throw new Exception("Ingredient type do not exists");
        }

        var ingredientId = IngredientNames.IndexOf(ingredientName);

        Vector4 instancePosition = position;
        instancePosition.w = IngredientBoundingSpheres[ingredientId];

        InstanceInfos.Add(new Vector4(ingredientId, (int)InstanceState.Normal, unitId));
        ProteinInstancePositions.Add(instancePosition);
        InstanceRotations.Add(Helper.QuanternionToVector4(rotation));

        UnitAtomCount += IngredientAtomCount[ingredientId];
    }

    public void LoadMembrane(string filePath, Vector3 position, Quaternion rotation)
    {
        if (LipidAtomPositions.Count != 0)
        {
            throw new Exception("Membrane already added");
        }

        var batchCount = 0;

        var lipidIndex = 1;
        var lipidAtomStart = 0;
        var sphereBatch = new List<Vector4>();
        var membraneAtoms = new List<Vector4>();
        var membraneData = Helper.ReadBytesAsFloats(filePath);

        var firstAtom = new Vector4(membraneData[0], membraneData[1], membraneData[2], PdbLoader.AtomRadii[(int)membraneData[3]]);
        sphereBatch.Add(firstAtom);
        var lastAtom = firstAtom;

        for (var i = 5; i < membraneData.Length; i += 5)
        {
            var currentAtom = new Vector4(membraneData[i], membraneData[i + 1], membraneData[i + 2], PdbLoader.AtomRadii[(int)membraneData[i + 3]]);
            var distance = Vector3.Distance(currentAtom, lastAtom);

            if (distance > 50 || i >= membraneData.Length -5)
            {
                var bounds = PdbLoader.GetBounds(sphereBatch);
                var center = new Vector4(bounds.center.x, bounds.center.y, bounds.center.z, 0);
                for (var j = 0; j < sphereBatch.Count; j++) sphereBatch[j] -= center;

                Vector4 batchPosition = position + bounds.center;
                batchPosition.w = Vector3.Magnitude(bounds.extents);

                LipidInstancePositions.Add(batchPosition);
                LipidSphereBatchInfos.Add(new Vector4(sphereBatch.Count, lipidAtomStart, 0, 0));

                batchCount++;
                //Debug.Log(sphereBatch.Count);

                lipidAtomStart += sphereBatch.Count;
                lipidIndex = (int)membraneData[i + 4];

                membraneAtoms.AddRange(sphereBatch);
                sphereBatch.Clear();
            }

            sphereBatch.Add(currentAtom);
            lastAtom = currentAtom;
        }

        int a = 0;
        Debug.Log(batchCount);

        LipidAtomPositions.AddRange(membraneAtoms);
        UnitAtomCount += LipidAtomPositions.Count;
    }

    //public void LoadMembrane(string filePath, Vector3 position, Quaternion rotation)
    //{
    //    if (LipidAtomPositions.Count != 0)
    //    {
    //        throw new Exception("Membrane already added");
    //    }

    //    var lipidIndex = 1;
    //    var lipidAtomStart = 0;
    //    var lipidAtoms = new List<Vector4>();
    //    int ingredientId = IngredientNames.Count;
    //    var membraneAtoms = new List<Vector4>();
    //    var membraneData = Helper.ReadBytesAsFloats(filePath);

    //    for (var i = 0; i < membraneData.Length; i += 5)
    //    {
    //        if ((int)membraneData[i + 4] != lipidIndex)
    //        {
    //            var bounds = PdbLoader.GetBounds(lipidAtoms);
    //            var center = new Vector4(bounds.center.x, bounds.center.y, bounds.center.z, 0);
    //            for (var j = 0; j < lipidAtoms.Count; j++) lipidAtoms[j] -= center;

    //            Vector4 batchPosition = position + bounds.center;
    //            batchPosition.w = Vector3.Magnitude(bounds.extents);
    //            LipidSphereBatchInfos.Add(new Vector4(lipidAtoms.Count, lipidAtomStart, 0, 0));
    //            LipidInstancePositions.Add(batchPosition);

    //            lipidAtomStart += lipidAtoms.Count;
    //            lipidIndex = (int)membraneData[i + 4];

    //            membraneAtoms.AddRange(lipidAtoms);
    //            lipidAtoms.Clear();
    //        }

    //        lipidAtoms.Add(new Vector4(membraneData[i], membraneData[i + 1], membraneData[i + 2], PdbLoader.AtomRadii[(int)membraneData[i + 3]]));
    //    }

    //    LipidAtomPositions.AddRange(membraneAtoms);

    //    UnitAtomCount += LipidAtomPositions.Count;
    //}

    public void AddNucleicAcids()
    {
        var atomSpheres = PdbLoader.ReadAtomSpheres(PdbLoader.DefaultPdbDirectory + "b-basepair.pdb");
        DnaAtoms.AddRange(atomSpheres);
    }

    private List<Vector4> NormalizeControlPoints(List<Vector4> controlPoints)
    {
        var normalizedControlPoints = new List<Vector4>();
        normalizedControlPoints.Add(controlPoints[0]);
        normalizedControlPoints.Add(controlPoints[1]);

        var currentPointId = 1;
        var currentPosition = controlPoints[currentPointId];

        float distance = DisplaySettings.Instance.DistanceContraint;
        float lerpValue = 0.0f;

        // Normalize the distance between control points
        while (true)
        {
            if (currentPointId + 2 >= controlPoints.Count) break;
            //if (currentPointId + 2 >= 100) break;

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
                    normalizedControlPoints.Add(candidate);
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

        return normalizedControlPoints;
    }

    public List<Vector4> GetSmoothNormals(List<Vector4> controlPoints)
    {
        var smoothNormals = new List<Vector4>();
        var crossDirection = Vector3.up;

        var p0 = controlPoints[0];
        var p1 = controlPoints[1];
        var p2 = controlPoints[2];

        smoothNormals.Add(Vector3.Normalize(Vector3.Cross(p0 - p1, p2 - p1)));

        for (int i = 2; i < controlPoints.Count - 1; i++)
        {
            p0 = controlPoints[i - 1];
            p1 = controlPoints[i];
            p2 = controlPoints[i + 1];

            var t = Vector3.Normalize(p2 - p0);
            var b = Vector3.Normalize(Vector3.Cross(t, smoothNormals.Last()));
            var n = -Vector3.Normalize(Vector3.Cross(t, b));

            smoothNormals.Add(n);
        }

        return smoothNormals;
    }

    public void AddDNAPath(List<Vector4> path)
    {
        var controlPoints = NormalizeControlPoints(path);
        var normals = GetSmoothNormals(controlPoints);

        for (int i = 0; i < controlPoints.Count; i++)
        {
            //var stopFlag = (i%25 == 0) ? 5 : DnaControlPointsPositions.Count;   // To debug
            var stopFlag = DnaControlPointsPositions.Count;
            controlPoints[i] = new Vector4(controlPoints[i].x, controlPoints[i].y, controlPoints[i].z, stopFlag);
        }
        
        DnaControlPointsNormals.AddRange(normals);
        DnaControlPointsPositions.AddRange(controlPoints);

        Debug.Log(controlPoints.Count);
    }

    //--------------------------------------------------------------------------------------
    // Object picking

    public int SelectedInstance = -1;
    public GameObject SelectedGameObject;

    public Vector3 _savedPosition;
    public Quaternion _savedRotation;

    public void SetSelectedElement(int elementId)
    {
        Debug.Log("Selected element id: " + elementId);

        if (elementId >= ProteinInstancePositions.Count) return;

        // If element id is different than the currently selected element
        if (SelectedInstance != elementId)
        {
            // if the currently selected instance was greater than -1 we reset the states
            if (SelectedInstance > -1)
            {
                //Debug.Log("Reset state");
                InstanceInfos[SelectedInstance] = new Vector4(InstanceInfos[SelectedInstance].x, (int) InstanceState.Normal, InstanceInfos[SelectedInstance].z);
            }

            // if new selected element is greater than one update set and set position to game object
            if (elementId > -1)
            {
                //Debug.Log("Update state");
                InstanceInfos[elementId] = new Vector4(InstanceInfos[elementId].x, (int)InstanceState.Highlighted, InstanceInfos[elementId].z);

                SelectedGameObject = GameObject.Find("Selected Element") ?? new GameObject("Selected Element");

                SelectedGameObject.transform.position = ProteinInstancePositions[elementId] * DisplaySettings.Instance.Scale;
                SelectedGameObject.transform.rotation = Helper.Vector4ToQuaternion(InstanceRotations[elementId]);

                _savedPosition = SelectedGameObject.transform.position;
                _savedRotation = SelectedGameObject.transform.rotation;
            }

            SelectedInstance = elementId;
            ComputeBufferManager.Instance.ProteinInstanceInfos.SetData(InstanceInfos.ToArray());
        }
    }

    private void UpdateSelectedInstance()
    {
        if (SelectedInstance == -1) return;

        if (_savedPosition != SelectedGameObject.transform.position || _savedRotation != SelectedGameObject.transform.rotation)
        {
            Debug.Log("Selected instance transform changed");

            ProteinInstancePositions[SelectedInstance] = SelectedGameObject.transform.position / DisplaySettings.Instance.Scale;
            InstanceRotations[SelectedInstance] = Helper.QuanternionToVector4(SelectedGameObject.transform.rotation);

            ComputeBufferManager.Instance.ProteinInstancePositions.SetData(ProteinInstancePositions.ToArray());
            ComputeBufferManager.Instance.ProteinInstanceRotations.SetData(InstanceRotations.ToArray());

            _savedPosition = SelectedGameObject.transform.position;
            _savedRotation = SelectedGameObject.transform.rotation;
        }
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

        NumLodLevels = 0;
        UnitAtomCount = 0;
        GlobalAtomCount = 0;
        UnitProteinInstanceCount = 0;

        SelectedInstance = -1;

        // Clear scene data
        InstanceInfos.Clear();
        ProteinInstancePositions.Clear();
        InstanceRotations.Clear();

        // Clear ingredient data
        IngredientNames.Clear();
        IngredientsColors.Clear();
        IngredientToggleFlags.Clear();
        IngredientBoundingSpheres.Clear();
        
        // Clear atom data
        ProteinAtomPositions.Clear();
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
        DnaControlPointsPositions.Clear();
        DnaControlPointsNormals.Clear();

        UnitInstancePositions.Clear();
    }

    public void UploadAllData()
    {
        Debug.Log("Init GPU buffer and upload all the data to GPU");
        
        ComputeBufferManager.Instance.NumProteinAtomMax = ProteinAtomPositions.Count + 1;
        ComputeBufferManager.Instance.NumIngredientsMax = IngredientNames.Count + 1;
        ComputeBufferManager.Instance.NumProteinInstancesMax = ProteinInstancePositions.Count + 1;
        ComputeBufferManager.Instance.NumProteinSphereBatchesMax = ProteinInstancePositions.Count * 2 + 1; ;
        ComputeBufferManager.Instance.NumLipidAtomMax = LipidAtomPositions.Count + 1;
        ComputeBufferManager.Instance.NumLipidInstancesMax = LipidInstancePositions.Count + 1;
        ComputeBufferManager.Instance.NumDnaAtomsMax = DnaAtoms.Count + 1;
        ComputeBufferManager.Instance.NumDnaControlPointsMax = DnaControlPointsPositions.Count + 1;
        
        ComputeBufferManager.Instance.InitBuffers();

        // Upload scene data
        ComputeBufferManager.Instance.ProteinInstanceInfos.SetData(InstanceInfos.ToArray());
        ComputeBufferManager.Instance.ProteinInstancePositions.SetData(ProteinInstancePositions.ToArray());
        ComputeBufferManager.Instance.ProteinInstanceRotations.SetData(InstanceRotations.ToArray());

        // Upload ingredient data
        ComputeBufferManager.Instance.ProteinColors.SetData(IngredientsColors.ToArray());
        ComputeBufferManager.Instance.ProteinVisibilityFlags.SetData(IngredientToggleFlags.ToArray());

        // Upload atom data
        ComputeBufferManager.Instance.ProteinAtomPositions.SetData(ProteinAtomPositions.ToArray());
        ComputeBufferManager.Instance.ProteinAtomCount.SetData(IngredientAtomCount.ToArray());
        ComputeBufferManager.Instance.ProteinAtomStart.SetData(IngredientAtomStart.ToArray());

        // Upload cluster data
        ComputeBufferManager.Instance.ProteinClusterPositions.SetData(ClusterPositions.ToArray());
        ComputeBufferManager.Instance.ProteinClusterCount.SetData(IngredientClusterCount.ToArray());
        ComputeBufferManager.Instance.ProteinClusterStart.SetData(IngredientClusterStart.ToArray());

        // Upload lipid data
        ComputeBufferManager.Instance.LipidAtomPositions.SetData(LipidAtomPositions.ToArray());
        ComputeBufferManager.Instance.LipidInstancePositions.SetData(LipidInstancePositions.ToArray());
        ComputeBufferManager.Instance.LipidSphereBatchInfos.SetData(LipidSphereBatchInfos.ToArray());

        // Upload Dna data
        ComputeBufferManager.Instance.DnaAtoms.SetData(DnaAtoms.ToArray());
        ComputeBufferManager.Instance.DnaControlPointsPositions.SetData(DnaControlPointsPositions.ToArray());
        ComputeBufferManager.Instance.DnaControlPointsNormals.SetData(DnaControlPointsNormals.ToArray());

        // Make sure that the renderer has been created
        //var a = Renderer.Instance;
    }
    
    public void UploadSceneData()
    {
        ComputeBufferManager.Instance.ProteinInstanceInfos.SetData(InstanceInfos.ToArray());
        ComputeBufferManager.Instance.ProteinInstancePositions.SetData(ProteinInstancePositions.ToArray());
        ComputeBufferManager.Instance.ProteinInstanceRotations.SetData(InstanceRotations.ToArray());
    }

    public void UploadIngredientToggleData()
    {
        var fluoFlags = new List<int>();

        int fluoCount = 0;
        foreach (var flag in IngredientToggleFlags)
        {
            if (flag == 1 && fluoCount < 4)
            {
                fluoCount++;
                fluoFlags.Add(fluoCount);
            }
            else
            {
                fluoFlags.Add(0);
            }
        }

        ComputeBufferManager.Instance.ProteinVisibilityFlags.SetData(IngredientToggleFlags.ToArray());
        ComputeBufferManager.Instance.ProteinFluorescenceFlags.SetData(fluoFlags.ToArray());
    }  

}
