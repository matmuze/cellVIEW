using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PdbLoader
{
    public static float[] AtomRadii = { 1.548f, 1.100f, 1.400f, 1.348f, 1.880f, 1.808f };
    public static string[] AtomSymbols = { "C", "H", "N", "O", "P", "S" };

    private static string _defaultPdbDirectory = Application.dataPath + "/../Data/Default/";
    private static List<string> _pdbDirectories = new List<string>() { _defaultPdbDirectory };

    // Color scheme taken from http://life.nthu.edu.tw/~fmhsu/rasframe/COLORS.HTM
    public static Color[] AtomColors = 
    { 
        new Color(100,100,100) / 255,     // C        light grey
        new Color(255,255,255) / 255,     // H        white       
        new Color(143,143,255) / 255,     // N        light blue
        new Color(220,10,10) / 255,       // O        red         
        new Color(255,165,0) / 255,       // P        orange      
        new Color(255,200,50) / 255       // S        yellow      
    };

    public static void AddPdbDirectory(string directory)
    {
        if (_pdbDirectories.Contains(directory)) return;
        _pdbDirectories.Add(directory);
    }

    public static string GetPdbFilePath(string pdbName)
    {
        if (!Directory.Exists(_defaultPdbDirectory)) Directory.CreateDirectory(_defaultPdbDirectory);

        var path = "";

        foreach (var pdbPath in _pdbDirectories.Select(pdbPath => pdbPath + pdbName + ".pdb").Where(File.Exists))
        {
            path = pdbPath;
            break;
        }

        return String.IsNullOrEmpty(path) ? DownloadPdbFile(pdbName) : path;
    }

    private static string DownloadPdbFile(string pdbName)
    {
        Debug.Log("Downloading pdb file");
        var www = new WWW("http://www.rcsb.org/pdb/download/downloadFile.do?fileFormat=pdb&compression=NO&structureId=" + WWW.EscapeURL(pdbName));

        while (!www.isDone)
        {
            EditorUtility.DisplayProgressBar("Download", "Downloading...", www.progress);
        }
        EditorUtility.ClearProgressBar();

        if (!string.IsNullOrEmpty(www.error)) throw new Exception(www.error);

        var path = _defaultPdbDirectory + pdbName + ".pdb";
        File.WriteAllText(path, www.text);

        return path;
    }

    public static Bounds GetBounds(List<Vector4> atoms)
    {
        var bbMin = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        var bbMax = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        foreach (var atom in atoms)
        {
            bbMin = Vector3.Min(bbMin, new Vector3(atom.x, atom.y, atom.z));
            bbMax = Vector3.Max(bbMax, new Vector3(atom.x, atom.y, atom.z));
        }

        var bbSize = bbMax - bbMin;
        var bbCenter = bbMin + bbSize * 0.5f;

        return new Bounds(bbCenter, bbSize);
    }

    public static void OffsetPoints(ref List<Vector4> points, Vector3 offset)
    {
        var offsetVector = new Vector4(offset.x, offset.y, offset.z, 0);

        for (var i = 0; i < points.Count(); i++)
            points[i] -= offsetVector;
    }

    public static List<Vector4> ReadClusterPdbFile(string path)
    {
        if (!File.Exists(path)) throw new Exception("File not found at: " + path);

        var clusters = new List<Vector4>();

        foreach (var line in File.ReadAllLines(path))
        {
            var split = line.Split(new []{' '}, StringSplitOptions.RemoveEmptyEntries);
            var x = float.Parse(split[0]);
            var y = float.Parse(split[1]);
            var z = float.Parse(split[2]);
            var r = float.Parse(split[3]);
            
            //should use -Z pdb are right-handed
            clusters.Add(new Vector4(-x, y, z, r));
        }

        Debug.Log("Loaded: "+ Path.GetFileName(path) + " num clusters: " + clusters.Count);
        return clusters;
    }


    //http://deposit.rcsb.org/adit/docs/pdb_atom_format.html#ATOM
    public static List<Vector4> ReadPdbFile(string path)
    {
        if (!File.Exists(path)) throw new Exception("File not found at: " + path);

        var atoms = new List<Vector4>();

        foreach (var line in File.ReadAllLines(path))
        {
            if (line.StartsWith("ATOM"))// || line.StartsWith("HETATM"))
            {
                var l = line.Length;

                var x = float.Parse(line.Substring(30, 8));
                var y = float.Parse(line.Substring(38, 8));
                var z = float.Parse(line.Substring(46, (l < 46 + 8) ? 8 : l - 46));

                //var atomSymbol = line.Substring(76, 2).Trim();
                var atomSymbol = line.Substring(13, 1).Trim();
                var symbolId = Array.IndexOf(AtomSymbols, atomSymbol);
                if (symbolId < 0)
                {
                    //throw new Exception("Atom symbol not found: " + atomSymbol);
                    //Debug.Log("Atom symbol not found: " + atomSymbol);
                    symbolId = 0;
                }
                
                //should use -Z pdb are right-handed
                var atom = new Vector4(-x, y, z, AtomRadii[symbolId]);
                atoms.Add(atom);
            }
        }

        Debug.Log("Loaded: " + Path.GetFileName(path) + " num atoms: " + atoms.Count);
        return atoms;
    }

    ////http://www.rcsb.org/pdb/101/static101.do?p=education_discussion/Looking-at-Structures/bioassembly_tutorial.html
    //public static void ReadBioAssemblyFile(string pdbName, out List<Vector4> atoms, out List<Matrix4x4> matrices)
    //{
    //    var path = GetPdbPath(pdbName);
    //    if (!File.Exists(path)) throw new Exception("File not found at: " + path);

    //    atoms = new List<Vector4>();

    //    foreach (var line in File.ReadAllLines(path))
    //    {
    //        if (line.StartsWith("ATOM") || line.StartsWith("HETATM"))
    //        {
    //            var x = float.Parse(line.Substring(30, 8));
    //            var y = float.Parse(line.Substring(38, 8));
    //            var z = float.Parse(line.Substring(46, 8));

    //            var atomSymbol = line.Substring(76, 2).Trim();
    //            //var atomSymbol = line.Substring(13, 1).Trim();
    //            var symbolId = Array.IndexOf(AtomSymbols, atomSymbol);
    //            if (symbolId < 0)
    //            {
    //                Debug.Log("Atom symbol not found: " + atomSymbol);
    //                symbolId = 0;
    //            }

    //            var atom = new Vector4(x, y, z, symbolId);
    //            atoms.Add(atom);
    //        }
    //    }

    //    matrices = new List<Matrix4x4>();
    //    var matrix = new Matrix4x4();

    //    foreach (var line in File.ReadAllLines(path))
    //    {
    //        if (line.StartsWith("REMARK 350"))
    //        {
    //            if (line.Contains("BIOMT1"))
    //            {
    //                matrix = Matrix4x4.identity;
    //                var split = line.Substring(30).Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

    //                matrix[0, 0] = float.Parse(split[0]);
    //                matrix[0, 1] = float.Parse(split[1]);
    //                matrix[0, 2] = float.Parse(split[2]);
    //                matrix[0, 3] = float.Parse(split[3]);
    //            }

    //            if (line.Contains("BIOMT2"))
    //            {
    //                var split = line.Substring(30).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

    //                matrix[1, 0] = float.Parse(split[0]);
    //                matrix[1, 1] = float.Parse(split[1]);
    //                matrix[1, 2] = float.Parse(split[2]);
    //                matrix[1, 3] = float.Parse(split[3]);
    //            }

    //            if (line.Contains("BIOMT3"))
    //            {
    //                var split = line.Substring(30).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

    //                matrix[2, 0] = float.Parse(split[0]);
    //                matrix[2, 1] = float.Parse(split[1]);
    //                matrix[2, 2] = float.Parse(split[2]);
    //                matrix[2, 3] = float.Parse(split[3]);

    //                matrices.Add(matrix);
    //            }
    //        }
    //    }
    //}
}

