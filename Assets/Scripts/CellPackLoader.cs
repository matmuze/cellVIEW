using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SimpleJSON;

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
	public static void AddCurveIngredients(JSONNode ingredientDictionary)
	{
		//in case there is curveN, grab the data if more than 4 points
		//use the given PDB for the representation.
		var pdbName = ingredientDictionary["source"]["pdb"].Value.Replace(".pdb", "");
		int nCurve = ingredientDictionary["nbCurve"].AsInt;//ingredientDictionary.Count - 3;
		List<Vector4> atomSpheres;
		if ((pdbName == "null") || (pdbName == "None")) {
			atomSpheres = new List<Vector4>();
			atomSpheres.Add(new Vector4(0,0,0,1));//put the scale here ?
		} else {
			var pdbPath = ProteinDiretory + pdbName + ".pdb";
			if (!File.Exists(pdbPath)){ 
				if (pdbName.Length == 4){
					PdbLoader.DownloadPdbFile(pdbName, ProteinDiretory); // If the pdb file does not exist try to download it
				}else {
					PdbLoader.DownloadPdbFromRecipeFile(pdbName, ProteinDiretory);
				}
			}
			atomSpheres = PdbLoader.ReadAtomSpheres(pdbPath);
		}
		Debug.Log (ingredientDictionary["name"]+" "+pdbName+" "+atomSpheres.Count);
		float distance = 34.0f;
		if (ingredientDictionary ["name"].Value.Contains ("DNA"))
			//angular 60
			//numStep 11
			//twist 34.3
			//scale sphere 1 // radii total 11.5
			distance = 34.0f;
		else if (ingredientDictionary ["name"].Value.Contains ("RNA"))
			distance = DisplaySettings.Instance.DistanceContraint;
		else if (ingredientDictionary ["name"].Value.Contains ("peptide"))
			//no twist/scale = 3/numStep = ?
			distance = DisplaySettings.Instance.DistanceContraint;
		else if (ingredientDictionary ["name"].Value.Contains ("lypoglycane"))
			//no distance constraint ?
			//numStep1
			//scale sphere 20
			distance = DisplaySettings.Instance.DistanceContraint;
		else 
			distance = DisplaySettings.Instance.DistanceContraint;
		for (int i=0; i<nCurve; i++) {
			var controlPoints = new List<Vector4> ();
			if ( ingredientDictionary ["curve" + i.ToString()].Count < 4 ) continue;
			for (int k = 0; k < ingredientDictionary ["curve" + i.ToString()].Count; k++) {
				var p = ingredientDictionary ["curve" + i.ToString()] [k];
				controlPoints.Add (new Vector4 (-p [0].AsFloat, p [1].AsFloat, p [2].AsFloat, 1));
			}
			SceneManager.Instance.AddDNAPath(controlPoints,atomSpheres.Count);	
			SceneManager.Instance.AddNucleicAcids(atomSpheres);
			//break;
		}
	}

	public static void AddRecipeIngredients(JSONNode recipeDictionary)
    {
		for (int j = 0; j < recipeDictionary.Count; j++)
		{
            var biomt = (bool)recipeDictionary[j]["source"]["biomt"].AsBool;
            var center = (bool)recipeDictionary[j]["source"]["transform"]["center"].AsBool;
			var pdbName = recipeDictionary[j]["source"]["pdb"].Value.Replace(".pdb", "");
			Debug.Log ("step "+recipeDictionary[j]["name"].Value);
			if (!recipeDictionary[j]["name"].Value.Contains("DNA")) continue;

			if (recipeDictionary[j].Count > 3){
				AddCurveIngredients(recipeDictionary[j]);
				continue;
			}
			else {
				//if (!pdbName.Contains("1p71")) continue;
				//continue;
			}

            if (pdbName == "") continue;  
            if (pdbName == "null") continue;  
			if (pdbName == "None") continue; 
			if (pdbName.StartsWith("EMDB")) continue;			
			if (pdbName.Contains("1PI7_1vpu_biounit")) continue;
			
            // Debug biomt
            //if (!biomt) continue;
            //if (!pdbName.Contains("2plv")) continue;
            //if (!pdbName.Contains("3j3q_1vu4")) continue;
            //if (!pdbName.Contains("3gau")) continue;

			var pdbPath = ProteinDiretory + pdbName + ".pdb";
			if (!File.Exists(pdbPath)){ 
				if (pdbName.Length == 4){
					PdbLoader.DownloadPdbFile(pdbName, ProteinDiretory); // If the pdb file does not exist try to download it
				}else {
					PdbLoader.DownloadPdbFromRecipeFile(pdbName, ProteinDiretory);
				}
			}

            // Load all data from text files
            var atoms = PdbLoader.ReadAtomData(pdbPath);
            var atomClusters = new List<List<Vector4>>();
            var biomtTransforms = (biomt) ? PdbLoader.ReadBiomtData(pdbPath) : new List<Matrix4x4>();
            
            //calculate nSphere
            float numClusterSeeds = (float)atoms.Count * (0.5f / 100.0f);

            var atomSpheres = new List<Vector4>();
		    var atomClustersL1 = new List<Vector4>();
		    var atomClustersL2 = new List<Vector4>();
            var atomClustersL3 = new List<Vector4>();
            
            // Treat this protein separatly as it has only CA in the pdb
		    if (PdbLoader.IsCarbonOnly(atoms))
		    {
                atomSpheres = PdbLoader.ClusterAtomsByResidue(atoms, 1, 3);
                atomClustersL1 = PdbLoader.ClusterAtomsByResidue(atoms, 1, 4);
                atomClustersL2 = PdbLoader.ClusterAtomsByChain(atoms, 3, 8);
                atomClustersL3 = PdbLoader.ClusterAtomsByChain(atoms, 10, 10);
		    }
            else
		    {
                atomSpheres = PdbLoader.GetAtomSpheres(atoms);
                atomClustersL1 = PdbLoader.ClusterAtomsByResidue(atoms, 8, 4);
                atomClustersL2 = PdbLoader.ClusterAtomsByChain(atoms, 3, 8);
                //atomClustersL3 = (atoms.Count > 500) ? PdbLoader.ClusterAtomsByChain(atoms, 10, 10) : PdbLoader.ClusterAtomsByChain(atoms, 3, 8);
                atomClustersL3 = (atoms.Count > 500) ? PdbLoader.ClusterAtomsKmeans(atoms, (int)numClusterSeeds, 1.0f): new List<Vector4>(atomClustersL2) ;
		    }
           
            // use biomt as one single instance until I find  better solution
		    if (biomt)
		    {
                atomSpheres = PdbLoader.BuildBiomt(atomSpheres, biomtTransforms);
                atomClustersL1 = PdbLoader.BuildBiomt(atomClustersL1, biomtTransforms);
                atomClustersL2 = PdbLoader.BuildBiomt(atomClustersL2, biomtTransforms);
                atomClustersL3 = PdbLoader.BuildBiomt(atomClustersL3, biomtTransforms);
            }

            var bounds = PdbLoader.GetBounds(atomSpheres);
            PdbLoader.OffsetPoints(ref atomSpheres, bounds.center);
            PdbLoader.OffsetPoints(ref atomClustersL1, bounds.center);
            PdbLoader.OffsetPoints(ref atomClustersL2, bounds.center);
            PdbLoader.OffsetPoints(ref atomClustersL3, bounds.center);

            if (recipeDictionary[j]["source"]["transform"].Count != 1)
            {
                //translate
                var tr = recipeDictionary[j]["source"]["transform"]["translate"];//rotate also
                var offset = new Vector3(-tr[0].AsFloat, tr[1].AsFloat, tr[2].AsFloat);

                Debug.Log("translate object with offset: " + offset.ToString());
                PdbLoader.OffsetPoints(ref atomSpheres, offset);
                PdbLoader.OffsetPoints(ref atomClustersL1, offset * 1.0f);
                PdbLoader.OffsetPoints(ref atomClustersL2, offset * 1.0f);
                PdbLoader.OffsetPoints(ref atomClustersL3, offset * 1.0f);
            }

            atomClusters.Add(atomClustersL1);
            atomClusters.Add(atomClustersL2);
            atomClusters.Add(atomClustersL3);

			// Add ingredient to scene manager
            SceneManager.Instance.AddIngredient(pdbName, bounds, atomSpheres, atomClusters);
			
			for (int k = 0; k < recipeDictionary[j]["results"].Count; k++)
			{
				var p = recipeDictionary[j]["results"][k][0];
				var r = recipeDictionary[j]["results"][k][1];

                var position = new Vector3(-p[0].AsFloat, p[1].AsFloat, p[2].AsFloat);
				var rotation = new Quaternion(r[0].AsFloat, r[1].AsFloat, r[2].AsFloat, r[3].AsFloat);

                var mat = Helper.quaternion_matrix(rotation);
				var euler = Helper.euler_from_matrix(mat);
				rotation = Helper.MayaRotationToUnity(euler);

				// Find centered position
				if (!center) position += Helper.QuaternionTransform(rotation, bounds.center);

                SceneManager.Instance.AddIngredientInstance(pdbName, position, rotation);

                //if (biomt)
                //{
                //    foreach (var matBiomt in biomtTransforms)
                //    {
                //        var rotBiomt = Helper.RotationMatrixToQuaternion(matBiomt);
                //        var posBiomt = new Vector3(matBiomt.m03, matBiomt.m13, matBiomt.m23) + position + bounds.center;

                //        SceneManager.Instance.AddIngredientInstance(pdbName, posBiomt, rotBiomt);
                //    }
                //}
                //else
                //{
                //     SceneManager.Instance.AddIngredientInstance(pdbName, position, rotation);
                //}
			}
			
            Debug.Log("Added: " + pdbName + " num instances: " + recipeDictionary[j]["results"].Count);
			Debug.Log("*****");
        }
	}
	
	public static void LoadRecipe(string recipePath)
	{
		//var proteinDiretory = Application.dataPath + "/../Data/HIV/proteins/";
		if (!Directory.Exists(ProteinDiretory)) throw new Exception("No directory found at: " + ProteinDiretory);
		
		//var cellPackSceneJsonPath = Application.dataPath + "/../Data/HIV/cellPACK/HIV-1_0.1.6-8_mixed_pdb.json";
		var cellPackSceneJsonPath = recipePath;//Application.dataPath + "/../Data/HIV/cellPACK/BloodHIV1.0_mixed_fixed_nc1.json";
		if (!File.Exists(cellPackSceneJsonPath)) throw new Exception("No file found at: " + cellPackSceneJsonPath);
		
		var resultData = Helper.ParseJson(cellPackSceneJsonPath);
		
		//we can traverse the json dictionary and gather ingredient source (PDB,center), sphereTree, instance.geometry if we want.
		//the recipe is optional as it will gave more information than just the result file.
		
		//check if cytoplasme present
		if (resultData["cytoplasme"] != null)
		{
			AddRecipeIngredients(resultData["cytoplasme"]["ingredients"]);
		}
		
		for (int i = 0; i < resultData["compartments"].Count; i++)
		{
			AddRecipeIngredients (resultData["compartments"][i]["interior"]["ingredients"]);
			AddRecipeIngredients (resultData ["compartments"] [i] ["surface"] ["ingredients"]);
		}
	}

    public static void LoadMembrane()
    {
        var membraneDataPath = Application.dataPath + "/../Data/HIV/membrane/HIV_mb.bin";
        if (!File.Exists(membraneDataPath)) throw new Exception("No file found at: " + membraneDataPath);
        SceneManager.Instance.LoadMembrane(membraneDataPath, Vector3.zero, Quaternion.identity);
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
		var atomSpheres = PdbLoader.ReadAtomSpheres(PdbLoader.DefaultPdbDirectory + "RNA_U_Base.pdb");
        SceneManager.Instance.AddDNAPath(controlPoints,atomSpheres.Count);
		SceneManager.Instance.AddNucleicAcids(atomSpheres);
    }

	public static void LoadMycoScene()
	{
		//var cellPackSceneJsonPath = Application.dataPath + "/../Data/HIV/cellPACK/Mycoplasma1.5_mixed_pdb_fixed.json";
		var cellPackSceneJsonPath = Application.dataPath + "/../Data/HIV/cellPACK/mycoDNA.json";
		LoadRecipe(cellPackSceneJsonPath);
		//LoadMembrane();
		//LoadRna();
	}
	public static void LoadMycoDNAScene()
	{
		var cellPackSceneJsonPath = Application.dataPath + "/../Data/HIV/cellPACK/Mycoplasma1.5_mixed_pdb_fixed.json";
		LoadRecipe(cellPackSceneJsonPath);
		//LoadMembrane();
		//LoadRna();
	}


	public static void LoadBloodHIVScene()
	{
		var cellPackSceneJsonPath = Application.dataPath + "/../Data/HIV/cellPACK/BloodHIV1.0_mixed_fixed_nc1.json";
		LoadRecipe(cellPackSceneJsonPath);
		LoadMembrane();
		//LoadRna();
	}

	public static void LoadHIVScene()
	{
		var cellPackSceneJsonPath = Application.dataPath + "/../Data/HIV/cellPACK/HIV-1_0.1.6-8_mixed_pdb.json";
		LoadRecipe(cellPackSceneJsonPath);
		LoadMembrane();
		LoadRna();
	}


    public static void LoadScene()
    {
		//LoadMycoScene ();
		//"HIV", "BloodHIV", "Mycoplasma"
		if (DisplaySettings.Instance.sceneid == 2)
			LoadMycoScene ();
		else if (DisplaySettings.Instance.sceneid == 1)
			LoadBloodHIVScene ();
		else if (DisplaySettings.Instance.sceneid == 0)
			LoadHIVScene ();
		else if (DisplaySettings.Instance.sceneid == 3)
			LoadMycoDNAScene ();
		else
			LoadHIVScene ();
        // Tell the manager what is the size of the dataset for duplication
        SceneManager.Instance.SetUnitInstanceCount();

        int n = 0;

        for (int i = -n; i <= n; i++)
        {
            for (int j = -n; j <= n; j++)
            {
                for (int k = -n; k <= n; k++)
                {
                    SceneManager.Instance.AddUnitInstance(new Vector3(i * 1700, j * 2600, k * 3500));
                }
            }
        }
        
        Debug.Log("Unit atom count " + SceneManager.Instance.UnitAtomCount);
        Debug.Log("Global atom count " + SceneManager.Instance.GlobalAtomCount);

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
