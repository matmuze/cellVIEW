using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
//based on python implementation of Kmeans cluster Ludo&Michel Sanner

public class Point{
//	# Instance variables
//	# self.coords is a list of coordinates for this Point
//	# self.n is the number of dimensions this Point lives in (ie, its space)
//	# self.reference is an object bound to this Point
//	# Initialize new Points
	public Vector3 coords;
	public int n;

	public Point(Vector3 pts){
		coords = pts;
		n = 3;
		//n = (int)pts.Count();
	}
}

public class Cluster {
	
//    # Instance variables
//    # self.points is a list of Points associated with this Cluster
//    # self.n is the number of dimensions this Cluster's Points live in
//    # self.centroid is the sample mean Point of this Cluster
	public List<Point> points;
	public int n;
	public Point centroid;
	public Point old_centroid;
	public Cluster( List<Point> pts){
		//# We forbid empty Clusters (they don't make mathematical sense!)
		if (pts.Count == 0) {
			throw new Exception ("ILLEGAL: EMPTY CLUSTER");
		}
		points = pts;
		n = 3;
//		# We also forbid Clusters containing Points in different spaces
//		# Ie, no Clusters with 2D Points and 3D Points
		foreach (Point p in points) {
			if (p.n != n)
				throw new Exception ("ILLEGAL: MULTISPACE CLUSTER");
		}
//			# Figure out what the centroid of this Cluster should be
		centroid = calculateCentroid ();
	}
	public float update( List<Point> pts ){
		old_centroid = new Point (centroid.coords);//copy ?
		points = new List<Point> (pts);
		centroid = calculateCentroid ();
		//x1,y1,z1 = old_centroid.coords
		//x2,y2,z2 = self.centroid.coords
		float shift = 0.0f;
		return Vector3.Distance (centroid.coords, old_centroid.coords);
	}
	//# Calculates the centroid Point - the centroid is the sample mean Point
	//# (in plain English, the average of all the Points in the Cluster)
	public Point calculateCentroid(){
		Vector3 centroid_coords = new Vector3 (0,0,0);
		//# For each coordinate:
		foreach (Point p in points) {
			centroid_coords = centroid_coords + p.coords;
		}
		//# Return a Point object using the average coordinates
		return new Point (centroid_coords/(float)points.Count);
	}

	public float radiusOfGyration(){
		float delta = 0.0f;
		foreach (Point p in points) {
			float d=Vector3.Distance(p.coords,centroid.coords);
			delta+=d;
		}
		return delta /(float) points.Count;
	}

	public float encapsualtingRadius(){
		float max = 0.0f;
		foreach (Point p in points) {
			float d=Vector3.Distance(p.coords,centroid.coords);
			if (d > max){
				max = d;
			}
		}
		return max;
	}
	public Vector2 getAllRadius(){
		float delta = 0.0f;
		float max = 0.0f;
		foreach (Point p in points) {
			float d=Vector3.Distance(p.coords,centroid.coords);
			if (d > max){
				max = d;
			}
			delta+=d;
		}
		return new Vector2 (delta, max);
	}
			

}

public class SphereTree  {
	public List<Point> points;
	public List<Point> seeds;
	public List<Point> seedsCoords;
	public List<Cluster> clusters;
	public SphereTree( ){
		clusters = new List<Cluster> ();
		seeds = new List<Point> ();
		seedsCoords = new List<Point> ();
	}
	public void setPoints(List<Vector3> positions){
		points = new List<Point> ();
		foreach (Vector3 v in positions) {
			points.Add (new Point(v));
		}
	}

	public void setPointsAtoms(List<PdbLoader.Atom> atoms){
		points = new List<Point> ();
		foreach (PdbLoader.Atom v in atoms) {
			points.Add (new Point(v.position));
		}
	}

	public void cluster_N(int howmany,float scale){
		seeds.Clear ();
		seedsCoords.Clear ();
		for (int i=0; i<howmany; i++) {
			int ind = UnityEngine.Random.Range (0, points.Count);
			seeds.Add (points [ind]);
			seedsCoords.Add (points [ind]);
		}
		kmeans (0.5f);
	}
	public void kmeans(float cutoff){
		//# Randomly sample k Points from the points list, build Clusters around them
		int k = seeds.Count;
		float smallest_distance = 999999.00f;
		clusters = new List<Cluster> ();
		foreach (Point p in seeds) {
			List<Point> pts = new List<Point> ();
			pts.Add (p);
			clusters.Add (new Cluster (pts));
		}
		//# Enter the program loop
		bool done = false;
		int count = 0;
		while (!done) {
			//# Make a list for each Cluster
			List<List<Point>> lists = new List<List<Point>> ();
			foreach (Cluster c in clusters) lists.Add (new List<Point> ());
			//# For each Point:
			foreach (Point p in points) {
				//# Figure out which Cluster's centroid is the nearest
				smallest_distance = Vector3.Distance (p.coords, clusters [0].centroid.coords);//
				int index = 0;
				for (int i=1; i<clusters.Count; i++) {
					float distance = Vector3.Distance (p.coords, clusters [i].centroid.coords);
					if (distance < smallest_distance) {
						smallest_distance = distance;
						index = i;
					}
				}
				//# Add this Point to that Cluster's corresponding list
				lists [index].Add (p);
			}
			Debug.Log ("ok one round");
			//# Update each Cluster with the corresponding list
			//# Record the biggest centroid shift for any Cluster
			float biggest_shift = 0.0f;
			for (int i=0; i<clusters.Count; i++) {
				if (lists [i].Count != 0) {
					float shift = clusters [i].update (lists [i]);
					biggest_shift = Mathf.Max (biggest_shift, shift);
					//# If the biggest centroid shift is less than the cutoff, stop
				}
			}
			Debug.Log ("ok one round two pass "+count+" "+biggest_shift);
			if (biggest_shift < cutoff) {
				done = true;
				break;
			}
			if (count > 2){
				done = true;
				break;
			}
		}
	}
	public List<Vector4> getClusters(float scale){
		float factor = scale;//default 1.0?, could 0.7
		if (clusters.Count == 0)
			return new List<Vector4>();
		List<Vector4> clxyzw = new List<Vector4>();
		for (int i=0; i<clusters.Count; i++) {
			//pos is cluster.centroid.coords
			//radius is rad = radg + factor*(radm-radg)
			//ptsCoords = cluster.points
			Vector2 radgm = clusters[i].getAllRadius();
			float rad = radgm.x + factor*(radgm.y-radgm.x);
			clxyzw.Add (new Vector4(clusters[i].centroid.coords.x,clusters[i].centroid.coords.y,clusters[i].centroid.coords.z,rad));
		}
		return clxyzw;
   }
}
