using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SimpleJSON;

public static class CellPackLoader
{
    //private static string _pdbCustererCmd =
    //    @"D:\Projects\Unity5\CellPackViewer\trunk_cluster\Misc\AtomPdbClusterer\clusterPdb.exe";
    
    //public static void ComputeClusters()
    //{
    //    var membraneDataPath = Application.dataPath + "/../Data/HIV/membrane/HIV_mb.bin";
    //    if (!File.Exists(membraneDataPath)) throw new Exception("No file found at: " + membraneDataPath);

    //    var pdbIngredientsDirectory = Application.dataPath + "/../Data/HIV/ingredients/";
    //    if (!Directory.Exists(pdbIngredientsDirectory))
    //        throw new Exception("No directory found at: " + pdbIngredientsDirectory);

    //    // Add ingredient pdb path to the loader
    //    PdbLoader.AddPdbDirectory(pdbIngredientsDirectory);

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
    //            var pdbPath = Application.dataPath + "/../Data/HIV/ingredients/" + pdbName + ".pdb";
    //            var clusterFilePath = Application.dataPath + "/../Data/HIV/ingredients/" + pdbName + ".pdbL0.txt";

    //            if (File.Exists(pdbPath) && !File.Exists(clusterFilePath))
    //            {
    //                Debug.Log("Start pdb cluster batch");

    //                System.Diagnostics.Process process = new System.Diagnostics.Process();
    //                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
    //                //startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
    //                startInfo.WorkingDirectory = pdbIngredientsDirectory;
    //                startInfo.FileName = _pdbCustererCmd;
    //                startInfo.Arguments = "-f" + pdbName + ".pdb -c AP -a -100 -b 3";
    //                process.StartInfo = startInfo;
    //                process.Start();
    //            }
    //        }

    //        return;

    //        var interiorIngredients = resultData["compartments"][i]["interior"]["ingredients"];
    //        for (int j = 0; j < interiorIngredients.Count; j++)
    //        {
    //            var pdbName = interiorIngredients[j]["source"]["pdb"].Value.Replace(".pdb", "");
    //            var pdbPath = Application.dataPath + "/../Data/HIV/ingredients/" + pdbName + ".pdb";
    //            var clusterFilePath = Application.dataPath + "/../Data/HIV/ingredients/" + pdbName + ".pdbL0.txt";

    //            if (File.Exists(pdbPath) && !File.Exists(clusterFilePath))
    //            {
    //                Debug.Log("Execute clustering for file: " + Path.GetFileName(pdbPath));

    //                System.Diagnostics.Process process = new System.Diagnostics.Process();
    //                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
    //                //startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
    //                startInfo.WorkingDirectory = pdbIngredientsDirectory;
    //                startInfo.FileName = _pdbCustererCmd;
    //                startInfo.Arguments = "-f" + pdbName + ".pdb -c AP -a -100 -b 3";
    //                process.StartInfo = startInfo;
    //                process.Start();
    //            }
    //        }
    //    }
    //}

