using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SimpleJSON;
using Debug = UnityEngine.Debug;

public static class CellPackLoader
{

    public static readonly string ProteinDiretory = Application.dataPath + "/../Data/HIV/proteins/";
    //public static readonly string PdbClustererCmd = Application.dataPath + "/../Misc/PdbClusterer/clusterPdb.exe";

    //public static void ClusterizeProtein(string pdbName)
    //{
    //    var strArg = "-f " + pdbName + ".pdb -c AP -a -100 -b 3";

    //    Debug.Log(strArg);
    //    Debug.Log(pdbName);
    //    Debug.Log(PdbClustererCmd);
    //    Debug.Log(ProteinDiretory);

    //    Process process = new System.Diagnostics.Process();
    //    ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
    //    //startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
    //    startInfo.WorkingDirectory = ProteinDiretory;
    //    startInfo.FileName = PdbClustererCmd;
    //    startInfo.Arguments = strArg;
    //    process.StartInfo = startInfo;
    //    process.Start();

    //    //startInfo.UseShellExecute = false;
    //    //startInfo.RedirectStandardError = true;
    //    //Process someProcess = Process.Start(startInfo);
    //    //string errors = someProcess.StandardError.ReadToEnd();
    //    //Debug.Log("errors: " + errors);

    //    return;
    //}

    //public static void ComputeClusters()
    //{
    //    var cellPackSceneJsonPath = Application.dataPath + "/../Data/HIV/cellPACK/HIV-1_0.1.6-8_mixed_pdb.json";
    //    if (!File.Exists(cellPackSceneJsonPath)) throw new Exception("No file found at: " + cellPackSceneJsonPath);

    //    var resultData = Helper.parseJson(cellPackSceneJsonPath);

    //    //we can traverse the json dictionary and gather ingredient source (PDB,center), sphereTree, instance.geometry if we want.
    //    //the recipe is optional as it will gave more information than just the result file.

    //    int nComp = resultData["compartments"].Count;

    //    Debug.Log("Start pdb cluster batch");

    //    //I dont do the cytoplasme compartments, will do when Blood will be ready
    //    for (int i = 0; i < nComp; i++)
    //    {
    //        //var surfaceIngredients = resultData["compartments"][i]["surface"]["ingredients"];
    //        //for (int j = 0; j < surfaceIngredients.Count; j++)
    //        //{
    //        //    var pdbName = surfaceIngredients[j]["source"]["pdb"].Value.Replace(".pdb", "");
    //        //    var pdbPath = ProteinDiretory + pdbName + ".pdb";

    //        //    if (File.Exists(pdbPath))
    //        //    {
    //        //        ClusterizeProtein(pdbName);
    //        //    }
    //        //}
            
    //        //var interiorIngredients = resultData["compartments"][i]["interior"]["ingredients"];
    //        //for (int j = 0; j < interiorIngredients.Count; j++)
    //        //{
    //        //    var pdbName = interiorIngredients[j]["source"]["pdb"].Value.Replace(".pdb", "");
    //        //    var pdbPath = ProteinDiretory + pdbName + ".pdb";
                
    //        //    if (File.Exists(pdbPath))
    //        //    {
    //        //        ClusterizeProtein(pdbName);
    //        //    }
    //        //}
    //    }
    //}
    
    //public static void AddProteinInstances(string pdbName, bool isSurface, bool center, JSONNode results)
    //{
    //    // Skip this element
    //    if (pdbName.Contains("1PI7_1vpu_biounit") || pdbName.Contains("1f6u")) return;
        
    //    var pdbPath = ProteinDiretory + pdbName + ".pdb";

    //    if (!File.Exists(pdbPath))
    //    {
    //        Debug.Log("Skiping protein: " + pdbName + " because one of its structure file is missing.");
    //        return;
    //    }

    //    // Read atoms data from pdb file
    //    var atoms = PdbLoader.ReadAtomData(pdbPath);

    //    // Get atom spheres
    //    var atomSpheres = PdbLoader.GetAtomSpheres(atoms);
    //    var bounds = PdbLoader.GetBounds(atomSpheres);
    //    PdbLoader.OffsetPoints(ref atomSpheres, bounds.center);

    //    var atomClusters = new List<List<Vector4>>();
        
    //    var atomClustersL1 = PdbLoader.ReadAtomClusters(ProteinDiretory + "cluster_levels/" + pdbName + ".pdbL0.txt");
    //    //var atomClustersL1 = PdbLoader.ClusterAtomsByResidue(atoms, 10, 5);
    //    PdbLoader.OffsetPoints(ref atomClustersL1, bounds.center);
    //    atomClusters.Add(atomClustersL1);

