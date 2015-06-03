using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
    
    public static string DownloadPdbFile(string pdbName, string dstPath)
    {
        Debug.Log("Downloading pdb file");
        var www = new WWW("http://www.rcsb.org/pdb/download/downloadFile.do?fileFormat=pdb&compression=NO&structureId=" + WWW.EscapeURL(pdbName));

        while (!www.isDone)
        {
            EditorUtility.DisplayProgressBar("Download", "Downloading...", www.progress);
        }
        EditorUtility.ClearProgressBar();

        if (!string.IsNullOrEmpty(www.error)) throw new Exception(pdbName + " " + www.error);

        var path = dstPath + pdbName + ".pdb";
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

    public static List<Vector4> ReadAtomClusters(string path)
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
            clusters.Add(new Vector4(-x, y, z, r));
        }

        Debug.Log("Loaded: " + Path.GetFileName(path) + " num clusters: " + clusters.Count);
        return clusters;
    }

    public struct Atom
    {
        public int residue;
        public char symbol;
        public char chainId;
        public int residueId;
        public Vector3 position;
    }

    //http://deposit.rcsb.org/adit/docs/pdb_atom_format.html#ATOM
    public static List<Atom> ReadAtomData(string path, bool bmt = false)
    {
        if (!File.Exists(path)) throw new Exception("File not found at: " + path);

        var atoms = new List<Atom>();
        
        //fix for wrong PDB file when too many atoms in the file
        //we need to go to mmCIF format
        int xi = 30;
        int yi = 38;
        int zi = 46;
        int counter = 0;
        foreach (var line in File.ReadAllLines(path))
        {
            if (line.StartsWith("ATOM"))// || line.StartsWith("HETATM"))
            {
                if (bmt)//check if need to fix
                {
                    if (atoms.Count == 99999)
                    {
                        Debug.Log(atoms.Count + " " + line);
                        xi += 1;
                        yi += 1;
                        zi += 1;
                    }
                }
                /*
                 * try {
                    float.Parse(line.Substring(xi, 8));
                    float.Parse(line.Substring(yi, 8));
                    float.Parse(line.Substring(zi, 8));
                }catch (Exception e) {
                    Debug.Log (line);
                    Debug.Log (line.Substring(xi, 8));
                    Debug.Log (line.Substring(yi, 8));
                    Debug.Log (line.Substring(zi, 8));
                    throw new Exception("Probleme with parsing: "+atoms.Count+" "+xi);
                }
                */
                var x = float.Parse(line.Substring(xi, 8));
                var y = float.Parse(line.Substring(yi, 8));
                var z = float.Parse(line.Substring(zi, 8));

                var atomSymbol = "";
                if (line.Length >= 78)
                {
                    atomSymbol = line.Substring(76, 2).Trim();
                }
                else
                {
                    atomSymbol = line.Substring(13, 1).Trim();
                }

                var symbol = atomSymbol[0];
                var chainId = line.Substring(23, 3)[0];
                var residueId = int.Parse(line.Substring(23, 3));

                var atom = new Atom
                {
                    symbol = symbol,
                    chainId = chainId,
                    residueId = residueId,
                    position = new Vector3(-x, y, z)
                };

                atoms.Add(atom);
            }

            if (line.StartsWith("ENDMDL"))
            {//&&!biomt) {
                //only parse the first model from a multimodel PDB file
                break;
            }
        }

        Debug.Log("Loaded: " + Path.GetFileName(path) + " num atoms: " + atoms.Count);
        return atoms;
    }

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

                var symbol = line.Substring(76, 1)[0];
                var symbolId = Array.IndexOf(AtomSymbols, symbol);
                if (symbolId < 0) symbolId = 0;

                atomSpheres.Add(new Vector4(-x, y, z, AtomRadii[symbolId]));
            }
        }

        Debug.Log("Loaded: " + Path.GetFileName(path) + " num atoms: " + atomSpheres.Count);
        return atomSpheres;
    }

    public static List<Vector4> GetAtomSpheres(List<Atom> atoms)
    {
        var spheres = new List<Vector4>();
        for(int i = 0; i < atoms.Count; i++)
        {
            var symbolId = Array.IndexOf(AtomSymbols, atoms[i].symbol);
            if (symbolId < 0) symbolId = 0;

            spheres.Add(new Vector4(atoms[i].position.x, atoms[i].position.y, atoms[i].position.z, AtomRadii[symbolId]));
        }

        return spheres;
    }
    
    public static List<Vector4> ClusterAtomsByResidue(List<Atom> atoms, int numAtomsPerResidueCluster, float maxRadius, bool logInfo = true)
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
                clusters.AddRange(ClusterSpheres(atomSpheres, numAtomsPerResidueCluster, maxRadius));
                atomSpheres.Clear();
                residueCount ++;
            }
        }

        if (logInfo) Debug.Log("Num residues: " + residueCount + " num clusters: " + clusters.Count);

        return clusters;
    }

    public static List<Vector4> ClusterAtomsByChain(List<Atom> atoms, int numResiduesPerChainCluster, float maxRadius)
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
                var residues = ClusterAtomsByResidue(atomSpheres, 10, 5, false);
                
                // Cluster the residues in the chain
                clusters.AddRange(ClusterSpheres(residues, numResiduesPerChainCluster, maxRadius));
                atomSpheres.Clear();
                chainCount++;
            }
        }

        Debug.Log("Num chains: " + chainCount + " num clusters: " + clusters.Count);

        return clusters;
    }

	public static List<Vector4> ClusterAtomsKmeans(List<Atom> atoms, int nSpheres,float scale)
	{
		//var clusters = new List<Vector4>();
		SphereTree km = new SphereTree ();
		km.setPointsAtoms (atoms);
		km.cluster_N (nSpheres, scale);
		var clusters = km.getClusters (scale);
		return clusters;
	}

    private static List<Vector4> ClusterSpheres(List<Vector4> residueAtoms, int numAtomsPerCluster, float maxRadius)
    {
        var numAtomPerCluster = Mathf.Min(residueAtoms.Count, numAtomsPerCluster);
        var numClusters = (int)Mathf.Floor(residueAtoms.Count / (float)numAtomPerCluster);
        var clusters = new List<Vector4>();

        for (int i = 0; i < numClusters; i++)
        {
            var rangeIndex = i * numAtomPerCluster;
            var rangeCount = numAtomPerCluster;

            if (i == numClusters - 1)
            {
                rangeCount += residueAtoms.Count - (rangeIndex + rangeCount);
            }

            var bounds = GetBounds(residueAtoms.GetRange(rangeIndex, rangeCount));
            var radius = Vector3.Magnitude(bounds.extents) * 0.5f;

            //if (radius > 10)
            //{
            //    int a = 0;
            //}

            var clusterPos = new Vector4(bounds.center.x, bounds.center.y, bounds.center.z, Mathf.Min(radius, maxRadius));
            clusters.Add(clusterPos);
        }

        return clusters;
    }

    //http://deposit.rcsb.org/adit/docs/pdb_atom_format.html#ATOM
	public static List<Vector4> ReadPdbFile(string path,bool bmt=false)
    {
		if (!File.Exists(path)) throw new Exception("File not found at: " + path);

        var atoms = new List<Vector4>();
		//fix for wrong PDB file when too many atoms in the file
		//we need to go to mmCIF format
		int xi = 30;
		int yi = 38;
		int zi = 46;
		int counter = 0;
		foreach (var line in File.ReadAllLines(path))
        {
            if (line.StartsWith("ATOM"))// || line.StartsWith("HETATM"))
            {
				if ( bmt )//check if need to fix
				{
					if (atoms.Count == 99999) {
						Debug.Log (atoms.Count+" "+line);
						xi+=1;
						yi+=1;
						zi+=1;
					}
				}
				/*
				 * try {
					float.Parse(line.Substring(xi, 8));
					float.Parse(line.Substring(yi, 8));
					float.Parse(line.Substring(zi, 8));
				}catch (Exception e) {
					Debug.Log (line);
					Debug.Log (line.Substring(xi, 8));
					Debug.Log (line.Substring(yi, 8));
					Debug.Log (line.Substring(zi, 8));
					throw new Exception("Probleme with parsing: "+atoms.Count+" "+xi);
				}
				*/
				var x = float.Parse(line.Substring(xi, 8));
                var y = float.Parse(line.Substring(yi, 8));
                var z = float.Parse(line.Substring(zi, 8));

                var atomSymbol = "";
                if (line.Length >= 78)
                {
                    atomSymbol = line.Substring(76, 2).Trim();
                }
                else
                {
                    atomSymbol = line.Substring(13, 1).Trim();
                }
                
                var symbolId = Array.IndexOf(AtomSymbols, atomSymbol);
                if (symbolId < 0)
                {
                    //throw new Exception("Atom symbol not found: " + atomSymbol);
                    //Debug.Log("Atom symbol not found: " + atomSymbol);
                    symbolId = 0;
                }

                //should use -Z pdb are right-handed
                //var atom = new Vector4(-x, y, z, symbolId);
                var atom = new Vector4(-x, y, z, AtomRadii[symbolId]);
                atoms.Add(atom);

            }
			if (line.StartsWith("ENDMDL")){//&&!biomt) {
				//only parse the first model from a multimodel PDB file
				break;
			}
        }

        Debug.Log("Loaded: " + Path.GetFileName(path) + " num atoms: " + atoms.Count);
        return atoms;
    }

    //http://deposit.rcsb.org/adit/docs/pdb_atom_format.html#ATOM
	public static List<Vector4> ReadPdbFile_2(string path, bool bmt=false)
    {
        if (!File.Exists(path)) throw new Exception("File not found at: " + path);

        var chainCount = 0;
        var residueCount = 0;
        var endOfChain = false;
        var currentResidueId = -1;
        var residueAtoms = new List<Vector4>();
        var residueClusters = new List<Vector4>();

        if (Path.GetFileName(path).Contains("ABEFIJ"))
        {
            int a = 0;
        }

        foreach (var line in File.ReadAllLines(path))
        {
            var isTer = line.StartsWith("TER") || line.StartsWith("END");
            var isAtom = line.StartsWith("ATOM");
			var isEndml = line.StartsWith("ENDMDL");
            // Flag to mark the begin of a new residue
            bool flushResidue = false;

			if (isEndml)
			{
				flushResidue = true;
				currentResidueId = -1;
			}

            if (isTer)
            {
                flushResidue = true;
                currentResidueId = -1;
            }

            if (isAtom)
            {
                var residueId = int.Parse(line.Substring(23, 3));
                if (currentResidueId == -1) currentResidueId = residueId;
                if (residueId != currentResidueId)
                {
                    flushResidue = true;
                    currentResidueId = residueId;
                }
            }

            // flush residue to cluster list
            if (flushResidue)
            {
                residueClusters.AddRange(ClusterResidue(residueAtoms));
                residueAtoms.Clear();
                residueCount++;
            }

            if (isAtom)
            {
                var x = float.Parse(line.Substring(30, 8));
                var y = float.Parse(line.Substring(38, 8));
                var z = float.Parse(line.Substring(46, 8));

				var atomSymbol = "";
				if (line.Length >= 78)
				{
					atomSymbol = line.Substring(76, 2).Trim();
				}
				else
				{
					atomSymbol = line.Substring(13, 1).Trim();
				}

				var symbolId = Array.IndexOf(AtomSymbols, atomSymbol);
                if (symbolId < 0) { symbolId = 0; }

                residueAtoms.Add(new Vector4(-x, y, z, AtomRadii[symbolId]));
            }
			if (isEndml)
			{
				break;
			}
        }

        // Last residue from the file
        if (residueAtoms.Count > 0) residueClusters.AddRange(ClusterResidue(residueAtoms));

        Debug.Log("Loaded: " + Path.GetFileName(path) + " num chains: " + chainCount + " num residues: " + residueCount + " num clusters: " + residueClusters.Count);
        return residueClusters;
    }

    public static List<Vector4> ClusterResidue(List<Vector4> residueAtoms)
    {
        var numAtomPerCluster = Mathf.Min(residueAtoms.Count, 4);
        var numClusters = (int)Mathf.Floor(residueAtoms.Count / (float)numAtomPerCluster);

        var residueClusters = new List<Vector4>();

        for (int i = 0; i < numClusters; i++)
        {
            var beginRange = i * numAtomPerCluster;
            var rangeSize = numAtomPerCluster;

            if (i == numClusters - 1)
            {
                rangeSize += residueAtoms.Count - (beginRange + rangeSize);
            }

            var bounds = GetBounds(residueAtoms.GetRange(beginRange, rangeSize));
            var radius = Vector3.Magnitude(bounds.extents) + 2.0f;
            var maxRadius = 4.0f;

            //if (radius > 10)
            //{
            //    int a = 0;
            //}
            
            var clusterPos = new Vector4(bounds.center.x, bounds.center.y, bounds.center.z, Mathf.Min(radius, maxRadius));
            
            residueClusters.Add(clusterPos);
        }

        return residueClusters;
    }

    ////http://www.rcsb.org/pdb/101/static101.do?p=education_discussion/Looking-at-Structures/bioassembly_tutorial.html
    public static List<Vector4> ReadBioAssemblyFile(string path)//, out List<Matrix4x4> matrices)
	{
		Debug.Log("GetPdbFilePath got "+path);
		if (!File.Exists(path)) throw new Exception("File not found at: " + path);
		
		var atoms_unit = new List<Vector4>();
		var matrices = new List<Matrix4x4>();
		var matrix = new Matrix4x4();
		
		foreach (var line in File.ReadAllLines(path))
		{
			if (line.StartsWith("REMARK 350"))
			{
				if (line.Contains("BIOMT1"))
				{
					matrix = Matrix4x4.identity;
					var split = line.Substring(30).Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
					
					matrix[0, 0] = float.Parse(split[0]);//RxX
					matrix[0, 1] = float.Parse(split[1]);//RxY
					matrix[0, 2] = float.Parse(split[2]);//RxZ
					matrix[0, 3] = float.Parse(split[3]);//TX
				}
				
				if (line.Contains("BIOMT2"))
				{
					var split = line.Substring(30).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
					
					matrix[1, 0] = float.Parse(split[0]);//RyX
					matrix[1, 1] = float.Parse(split[1]);//RyY
					matrix[1, 2] = float.Parse(split[2]);//RyZ
					matrix[1, 3] = float.Parse(split[3]);//TY
				}
				
				if (line.Contains("BIOMT3"))
				{
					var split = line.Substring(30).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
					
					matrix[2, 0] = float.Parse(split[0]);//RzX
					matrix[2, 1] = float.Parse(split[1]);//RzY
					matrix[2, 2] = float.Parse(split[2]);//RzZ
					matrix[2, 3] = float.Parse(split[3]);//TZ
					//convert to left hand
					//					var m = matrix;//.transpose;
					//					m.SetRow(0,new Vector4(-m[0,0],m[0,1],m[0,2]));
					//					m.SetRow(1,new Vector4(-m[1,0],m[1,1],m[1,2]));
					//					m.SetRow(2,new Vector4(-m[2,0],m[2,1],m[2,2])*-1.0f);
					//					m.SetRow(3,new Vector4(-m[3,0],m[3,1],m[3,2]));
					
					matrices.Add(matrix);
				}
			}
			if (line.StartsWith("ATOM") || line.StartsWith("HETATM"))
			{
				var x = float.Parse(line.Substring(30, 8));
				var y = float.Parse(line.Substring(38, 8));
				var z = float.Parse(line.Substring(46, 8));
				
				//var atomSymbol = line.Substring(76, 2).Trim();
				var atomSymbol = line.Substring(13, 1).Trim();
				//var atomSymbol = line.Substring(13, 1).Trim();
				var symbolId = Array.IndexOf(AtomSymbols, atomSymbol);
				if (symbolId < 0)
				{
					//Debug.Log("Atom symbol not found: " + atomSymbol);
					symbolId = 0;
				}
				
				var atom = new Vector4(x, y, z, symbolId);
				atoms_unit.Add(atom);
			}
		}
		
		
		var atoms = new List<Vector4>();
		for (var i = 0; i < atoms_unit.Count; i++) {
			foreach (var mat in matrices){
				//Debug.Log (matrix.ToString());
				var euler = Helper.euler_from_matrix(mat);
				//Debug.Log (euler.ToString());
				var rotation = Helper.MayaRotationToUnity(new Vector3(euler.x,euler.y,euler.z));
				//var pos = mat.GetRow(3);
				var pos = mat.GetColumn(3);
				var ipos=new Vector3(pos.x,pos.y,pos.z);
				var m = Matrix4x4.TRS(ipos,rotation,new Vector3(1,1,1));
				var ap = new Vector3(atoms_unit[i].x,atoms_unit[i].y,atoms_unit[i].z);
				//var p = rotation*( ap + ipos);
				//Vector3 p = m.MultiplyPoint3x4(ap);
				Vector3 p = new Vector3(0,0,0);
				//mat = mat.transpose;
				p.x= mat[0,3] + (ap.x*mat[0,0]+ap.y*mat[0,1]+ap.z*mat[0,2]); 
				p.y= mat[1,3] + (ap.x*mat[1,0]+ap.y*mat[1,1]+ap.z*mat[1,2]); 
				p.z= mat[2,3] + (ap.x*mat[2,0]+ap.y*mat[2,1]+ap.z*mat[2,2]); 
				var atom = new Vector4(p.x, p.y, p.z, atoms_unit[i].w);
				atoms.Add(atom);
				//break;
			}//break;
		}

        Debug.Log("Loaded " + Path.GetFileName(path) + " num atoms: " + atoms.Count + " using biomt " + matrices.Count + " " + atoms_unit.Count);
		return atoms;
	}
}

