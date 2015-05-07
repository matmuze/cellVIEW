using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SimpleJSON;

public static class CellPackLoader
{
    public static void LoadScene()
    {
        //var membraneDataPath = Application.dataPath + "/../Data/HIV/membrane/HIV_mb.bin";
        //if (!File.Exists(membraneDataPath)) throw new Exception("No file found at: " + membraneDataPath);

        //var cellPackSceneDataPath = Application.dataPath + "/../Data/HIV/cellPACK/HIV-1_0.1.6-7_mixed_pdb.bin";
        //if (!File.Exists(cellPackSceneDataPath)) throw new Exception("No file found at: " + cellPackSceneDataPath);

        //var cellPackIngredientNamesPath = Application.dataPath + "/../Data/HIV/cellPACK/HIV-1_0.1.6-7_mixed_pdb.txt";
        //if (!File.Exists(cellPackIngredientNamesPath)) throw new Exception("No file found at: " + cellPackIngredientNamesPath);

        //var pdbIngredientsDirectory = Application.dataPath + "/../Data/HIV/ingredients/";
        //if (!Directory.Exists(pdbIngredientsDirectory)) throw new Exception("No directory found at: " + pdbIngredientsDirectory);

        //// Add ingredient pdb path to the loader
        //PdbLoader.AddPdbDirectory(pdbIngredientsDirectory);

        //// Fetch scene data
        //var sceneData = Helper.ReadBytesAsFloats(cellPackSceneDataPath);

        //// Fetch ingredient pdb names
        //var pdbNames = File.ReadAllLines(cellPackIngredientNamesPath);
        
        //for (var i = 0; i < sceneData.Length; i += 8)
        //{
        //    int pdbNameIndex = (int)sceneData[i];
        //    var pdbName = pdbNames[pdbNameIndex];

        //    var position = new Vector3(sceneData[i + 1], sceneData[i + 2], sceneData[i + 3]);
        //    var rotation = new Quaternion(sceneData[i + 4], sceneData[i + 5], -sceneData[i + 6], sceneData[i + 7]);

        //    if (String.Compare(pdbName, "3j3q_1vu4_A_biomt", StringComparison.OrdinalIgnoreCase) == 0)
        //    {
        //        // skip this one because of weird positions
        //    }
        //    else
        //    {
        //        SceneManager.Instance.AddIngredientPdb(pdbName, true); // Decide to center the atoms or not
        //        SceneManager.Instance.AddIngredientInstance(pdbName, position, rotation);
        //    }
        //}

        //SceneManager.Instance.AddMembrane(membraneDataPath, Vector3.zero, Quaternion.identity);
        //SceneManager.Instance.UploadAllData();
        //SceneManager.Instance.ResizeArrays();
    }

    public static void LoadRecipe()
    {
        var membraneDataPath = Application.dataPath + "/../Data/HIV/membrane/HIV_mb.bin";
        if (!File.Exists(membraneDataPath)) throw new Exception("No file found at: " + membraneDataPath);

        var pdbIngredientsDirectory = Application.dataPath + "/../Data/HIV/ingredients/";
        if (!Directory.Exists(pdbIngredientsDirectory)) throw new Exception("No directory found at: " + pdbIngredientsDirectory);

        // Add ingredient pdb path to the loader
        PdbLoader.AddPdbDirectory(pdbIngredientsDirectory);

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

                //Debug.Log (pdbName+" "+surface_ingredients[j]["results"].Count.ToString ()+" "+ center.ToString());

                var atoms = PdbLoader.ReadPdbFile(pdbName);
                var bounds = PdbLoader.GetBounds(atoms);

                // Center atoms
                PdbLoader.OffsetAtoms(ref atoms, bounds.center);
                SceneManager.Instance.AddIngredient(pdbName, atoms, bounds); 

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
            }

            var interiorIngredients = resultData["compartments"][i]["interior"]["ingredients"];
            for (int j = 0; j < interiorIngredients.Count; j++)
            {
                var pdbName = interiorIngredients[j]["source"]["pdb"].Value.Replace(".pdb", "");
                var center = (bool)interiorIngredients[j]["source"]["transform"]["center"].AsBool;

                Debug.Log(pdbName + " " + interiorIngredients[j]["results"].Count.ToString() + " " + center.ToString());

                var atoms = PdbLoader.ReadPdbFile(pdbName);
                var bounds = PdbLoader.GetBounds(atoms);

                // Center atoms
                if (center) PdbLoader.OffsetAtoms(ref atoms, bounds.center);
                SceneManager.Instance.AddIngredient(pdbName, atoms, bounds); 

                for (int k = 0; k < interiorIngredients[j]["results"].Count; k++)
                {
                    var p = interiorIngredients[j]["results"][k][0];
                    var r = interiorIngredients[j]["results"][k][1];
                    var position = new Vector3(-p[0].AsFloat, p[1].AsFloat, p[2].AsFloat);
                    var rot = new Quaternion(r[0].AsFloat, r[1].AsFloat, r[2].AsFloat, r[3].AsFloat);
                    var mat = Helper.quaternion_matrix(rot);//.transpose;
                    var euler = Helper.euler_from_matrix(mat);
                    var rotation = Helper.MayaRotationToUnity(euler);

                    // Find centered position
                    //position += Helper.QTransform(Helper.QuanternionToVector4(rotation), bounds.center);
                    
                    SceneManager.Instance.AddIngredientInstance(pdbName, position, rotation);
                }
            }
        }

        SceneManager.Instance.AddMembrane(membraneDataPath, Vector3.zero, Quaternion.identity);
        SceneManager.Instance.UploadAllData();
    }

    public static void ClearScene()
    {
        SceneManager.Instance.ClearScene();
    }

    public static void DebugAddInstance()
    {
        //var pdbIngredientsPath = Application.dataPath + "/../Data/HIV/ingredients/";
        //if (!Directory.Exists(pdbIngredientsPath)) throw new Exception("No directory found at: " + pdbIngredientsPath);

        //// Add ingredient pdb path to the loader
        //PdbLoader.AddPdbDirectory(pdbIngredientsPath);

        //SceneManager.Instance.ClearScene();

        //SceneManager.Instance.AddIngredientPdb("MA_matrix_G1");
        //SceneManager.Instance.AddIngredientInstance("MA_matrix_G1", Vector3.zero, Quaternion.identity);
       
        ////SceneManager.Instance.LoadMembrane("HIV_mb", Vector3.zero, Quaternion.identity);
        ////SceneManager.Instance.AddBioAssemblyInstance("3j3q_1vu4_A_biomt", Vector3.zero, Quaternion.identity);
        
        //SceneManager.Instance.UploadAllData();
    }
}