    //    var atomClustersL2 = PdbLoader.ReadAtomClusters(ProteinDiretory + "cluster_levels/" + pdbName + ".pdbL1.txt");
    //    //var atomClustersL2 = PdbLoader.ClusterAtomsByChain(atoms, 5, 8); 
    //    PdbLoader.OffsetPoints(ref atomClustersL2, bounds.center);
    //    atomClusters.Add(atomClustersL2);

    //    var atomClustersL3 = PdbLoader.ReadAtomClusters(ProteinDiretory + "cluster_levels/" + pdbName + ".pdbL2.txt");
    //    //var atomClustersL3 = PdbLoader.ClusterAtomsByChain(atoms, 20, 15); 
    //    PdbLoader.OffsetPoints(ref atomClustersL3, bounds.center);
    //    atomClusters.Add(atomClustersL3);
        
    //    // Add ingredient
    //    SceneManager.Instance.AddIngredient(pdbName, bounds, atomSpheres, atomClusters); 

    //    for (int k = 0; k < results.Count; k++)
    //    {
    //        var p = results[k][0];
    //        var r = results[k][1];

    //        var position = new Vector3(-p[0].AsFloat, p[1].AsFloat, p[2].AsFloat);
    //        var rot = new Quaternion(r[0].AsFloat, r[1].AsFloat, r[2].AsFloat, r[3].AsFloat);
    //        var mat = (isSurface) ? Helper.quaternion_matrix(rot).transpose : Helper.quaternion_matrix(rot);
    //        var euler = Helper.euler_from_matrix(mat);
    //        var rotation = Helper.MayaRotationToUnity(euler);

    //        // Find centered position
    //        if (isSurface) position += Helper.QTransform(Helper.QuanternionToVector4(rotation), bounds.center);

    //        // Add instance
    //        SceneManager.Instance.AddIngredientInstance(pdbName, position, rotation);
    //    }

    //    Debug.Log("Added protein: " + pdbName + " num instances: " + results.Count);
    //    Debug.Log("*****");
    //}

	public static void AddRecipeIngredients(JSONNode recipeDictionary)
    {
		for (int j = 0; j < recipeDictionary.Count; j++)
		{
			//if (j == 2 ) break;
			var pdbName = recipeDictionary[j]["source"]["pdb"].Value.Replace(".pdb", "");
			var center = (bool)recipeDictionary[j]["source"]["transform"]["center"].AsBool;
			var biomt = (bool)recipeDictionary[j]["source"]["biomt"].AsBool;
            
            if (pdbName == "null") continue;  
			if (pdbName == "None") continue; 
			if (pdbName == "") continue;  
			if (pdbName.StartsWith("EMDB")) continue;			
			if (pdbName.Contains("1PI7_1vpu_biounit")) continue;
			var iname = recipeDictionary[j].Value;
            
            // Debug biomt
            //if (!pdbName.Contains("2plv")) continue;
            
            //var atoms = new List<Vector4>();
            var atoms = new List<PdbLoader.Atom>();
            var biomtInstances = new List<Vector4>();
            var atomClusters = new List<List<Vector4>>();
            
            var pdbPath = ProteinDiretory + pdbName + ".pdb";
            if (!File.Exists(pdbPath)) PdbLoader.DownloadPdbFile(pdbPath, ProteinDiretory); // If the pdb file does not exist try to download it
            
            if (biomt)
            {
                Debug.Log("Biomt instance: " + pdbName);
                //atoms = PdbLoader.ReadBioAssemblyFile(pdbPath);
            }
            else
            {
                atoms = PdbLoader.ReadAtomData(pdbPath);
            }

            // Get atom spheres
		    var atomSpheres = PdbLoader.GetAtomSpheres(atoms);
            var bounds = PdbLoader.GetBounds(atomSpheres);
            PdbLoader.OffsetPoints(ref atomSpheres, bounds.center);

            // Get cluster spheres
            var atomClustersL1 = PdbLoader.ClusterAtomsByResidue(atoms, 10, 5);
            PdbLoader.OffsetPoints(ref atomClustersL1, bounds.center);

			var atomClustersL2 = PdbLoader.ClusterAtomsByChain(atoms, 5, 10);
			PdbLoader.OffsetPoints(ref atomClustersL2, bounds.center);

			//calculate nSphere 
			int totalNb=atoms.Count;

			float nbSphere = (float)totalNb*(0.5f/100.0f);
			var atomClustersL3 = PdbLoader.ClusterAtomsKmeans(atoms, (int)nbSphere, 1.0f);
			PdbLoader.OffsetPoints(ref atomClustersL3, bounds.center);

            if (recipeDictionary[j]["source"]["transform"].Count != 1)
            {
                //translate
                var tr = recipeDictionary[j]["source"]["transform"]["translate"];//rotate also
                var offset = new Vector3(-tr[0].AsFloat, tr[1].AsFloat, tr[2].AsFloat);
                Debug.Log("translate " + offset.ToString());
                PdbLoader.OffsetPoints(ref atomSpheres, offset * -1.0f);
                PdbLoader.OffsetPoints(ref atomClustersL1, offset * -1.0f);
                PdbLoader.OffsetPoints(ref atomClustersL2, offset * -1.0f);
				PdbLoader.OffsetPoints(ref atomClustersL3, offset * -1.0f);
            }

			atomClusters.Add(atomClustersL1);
			atomClusters.Add(atomClustersL2);
            atomClusters.Add(atomClustersL3);

			// Add ingredient
            SceneManager.Instance.AddIngredient(pdbName, bounds, atomSpheres, atomClusters);
			
			for (int k = 0; k < recipeDictionary[j]["results"].Count; k++)
			{
				var p = recipeDictionary[j]["results"][k][0];
				var r = recipeDictionary[j]["results"][k][1];
				
				var position = new Vector3(-p[0].AsFloat, p[1].AsFloat, p[2].AsFloat);
				var rot = new Quaternion(r[0].AsFloat, r[1].AsFloat, r[2].AsFloat, r[3].AsFloat);
				var mat = Helper.quaternion_matrix(rot).transpose;
				var euler = Helper.euler_from_matrix(mat);
				var rotation = Helper.MayaRotationToUnity(euler);
				
				// Find centered position
				if (!center) position += Helper.QTransform(Helper.QuanternionToVector4(rotation), bounds.center);

				// Add instance
				SceneManager.Instance.AddIngredientInstance(pdbName, position, rotation);
			}
			SceneManager.Instance.totalNbAtoms+=(totalNb*recipeDictionary[j]["results"].Count);
			Debug.Log("Added: " + pdbName + " num instances: " + recipeDictionary[j]["results"].Count);
			Debug.Log("*****");
		}
	}
	
