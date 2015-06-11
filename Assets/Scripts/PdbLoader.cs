using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class PdbLoader
{
    public static float[] AtomRadii = { 1.548f, 1.100f, 1.400f, 1.348f, 1.880f, 1.808f };
    public static string[] AtomSymbols = { "C", "H", "N", "O", "P", "S" };
    public static float[] FluoColors = { 1,0,0,0, 0,1,0,0, 0,0,1,0, 1,1,0,0 };


    public static string DefaultPdbDirectory = Application.dataPath + "/../Data/Default/";

    public struct Atom
    {
        public int residue;
        public int residueId;
        
        public char symbol;
        public char chainId;
        public string name;
        
        public Vector3 position;
    }

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

    public static string DownloadPdbFile(string fileName, string dstPath = null)
    {
        Debug.Log("Downloading pdb file");
        var www = new WWW("http://www.rcsb.org/pdb/download/downloadFile.do?fileFormat=pdb&compression=NO&structureId=" + WWW.EscapeURL(fileName));
        
        #if UNITY_EDITOR
        while (!www.isDone)
        {
            EditorUtility.DisplayProgressBar("Download", "Downloading...", www.progress);
        }
        EditorUtility.ClearProgressBar();
        #endif

        if (!string.IsNullOrEmpty(www.error)) throw new Exception(fileName + " " + www.error);

        var path = (string.IsNullOrEmpty(dstPath) ? DefaultPdbDirectory : dstPath) + fileName + ".pdb";
        File.WriteAllText(path, www.text);

        return path;
    }

    public static string DownloadRecipeFile(string fileName, string dstPath = null)
    {
        Debug.Log("Downloading recipe file");
        var www = new WWW("https://raw.githubusercontent.com/mesoscope/cellPACK_data/master/cellPACK_database_1.1.0/recipes/" + fileName + ".json");
        
        #if UNITY_EDITOR
        while (!www.isDone)
        {
            EditorUtility.DisplayProgressBar("Download", "Downloading...", www.progress);
        }
        EditorUtility.ClearProgressBar();
        #endif

        if (!string.IsNullOrEmpty(www.error)) throw new Exception(fileName + " " + www.error);

        var path = (string.IsNullOrEmpty(dstPath) ? DefaultPdbDirectory : dstPath) + fileName + ".json";
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

    public static List<Vector4> GetAtomSpheres(List<Atom> atoms)
    {
        var spheres = new List<Vector4>();
        for (int i = 0; i < atoms.Count; i++)
        {
            var symbolId = Array.IndexOf(AtomSymbols, atoms[i].symbol);
            if (symbolId < 0) symbolId = 0;

            spheres.Add(new Vector4(atoms[i].position.x, atoms[i].position.y, atoms[i].position.z, AtomRadii[symbolId]));
        }

        return spheres;
    }

    public static List<Vector3> GetAtomPoints(List<Atom> atoms)
    {
        var points = new List<Vector3>();
        for (int i = 0; i < atoms.Count; i++)
        {
            points.Add(new Vector3(atoms[i].position.x, atoms[i].position.y, atoms[i].position.z));
        }
        return points;
    }

    public static bool IsCarbonOnly(List<Atom> atoms)
    {
        foreach (var atom in atoms)
        {
            if (String.CompareOrdinal(atom.name, "CA") != 0) return false;
        }

        return true;
    }

    //----------------------------------------------------------------------------------------------
    
    //http://deposit.rcsb.org/adit/docs/pdb_atom_format.html#ATOM
    public static List<Atom> ReadAtomData(string path)
    {
        if (!File.Exists(path)) throw new Exception("File not found at: " + path);

        var atoms = new List<Atom>();
        
        foreach (var line in File.ReadAllLines(path))
        {
            if (line.StartsWith("ATOM"))// || line.StartsWith("HETATM"))
            {
                var x = float.Parse(line.Substring(30, 8));
                var y = float.Parse(line.Substring(38, 8));
                var z = float.Parse(line.Substring(46, 8));

                var name = line.Substring(12, 4).Trim();
                var chainId = line.Substring(23, 3)[0];
                var residueId = int.Parse(line.Substring(23, 3));

                // Remove numbers from the name
                var t = Regex.Replace(name, @"[\d-]", string.Empty).Trim();
                var symbolId = Array.IndexOf(AtomSymbols, t[0].ToString());
                if (symbolId < 0)
                {
                    throw new Exception("Atom symbol unknown: " + name);
                }

                var atom = new Atom
                {
                    name = name,
                    symbol = name[0],
                    chainId = chainId,
                    residueId = residueId,
                    position = new Vector3(-x, y, z)
                };

                atoms.Add(atom);
            }

            if (line.StartsWith("ENDMDL")) // Only parse first model of MDL files
            {
                break;
            }
        }

        Debug.Log("Loaded: " + Path.GetFileName(path) + " num atoms: " + atoms.Count);
        return atoms;
    }

    //http://deposit.rcsb.org/adit/docs/pdb_atom_format.html#ATOM
    public static List<Vector4> ReadAtomSpheres(string path)
    {
        if (!File.Exists(path)) throw new Exception("File not found at: " + path);

        var atomSpheres = new List<Vector4>();
        
        foreach (var line in File.ReadAllLines(path))
        {
            if (line.StartsWith("ATOM"))// || line.StartsWith("HETATM"))
            {
                var x = float.Parse(line.Substring(30, 8));
                var y = float.Parse(line.Substring(38, 8));
                var z = float.Parse(line.Substring(46, 8));
                var name = line.Substring(12, 4).Trim();

                // Remove numbers from the name
                var t = Regex.Replace(name, @"[\d-]", string.Empty).Trim();
                var symbolId = Array.IndexOf(AtomSymbols, t[0].ToString());
                if (symbolId < 0)
                {
                    throw new Exception("Atom symbol unknown: " + name);
                }

                atomSpheres.Add(new Vector4(-x, y, z, AtomRadii[symbolId]));
            }

            if (line.StartsWith("ENDMDL")) // Only parse first model of MDL files
            {
                break;
            }
        }

        Debug.Log("Loaded: " + Path.GetFileName(path) + " num atoms: " + atomSpheres.Count);
        return atomSpheres;
    }

    //----------------------------------------------------------------------------------------------

    public static List<Vector4> ReadClusterSpheres(string path)
    {
        if (!File.Exists(path)) throw new Exception("File not found at: " + path);

        var clusters = new List<Vector4>();

        foreach (var line in File.ReadAllLines(path))
        {
            var split = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var x = float.Parse(split[0]);
            var y = float.Parse(split[1]);
            var z = float.Parse(split[2]);
            var r = float.Parse(split[3]);

            //should use -Z pdb are right-handed
            clusters.Add(new Vector4(x, y, z, r));
        }

        Debug.Log("Loaded: " + Path.GetFileName(path) + " num clusters: " + clusters.Count);
        return clusters;
    }



    public static List<Vector4> ClusterAtomsByResidue(List<Atom> atoms, int numAtomsPerResidueCluster, float radius, bool logInfo = true)
    {
        var clusters = new List<Vector4>();
        var atomSpheres = new List<Vector4>();

        var residueCount = 0;

        for (int i = 0; i < atoms.Count; i++)
        {
            var symbolId = Array.IndexOf(AtomSymbols, atoms[i].symbol);
            if (symbolId < 0) symbolId = 0;

            atomSpheres.Add(new Vector4(atoms[i].position.x, atoms[i].position.y, atoms[i].position.z, AtomRadii[symbolId]));

            if (i == atoms.Count -1 || atoms[i].residueId != atoms[i + 1].residueId)
            {
                clusters.AddRange(ClusterSpheres(atomSpheres, numAtomsPerResidueCluster, radius));
                atomSpheres.Clear();
                residueCount ++;
            }
        }

        if (logInfo) Debug.Log("Num residues: " + residueCount + " num clusters: " + clusters.Count);

        return clusters;
    }

    public static List<Vector4> ClusterAtomsByChain(List<Atom> atoms, int numResiduesPerChainCluster, float radius)
    {
        var clusters = new List<Vector4>();
        var atomSpheres = new List<Atom>();

        var chainCount = 0;

        for (int i = 0; i < atoms.Count; i++)
        {
            var symbolId = Array.IndexOf(AtomSymbols, atoms[i].symbol);
            if (symbolId < 0) symbolId = 0;

            atomSpheres.Add(atoms[i]);

            if (i == atoms.Count - 1 || atoms[i].chainId != atoms[i + 1].chainId)
            {
                // Cluster the current chain by residue
                var residues = ClusterAtomsByResidue(atomSpheres, 10, radius, false);
                
                // Cluster the residues in the chain
                clusters.AddRange(ClusterSpheres(residues, numResiduesPerChainCluster, radius));
                atomSpheres.Clear();
                chainCount++;
            }
        }

        Debug.Log("Num chains: " + chainCount + " num clusters: " + clusters.Count);

        return clusters;
    }

    // Cluster a set of spheres in a list -- naive approach based to spatial locatlity
    private static List<Vector4> ClusterSpheres(List<Vector4> spheres, int numSpheresPerCluster, float radius)
    {
        var numAtomPerCluster = Mathf.Min(spheres.Count, numSpheresPerCluster);
        var numClusters = (int)Mathf.Floor(spheres.Count / (float)numAtomPerCluster);
        var clusters = new List<Vector4>();

        for (int i = 0; i < numClusters; i++)
        {
            var rangeIndex = i * numAtomPerCluster;
            var rangeCount = numAtomPerCluster;

            if (i == numClusters - 1)
            {
                rangeCount += spheres.Count - (rangeIndex + rangeCount);
            }

            var bounds = GetBounds(spheres.GetRange(rangeIndex, rangeCount));

            var clusterPos = new Vector4(bounds.center.x, bounds.center.y, bounds.center.z, radius);
            clusters.Add(clusterPos);
        }

        return clusters;
    }

	public static List<Vector4> ClusterAtomsKmeans(List<Atom> atoms, int nSpheres, float scale)
	{
		var atomPoints = GetAtomPoints(atoms);
        var clusters = KMeansClustering.GetKMeansClusterSpheres(atomPoints, nSpheres, scale);

        Debug.Log("K mean clusters: " +  clusters.Count);

        return clusters;
	}

    //http://www.rcsb.org/pdb/101/static101.do?p=education_discussion/Looking-at-Structures/bioassembly_tutorial.html
    public static List<Matrix4x4> ReadBiomtData(string path)
    {
        if (!File.Exists(path)) throw new Exception("File not found at: " + path);
        
        var matrices = new List<Matrix4x4>();
        var matrix = new Matrix4x4();

        foreach (var line in File.ReadAllLines(path))
        {
            if (line.StartsWith("REMARK 350"))
            {
                if (line.Contains("BIOMT1"))
                {
                    matrix = Matrix4x4.identity;
                    var split = line.Substring(24).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    matrix[0, 0] = float.Parse(split[0]);
                    matrix[0, 1] = float.Parse(split[1]);
                    matrix[0, 2] = float.Parse(split[2]);
                    matrix[0, 3] = float.Parse(split[3]);
                }

                if (line.Contains("BIOMT2"))
                {
                    var split = line.Substring(24).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    matrix[1, 0] = float.Parse(split[0]);
                    matrix[1, 1] = float.Parse(split[1]);
                    matrix[1, 2] = float.Parse(split[2]);
                    matrix[1, 3] = float.Parse(split[3]);
                }

                if (line.Contains("BIOMT3"))
                {
                    var split = line.Substring(24).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    matrix[2, 0] = float.Parse(split[0]);
                    matrix[2, 1] = float.Parse(split[1]);
                    matrix[2, 2] = float.Parse(split[2]);
                    matrix[2, 3] = float.Parse(split[3]);

                    matrices.Add(matrix);
                }
            }
        }

        Debug.Log("Load biomt: " + Path.GetFileName(path) + " instance count: " + matrices.Count);

        return matrices;
    }

    public static List<Vector4> BuildBiomt(List<Vector4> atomSpheres, List<Matrix4x4> transforms)
    {
        // Code de debug, permet de comparer avec un resultat valide
        // La je load tous les atoms d'un coup et je les transform individuelement
        var biomtSpheres = new List<Vector4>();

        foreach (var transform in transforms)
        {
            var posBiomt = new Vector3(transform.m03, transform.m13, transform.m23);
            var rotBiomt = Helper.RotationMatrixToQuaternion(transform);

            foreach (var sphere in atomSpheres)
            {
                //var atomPos = Helper.QuaternionTransform(rotBiomt, sphere) + posBiomt;
                var atomPos = transform.MultiplyVector(sphere) + posBiomt;
                biomtSpheres.Add(new Vector4(atomPos.x, atomPos.y, atomPos.z, sphere.w));
            }
        }

        return biomtSpheres;
    }
}

