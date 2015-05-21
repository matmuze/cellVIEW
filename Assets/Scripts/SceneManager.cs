using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class SceneManager : MonoBehaviour
{
    //*****//
    
    public const int NumDnaControlPointsMax = 1000000;
    public const int NumDnaAtomsMax = 1000;

    // Dna data
    public List<Vector4> DnaAtoms = new List<Vector4>();
    public List<Vector4> DnaControlPoints = new List<Vector4>();

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

    public void LoadDna()
    {
        DnaControlPoints.Add(Vector4.zero);

        for (int i = 0; i < 10000; i++)
        {
            var rand = UnityEngine.Random.onUnitSphere;
            var newc = DnaControlPoints.Last() + new Vector4(rand.x, rand.y, rand.z, 0) * DisplaySettings.Instance.DistanceContraint * 0.5f;
            DnaControlPoints.Add(newc);
        }

        var bounds = PdbLoader.GetBounds(DnaControlPoints);
        PdbLoader.OffsetPoints(ref DnaControlPoints, bounds.center);

        var atoms = PdbLoader.ReadPdbFile(PdbLoader.GetPdbFilePath("b-basepair"));
        var atomBounds = PdbLoader.GetBounds(atoms);
        PdbLoader.OffsetPoints(ref atoms, atomBounds.center);
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
        DnaAtoms.Clear();
        DnaControlPoints.Clear();
    }

    public void UploadAllData()
    {
        // Upload Dna data
        ComputeBufferManager.Instance.DnaAtoms.SetData(DnaAtoms.ToArray());
        ComputeBufferManager.Instance.DnaControlPoints.SetData(DnaControlPoints.ToArray());
    }
}