	 public static void LoadRecipe()
    {
        //var proteinDiretory = Application.dataPath + "/../Data/HIV/proteins/";
        if (!Directory.Exists(ProteinDiretory)) throw new Exception("No directory found at: " + ProteinDiretory);

        // Add ingredient pdb path to the loader
        PdbLoader.AddPdbDirectory(ProteinDiretory);

        //var cellPackSceneJsonPath = Application.dataPath + "/../Data/HIV/cellPACK/HIV-1_0.1.6-8_mixed_pdb.json";
		var cellPackSceneJsonPath = Application.dataPath + "/../Data/HIV/cellPACK/BloodHIV1.0_mixed_fixed_nc1.json";
        if (!File.Exists(cellPackSceneJsonPath)) throw new Exception("No file found at: " + cellPackSceneJsonPath);

        var resultData = Helper.parseJson(cellPackSceneJsonPath);

        //we can traverse the json dictionary and gather ingredient source (PDB,center), sphereTree, instance.geometry if we want.
        //the recipe is optional as it will gave more information than just the result file.

		//check if cytoplasme present
        if (resultData["cytoplasme"] != null)
        {
            var exteriorIngredients = resultData["cytoplasme"]["ingredients"];
            AddRecipeIngredients(exteriorIngredients);
        }

        int nComp = resultData["compartments"].Count;

        //I dont do the cytoplasme compartments, will do when Blood will be ready
        for (int i = 0; i < nComp; i++)
        {
			var surfaceIngredients = resultData ["compartments"] [i] ["surface"] ["ingredients"];
			AddRecipeIngredients (surfaceIngredients);
			var interiorIngredients = resultData ["compartments"] [i] ["interior"] ["ingredients"];
			AddRecipeIngredients (interiorIngredients);
		}
		SceneManager.Instance.UploadAllData();
    }
	
    //public static void LoadRecipe()
    //{
    //    if (!Directory.Exists(ProteinDiretory)) throw new Exception("No directory found at: " + ProteinDiretory);

    //    // Add ingredient pdb path to the loader
    //    PdbLoader.AddPdbDirectory(ProteinDiretory);

    //    var cellPackSceneJsonPath = Application.dataPath + "/../Data/HIV/cellPACK/HIV-1_0.1.6-8_mixed_pdb.json";
    //    if (!File.Exists(cellPackSceneJsonPath)) throw new Exception("No file found at: " + cellPackSceneJsonPath);

