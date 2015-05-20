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
        SceneManager.Instance.LoadDnaControlPoints();
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
