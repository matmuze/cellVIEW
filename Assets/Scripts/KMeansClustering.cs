using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//based on python implementation of Kmeans cluster Ludo&Michel Sanner

public class Cluster 
{
    public Vector3 Centroid;
    public Vector3 PreviousCentroid;
    public List<Vector3> Points;
    
    public Cluster(List<Vector3> points)
    {
		// We forbid empty Clusters (they don't make mathematical sense!)
        if (points.Count == 0) 
        {
			throw new Exception ("ILLEGAL: EMPTY CLUSTER");
		}

        Points = new List<Vector3>(points);

        //Figure out what the centroid of this Cluster should be
		Centroid = CalculateCentroid ();
	}

    public float Update(List<Vector3> points)
    {
        PreviousCentroid = Centroid;
        Points = new List<Vector3>(points);
		Centroid = CalculateCentroid ();
		
        //x1,y1,z1 = old_centroid.coords
		//x2,y2,z2 = self.centroid.coords
		
        float shift = 0.0f;
		return Vector3.Distance (Centroid, PreviousCentroid);
	}

	//# Calculates the centroid Point - the centroid is the sample mean Point
	//# (in plain English, the average of all the Points in the Cluster)
    public Vector3 CalculateCentroid()
    {
		Vector3 centroid = Vector3.zero;
		//# For each coordinate:
        foreach (var p in Points)
        {
            centroid = centroid + p;
		}
		//# Return a Point object using the average coordinates
        return (centroid / (float)Points.Count);
	}

	public float RadiusOfGyration()
    {
		float delta = 0.0f;
		foreach (var p in Points)
        {
			float d = Vector3.Distance(p, Centroid);
			delta += d;
		}
		return delta /(float) Points.Count;
	}

	public float BoundingRadius()
    {
		float max = 0.0f;
		foreach (var p in Points) 
        {
			float d=Vector3.Distance(p, Centroid);
			if (d > max)
            {
				max = d;
			}
		}
		return max;
	}

	public Vector2 GetAllRadius()
    {
		float delta = 0.0f;
		float max = 0.0f;

		foreach (var p in Points) 
        {
			float d=Vector3.Distance(p, Centroid);
			
            if (d > max)
            {
				max = d;
			}
			delta+=d;
		}
		return new Vector2 (delta, max);
	}
}

public class KMeansClustering
{
    public static List<Vector4> GetKMeansClusterSpheres(List<Vector3> points, int numSeeds, float scale)
    {
        var clusters = GetKMeansClusters(points, numSeeds, 0.5f);
        return GetClusterSpheres(clusters, scale);
	}

    public static List<Cluster> GetKMeansClusters(List<Vector3> points, int numSeeds, float cutoff, bool debugLog = false)
    {
        if(numSeeds <= 0) throw new Exception("Num seeds too low");

		float smallestDistance = 999999.00f;
		
        // Populare clusters with the seeds
        var clusters = new List<Cluster>();
        for (int i = 0; i < numSeeds; i++)
        {
            var randomPoint = new List<Vector3> {points[UnityEngine.Random.Range(0, points.Count)]};
            clusters.Add(new Cluster(randomPoint));
        }

		//# Enter the program loop
		bool done = false;
		int count = 0;

		while (!done) 
        {
			//# Make a empty list of points for each Cluster
			var lists = clusters.Select(c => new List<Vector3>()).ToList();

            // for each point
            foreach (var p in points)
            {
				//# Figure out which Cluster's centroid is the nearest
                smallestDistance = Vector3.Distance(p, clusters[0].Centroid);//
				
                int index = 0;
                for (int i = 1; i < clusters.Count; i++) 
                {
                    float distance = Vector3.Distance(p, clusters[i].Centroid);
					
                    if (distance < smallestDistance)
                    {
						smallestDistance = distance;
						index = i;
					}
				}

				//# Add this Point to that Cluster's corresponding list
				lists[index].Add (p);
			}

            if(debugLog) Debug.Log("ok one round");
			
            //# Update each Cluster with the corresponding list
			//# Record the biggest centroid shift for any Cluster
			float biggest_shift = 0.0f;

            for (int i = 0; i < clusters.Count; i++)
            {
				if (lists[i].Count != 0)
                {
                    float shift = clusters[i].Update(lists[i]);
					biggest_shift = Mathf.Max (biggest_shift, shift);
					//# If the biggest centroid shift is less than the cutoff, stop
				}
			}

            if (debugLog) Debug.Log("ok one round two pass " + count + " " + biggest_shift);
			
            if (biggest_shift < cutoff) 
            {
				done = true;
				break;
			}

			if (count > 2)
            {
				done = true;
				break;
			}
		}

        return clusters;
    }

    private static List<Vector4> GetClusterSpheres(List<Cluster> clusters, float scale)
    {
		float factor = scale;//default 1.0?, could 0.7

        if (clusters.Count == 0)
			return new List<Vector4>();
		
        List<Vector4> clxyzw = new List<Vector4>();

        for (int i = 0; i < clusters.Count; i++)
        {
			//pos is cluster.centroid.coords
			//radius is rad = radg + factor*(radm-radg)
			//ptsCoords = cluster.points
            Vector2 radgm = clusters[i].GetAllRadius();
			float rad = radgm.x + factor*(radgm.y-radgm.x);
            clxyzw.Add(new Vector4(clusters[i].Centroid.x, clusters[i].Centroid.y, clusters[i].Centroid.z, rad));
		}

		return clxyzw;
   }
}