    public static void LoadRecipe()
    {
        var proteinDiretory = Application.dataPath + "/../Data/HIV/proteins/";
        if (!Directory.Exists(proteinDiretory)) throw new Exception("No directory found at: " + proteinDiretory);

        // Add ingredient pdb path to the loader
        PdbLoader.AddPdbDirectory(proteinDiretory);

        var cellPackSceneJsonPath = Application.dataPath + "/../Data/HIV/cellPACK/HIV-1_0.1.6-8_mixed_pdb.json";
        if (!File.Exists(cellPackSceneJsonPath)) throw new Exception("No file found at: " + cellPackSceneJsonPath);

        var resultData = Helper.parseJson(cellPackSceneJsonPath);

        //we can traverse the json dictionary and gather ingredient source (PDB,center), sphereTree, instance.geometry if we want.
        //the recipe is optional as it will gave more information than just the result file.

        int nComp = resultData["compartments"].Count;

        //I dont do the cytoplasme compartments, will do when Blood will be ready
        for (int i = 0; i < nComp; i++)
        {
            var surfaceIngredients = resultData["compartments"][i]["surface"]["ingredients"];
            for (int j = 0; j < surfaceIngredients.Count; j++)
            {
                var pdbName = surfaceIngredients[j]["source"]["pdb"].Value.Replace(".pdb", "");
                var center = (bool)surfaceIngredients[j]["source"]["transform"]["center"].AsBool;

                if (pdbName.Contains("1PI7_1vpu_biounit")) continue;

                var pdbPath = proteinDiretory + pdbName + ".pdb";
                var clusterLevelPath = proteinDiretory + "cluster_levels/" + pdbName + ".pdbL0.txt";

                if (!File.Exists(pdbPath) || !File.Exists(clusterLevelPath))
                {
                    Debug.Log("Skiping protein: " + pdbName + " because one of its structure file is missing.");
                }

                // Read atoms from pdb file
                var atoms = PdbLoader.ReadPdbFile(pdbPath);
                var bounds = PdbLoader.GetBounds(atoms);
                PdbLoader.OffsetPoints(ref atoms, bounds.center);

                // Read clusters level 0
                var clustersLevel0 = PdbLoader.ReadPdbFile_2(pdbPath); //PdbLoader.ReadClusterPdbFile(clusterLevelPath); //
                PdbLoader.OffsetPoints(ref clustersLevel0, bounds.center);

                // Add ingredient
                SceneManager.Instance.AddIngredient(pdbName, bounds, atoms, new List<List<Vector4>> { clustersLevel0 });

                for (int k = 0; k < surfaceIngredients[j]["results"].Count; k++)
                {
                    var p = surfaceIngredients[j]["results"][k][0];
                    var r = surfaceIngredients[j]["results"][k][1];

                    var position = new Vector3(-p[0].AsFloat, p[1].AsFloat, p[2].AsFloat);
                    var rot = new Quaternion(r[0].AsFloat, r[1].AsFloat, r[2].AsFloat, r[3].AsFloat);
                    var mat = Helper.quaternion_matrix(rot).transpose;
                    var euler = Helper.euler_from_matrix(mat);
                    var rotation = Helper.MayaRotationToUnity(euler);

                    // Find centered position
                    position += Helper.QTransform(Helper.QuanternionToVector4(rotation), bounds.center);

                    // Add instance
                    SceneManager.Instance.AddIngredientInstance(pdbName, position, rotation);
                }

                Debug.Log("Added: " + pdbName + " num instances: " + surfaceIngredients[j]["results"].Count);
                Debug.Log("*****");
            }
            
            var interiorIngredients = resultData["compartments"][i]["interior"]["ingredients"];
            for (int j = 0; j < interiorIngredients.Count; j++)
            {
                var pdbName = interiorIngredients[j]["source"]["pdb"].Value.Replace(".pdb", "");
                var center = (bool)interiorIngredients[j]["source"]["transform"]["center"].AsBool;

                if (pdbName.Contains("1PI7_1vpu_biounit")) continue;

                var pdbPath = proteinDiretory + pdbName + ".pdb";
                var clusterLevelPath = proteinDiretory + "cluster_levels/" + pdbName + ".pdbL0.txt";

                if (!File.Exists(pdbPath) || !File.Exists(clusterLevelPath))
                {
                    Debug.Log("Skiping protein: " + pdbName + " because one of its structure file is missing.");
                }

                // Read atoms from pdb file
                var atoms = PdbLoader.ReadPdbFile(pdbPath);
                var bounds = PdbLoader.GetBounds(atoms);
                PdbLoader.OffsetPoints(ref atoms, bounds.center);

                // Read clusters level 0
                var clustersLevel0 = PdbLoader.ReadPdbFile_2(pdbPath); //PdbLoader.ReadClusterPdbFile(clusterLevelPath); 
                PdbLoader.OffsetPoints(ref clustersLevel0, bounds.center);

                // Add ingredient
                SceneManager.Instance.AddIngredient(pdbName, bounds, atoms, new List<List<Vector4>> { clustersLevel0 });

                for (int k = 0; k < interiorIngredients[j]["results"].Count; k++)
                {
                    var p = interiorIngredients[j]["results"][k][0];
                    var r = interiorIngredients[j]["results"][k][1];
                    var position = new Vector3(-p[0].AsFloat, p[1].AsFloat, p[2].AsFloat);
                    var rot = new Quaternion(r[0].AsFloat, r[1].AsFloat, r[2].AsFloat, r[3].AsFloat);
                    var mat = Helper.quaternion_matrix(rot); //.transpose;
                    var euler = Helper.euler_from_matrix(mat);
                    var rotation = Helper.MayaRotationToUnity(euler);

                    // Find centered position
                    //position += Helper.QTransform(Helper.QuanternionToVector4(rotation), bounds.center);

                    SceneManager.Instance.AddIngredientInstance(pdbName, position, rotation);
                }

                Debug.Log("Added: " + pdbName + " num instances: " + interiorIngredients[j]["results"].Count);
                Debug.Log("*****");
            }
        }
    }

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
        LoadRecipe();
        LoadMembrane();
        LoadRna();

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
