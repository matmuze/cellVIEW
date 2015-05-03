using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CellPackLoader
{
    public static void LoadScene()
    {
        var membraneDataPath = Application.dataPath + "/../Data/HIV/membrane/HIV_mb.bin";
        if (!File.Exists(membraneDataPath)) throw new Exception("No file found at: " + membraneDataPath);

        var cellPackSceneDataPath = Application.dataPath + "/../Data/HIV/cellPACK/HIV-1_0.1.6-7_mixed_pdb.bin";
        if (!File.Exists(cellPackSceneDataPath)) throw new Exception("No file found at: " + cellPackSceneDataPath);

        var cellPackIngredientNamesPath = Application.dataPath + "/../Data/HIV/cellPACK/HIV-1_0.1.6-7_mixed_pdb.txt";
        if (!File.Exists(cellPackIngredientNamesPath)) throw new Exception("No file found at: " + cellPackIngredientNamesPath);

        var pdbIngredientsDirectory = Application.dataPath + "/../Data/HIV/ingredients/";
        if (!Directory.Exists(pdbIngredientsDirectory)) throw new Exception("No directory found at: " + pdbIngredientsDirectory);

        // Add ingredient pdb path to the loader
        PdbLoader.AddPdbDirectory(pdbIngredientsDirectory);

        // Fetch scene data
        var sceneData = Helper.ReadBytesAsFloats(cellPackSceneDataPath);

        // Fetch ingredient pdb names
        var pdbNames = File.ReadAllLines(cellPackIngredientNamesPath);

        for (var i = 0; i < sceneData.Length; i += 8)
        {
            int pdbNameIndex = (int)sceneData[i];
            var pdbName = pdbNames[pdbNameIndex];

            var position = new Vector3(sceneData[i + 1], sceneData[i + 2], sceneData[i + 3]);
            var rotation = new Quaternion(sceneData[i + 4], sceneData[i + 5], -sceneData[i + 6], sceneData[i + 7]);

            if (String.Compare(pdbName, "3j3q_1vu4_A_biomt", StringComparison.OrdinalIgnoreCase) == 0)
            {
                // skip this one because of weird positions
            }
            else
            {
                SceneManager.Instance.AddIngredientPdb(pdbName, true); // Decide to center the atoms or not
                SceneManager.Instance.AddIngredientInstance(pdbName, position, rotation);
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
        var pdbIngredientsPath = Application.dataPath + "/../Data/HIV/ingredients/";
        if (!Directory.Exists(pdbIngredientsPath)) throw new Exception("No directory found at: " + pdbIngredientsPath);

        // Add ingredient pdb path to the loader
        PdbLoader.AddPdbDirectory(pdbIngredientsPath);

        SceneManager.Instance.ClearScene();

        SceneManager.Instance.AddIngredientPdb("MA_matrix_G1");
        SceneManager.Instance.AddIngredientInstance("MA_matrix_G1", Vector3.zero, Quaternion.identity);
       
        //SceneManager.Instance.LoadMembrane("HIV_mb", Vector3.zero, Quaternion.identity);
        //SceneManager.Instance.AddBioAssemblyInstance("3j3q_1vu4_A_biomt", Vector3.zero, Quaternion.identity);
        
        SceneManager.Instance.UploadAllData();
    }
}