    //    var resultData = Helper.parseJson(cellPackSceneJsonPath);

    //    //we can traverse the json dictionary and gather ingredient source (PDB,center), sphereTree, instance.geometry if we want.
    //    //the recipe is optional as it will gave more information than just the result file.

    //    int nComp = resultData["compartments"].Count;

    //    //I dont do the cytoplasme compartments, will do when Blood will be ready
    //    for (int i = 0; i < nComp; i++)
    //    {
    //        var surfaceIngredients = resultData["compartments"][i]["surface"]["ingredients"];
    //        for (int j = 0; j < surfaceIngredients.Count; j++)
    //        {
    //            var pdbName = surfaceIngredients[j]["source"]["pdb"].Value.Replace(".pdb", "");
    //            var center = (bool)surfaceIngredients[j]["source"]["transform"]["center"].AsBool;

    //            AddProteinInstances(pdbName, true, center, surfaceIngredients[j]["results"]);
    //        }
            
    //        var interiorIngredients = resultData["compartments"][i]["interior"]["ingredients"];
    //        for (int j = 0; j < interiorIngredients.Count; j++)
    //        {
    //            var pdbName = interiorIngredients[j]["source"]["pdb"].Value.Replace(".pdb", "");
    //            var center = (bool)interiorIngredients[j]["source"]["transform"]["center"].AsBool;

    //            AddProteinInstances(pdbName, false, center, interiorIngredients[j]["results"]);
    //        }
    //    }
    //}

    public static void LoadMembrane()
    {
        var membraneDataPath = Application.dataPath + "/../Data/HIV/membrane/HIV_mb.bin";
        if (!File.Exists(membraneDataPath)) throw new Exception("No file found at: " + membraneDataPath);
        SceneManager.Instance.LoadMembrane(membraneDataPath, Vector3.zero, Quaternion.identity);

        SceneManager.Instance.UploadAllData();
    }

    public static void LoadRna()
    {
        var rnaControlPointsPath = Application.dataPath + "/../Data/HIV/rna/rna_allpoints.txt";
        if (!File.Exists(rnaControlPointsPath)) throw new Exception("No file found at: " + rnaControlPointsPath);

        var controlPoints = new List<Vector4>();
        foreach (var line in File.ReadAllLines(rnaControlPointsPath))
        {
            var split = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var x = float.Parse(split[0]);
            var y = float.Parse(split[1]);
            var z = float.Parse(split[2]);

            //should use -Z pdb are right-handed
            controlPoints.Add(new Vector4(-x, y, z, 1));
        }

        SceneManager.Instance.LoadRna(controlPoints);
        SceneManager.Instance.UploadAllData();
    }

    public static void LoadScene()
    {
		SceneManager.Instance.totalNbAtoms = 0;
        LoadRecipe();
        //LoadMembrane();
        //LoadRna();
		Debug.Log ("Total nb Atoms " + SceneManager.Instance.totalNbAtoms);
        // Tell the manager what is the size of the dataset for duplication
        SceneManager.Instance.SetUnitSize();

        int n = 1;

        for (int i = -n; i <= n; i++)
        {
            for (int j = -n; j <= n; j++)
            {
                for (int k = -n; k <= n; k++)
                {
                    //SceneManager.Instance.AddUnitInstance(new Vector3(i * 1700, j * 2600, k * 3500));
                }
            }
        }

        SceneManager.Instance.UploadAllData();
    }

    public static void ClearScene()
    {
        SceneManager.Instance.ClearScene();
    }

    public static void DebugAddInstance()
    {
        //var atoms = PdbLoader.ReadPdbFile(Application.dataPath + "/../Data/HIV/ingredients/3j3q_1vu4_A_biomt.pdb");
        //var bounds = PdbLoader.GetBounds(atoms);
        //PdbLoader.OffsetPoints(ref atoms, bounds.center);
        //SceneManager.Instance.AddIngredient("3j3q_1vu4_A_biomt", atoms, bounds);

        //var clusters = PdbLoader.ReadClusterPdbFile(Application.dataPath + "/../Data/HIV/ingredients/3j3q_1vu4_A_biomt_0.txt");
        //PdbLoader.OffsetPoints(ref clusters, bounds.center);
        //SceneManager.Instance.AddIngredientClusteringLevel("3j3q_1vu4_A_biomt", clusters); 

        //SceneManager.Instance.AddIngredientInstance("3j3q_1vu4_A_biomt", Vector3.zero, Quaternion.identity);
        //SceneManager.Instance.UploadAllData();
    }
}
