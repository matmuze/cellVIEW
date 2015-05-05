using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SimpleJSON;

using miniJson;
public static class CellPackLoader
{
    public static void LoadScene()
    {
		bool use_json = true;
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

		for (var i = 0; i < sceneData.Length; i += 8) {
				int pdbNameIndex = (int)sceneData [i];
				var pdbName = pdbNames [pdbNameIndex];

				var position = new Vector3 (-sceneData [i + 1], sceneData [i + 2], sceneData [i + 3]);
				var rot = new Quaternion (sceneData [i + 4], sceneData [i + 5], sceneData [i + 6], sceneData [i + 7]);
				
				var mat = Helper.quaternion_matrix(rot);//.transpose;
				var euler = Helper.euler_from_matrix(mat);
				var rotation =  Helper.MayaRotationToUnity(euler);
				SceneManager.Instance.AddIngredientPdb(pdbName, false); // Decide to center the atoms or not 
				SceneManager.Instance.AddIngredientInstance(pdbName, position, rotation);
		}
		SceneManager.Instance.AddMembrane(membraneDataPath, Vector3.zero, Quaternion.identity);
        SceneManager.Instance.UploadAllData();
    }

	public static void loadRecipe(){
		//fetch recipe name + recipe version
		//fetch result file
		//build the instance
		var membraneDataPath = Application.dataPath + "/../Data/HIV/membrane/HIV_mb.bin";
		if (!File.Exists(membraneDataPath)) throw new Exception("No file found at: " + membraneDataPath);
		
		var pdbIngredientsDirectory = Application.dataPath + "/../Data/HIV/ingredients/";
		if (!Directory.Exists(pdbIngredientsDirectory)) throw new Exception("No directory found at: " + pdbIngredientsDirectory);

		var cellPackRecipeJsonPath = Application.dataPath + "/../Data/HIV/cellPACK/HIV-1_0.1.6-7.json";
		if (!File.Exists(cellPackRecipeJsonPath)) cellPackRecipeJsonPath = Helper.DownloadRecipeFile("HIV-1_0.1.6-7");//throw new Exception("No file found at: " + cellPackSceneJsonPath);

		var cellPackSceneJsonPath = Application.dataPath + "/../Data/HIV/cellPACK/HIV-1_0.1.6-8_mixed_pdb.json";
		if (!File.Exists(cellPackSceneJsonPath)) throw new Exception("No file found at: " + cellPackSceneJsonPath);		
	
		var result_data = Helper.parseJson(cellPackSceneJsonPath);
		var recipe_data = Helper.parseJson(cellPackRecipeJsonPath);
		//we can traverse the json dictionary and gather ingredient source (PDB,center), sphereTree, instance.geometry if we want.
		//the recipe is optional as it will gave more information than just the result file.

		// Add ingredient pdb path to the loader
		PdbLoader.AddPdbDirectory(pdbIngredientsDirectory);

		int nComp = result_data["compartments"].Count;
		//I dont do the cytoplasme compartments, will do when Blood will be ready
		for (int i=0; i<nComp; i++) {
			JSONNode surface_ingredients = result_data["compartments"][i]["surface"]["ingredients"];
			JSONNode interior_ingredients = result_data["compartments"][i]["interior"]["ingredients"];
			for (int j=0;j<surface_ingredients.Count;j++){
				var pdbName = surface_ingredients[j]["source"]["pdb"].Value;
				pdbName = pdbName.Replace(".pdb","");
				//remove .pdb if in the name
				var center = (bool)surface_ingredients[j]["source"]["transform"]["center"].AsBool;
				//Debug.Log (pdbName+" "+surface_ingredients[j]["results"].Count.ToString ()+" "+center.ToString());
				for (int k=0;k<surface_ingredients[j]["results"].Count;k++){//or Length of the array
					var p = surface_ingredients[j]["results"][k][0];
					var r = surface_ingredients[j]["results"][k][1];
					var position = new Vector3 (-p[0].AsFloat,p[1].AsFloat,p[2].AsFloat);
					var rot = new Quaternion (r[0].AsFloat,r[1].AsFloat,r[2].AsFloat,r[3].AsFloat);
					var mat = Helper.quaternion_matrix(rot);//.transpose;
					var euler = Helper.euler_from_matrix(mat);
					var rotation =  Helper.MayaRotationToUnity(euler);
					SceneManager.Instance.AddIngredientPdb (pdbName, center); // Decide to center the atoms or not
					SceneManager.Instance.AddIngredientInstance (pdbName, position, rotation);
				}
			}
			for (int j=0;j<interior_ingredients.Count;j++){
				var pdbName = interior_ingredients[j]["source"]["pdb"].Value;
				pdbName = pdbName.Replace(".pdb","");
				var center = (bool)interior_ingredients[j]["source"]["transform"]["center"].AsBool;
				//Debug.Log (pdbName+" "+interior_ingredients[j]["results"].Count.ToString ()+" "+center.ToString());				
				for (int k=0;k<interior_ingredients[j]["results"].Count;k++){//or Length of the array
					var p = interior_ingredients[j]["results"][k][0];
					var r = interior_ingredients[j]["results"][k][1];
					var position = new Vector3 (-p[0].AsFloat,p[1].AsFloat,p[2].AsFloat);
					var rot = new Quaternion (r[0].AsFloat,r[1].AsFloat,r[2].AsFloat,r[3].AsFloat);
					var mat = Helper.quaternion_matrix(rot);//.transpose;
					var euler = Helper.euler_from_matrix(mat);
					var rotation =  Helper.MayaRotationToUnity(euler);
					SceneManager.Instance.AddIngredientPdb (pdbName, center); // Decide to center the atoms or not
					SceneManager.Instance.AddIngredientInstance (pdbName, position, rotation);
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
