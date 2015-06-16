using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CellPackLoader
{
   public static void LoadScene()
    {
        var rnaControlPointsPath = Application.dataPath + "/../Data/rna_allpoints.txt";
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

        SceneManager.Instance.LoadDna(controlPoints);
        SceneManager.Instance.UploadAllData();
    }

    public static void ClearScene()
    {
        SceneManager.Instance.ClearScene();
    }

    public static void DebugAddInstance()
    {
        
    }
}
