using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public static class paletteGenerator  {
	/**
	 * fromm http://tools.medialab.sciences-po.fr/iwanthue/js/libs/chroma.palette-gen.js
	 * http://tools.medialab.sciences-po.fr/iwanthue/index.php
      chroma.palette-gen.js - a palette generator for data scientists
	  based on Chroma.js HCL color space
      Copyright (C) 2012  Mathieu Jacomy
  
  	The JavaScript code in this page is free software: you can
      redistribute it and/or modify it under the terms of the GNU
      General Public License (GNU GPL) as published by the Free Software
      Foundation, either version 3 of the License, or (at your option)
      any later version.  The code is distributed WITHOUT ANY WARRANTY;
      without even the implied warranty of MERCHANTABILITY or FITNESS
      FOR A PARTICULAR PURPOSE.  See the GNU GPL for more details.
  
      As additional permission under GNU GPL version 3 section 7, you
      may distribute non-source (e.g., minimized or compacted) forms of
      that code without the copy of the GNU GPL normally required by
      section 4, provided you include this license notice and a URL
      through which recipients can access the Corresponding Source.  
  */
	// TODO fix/add the tolab/torgb/tohcl convertion 
	// v0.1

	private static int K = 18;
	private static float X = 0.950470f;
	private static float Y = 1.0f;
	private static float Z = 1.088830f;

	public delegate bool afunction( Vector3 x);
	public static List<Vector3> colorSamples;
	public static List<int> samplesClosest;
	public static Dictionary<int,List<int>> kmeansClusters;
	public static System.Random rnd;
	public static List<Vector3> kMeans;// = new List<Vector3>(); 

	public static void Clear(){
		colorSamples.Clear ();
		samplesClosest.Clear ();
		kmeansClusters.Clear ();
	}
	public static float  limit (float x,float  min,float  max) {
		//if (min == null) {
	//		min = 0;
	//	}
	//	if (max == null) {
	//		max = 1;
	//	}
		if (x < min) {
			x = min;
		}
		if (x > max) {
			x = max;
		}
		return x;
	}

	public static float finv (float t) {
		if (t > (6.0f / 29.0f)) {
			return t * t * t;
		} else {
			return 3 * (6.0f / 29.0f) * (6.0f / 29.0f) * (t - 4.0f / 29.0f);
		}
	}

	public static  Vector3 lab2xyz (Vector3 lab ) {
		/*
    	Convert from L*a*b* doubles to XYZ doubles
    	Formulas drawn from http://en.wikipedia.org/wiki/Lab_color_spaces
    */
		float sl = (lab.x + 0.16f) / 1.16f;
		Vector3 ill = new Vector3(0.96421f, 1.00000f, 0.82519f);
		float y = ill[1] * finv(sl);
		float x = ill[0] * finv(sl + (lab.y / 5.0f));
		float z = ill[2] * finv(sl - (lab.z / 2.0f));
		return new Vector3(x, y, z);
	}

	public static float correct  (float cl) {
		float a = 0.055f;
		if (cl <= 0.0031308f) {
			return 12.92f * cl;
		} else {
			return (1 + a) * Mathf.Pow(cl, 1 / 2.4f) - a;
		}
	}

	public static  Vector3 xyz2rgb (Vector3 xyz) {
		/*
    	Convert from XYZ doubles to sRGB bytes
    	Formulas drawn from http://en.wikipedia.org/wiki/Srgb
    */
		float rl = 3.2406f * xyz.x - 1.5372f * xyz.y - 0.4986f * xyz.z;
		float gl = -0.9689f * xyz.x + 1.8758f * xyz.y + 0.0415f * xyz.z;
		float bl = 0.0557f * xyz.x - 0.2040f * xyz.y + 1.0570f * xyz.z;
		bool clip = Mathf.Min(Mathf.Min(rl, gl), bl) < -0.001f || Mathf.Max(Mathf.Max(rl, gl), bl) > 1.001f;
		if (clip) {
			rl = rl < 0.0f ? 0.0f : rl > 1.0f ? 1.0f : rl;
			gl = gl < 0.0f ? 0.0f : gl > 1.0f ? 1.0f : gl;
			bl = bl < 0.0f ? 0.0f : bl > 1.0f ? 1.0f : bl;
		}
		//if (clip) {
		//	rl = 0;
		//	gl = 0;
		//	bl = 0;
		//}

		float r = Mathf.Round(255.0f * correct(rl));
		float g = Mathf.Round(255.0f * correct(gl));
		float b = Mathf.Round(255.0f * correct(bl));
		return new Vector3(r, g, b);
	}
	
	public static  Vector3 lab2rgb (Vector3 lab) {
		/*
    	Convert from LAB doubles to sRGB bytes 
    	(just composing the above transforms)
    */	
		//Debug.Log (lab);
		Vector3 xyz = lab2xyz(lab);
		//Debug.Log (xyz);
		return xyz2rgb(xyz);
	}

	public static  Vector3 lab2hcl  (Vector3 lab) {
		/*
    	Convert from a qualitative parameter c and a quantitative parameter l to a 24-bit pixel. These formulas were invented by David Dalrymple to obtain maximum contrast without going out of gamut if the parameters are in the range 0-1.
    	
    	A saturation multiplier was added by Gregor Aisch
    */
		float L = lab.x;
		float l = lab.x;
		l = (l - 0.09f) / 0.61f;
		float r = Mathf.Sqrt(lab.y * lab.y + lab.z * lab.z);
		float s = r / (l * 0.311f + 0.125f);
		float TAU = 6.283185307179586476925287f;
		float angle = Mathf.Atan2(lab.y, lab.z);
		float c = (TAU / 6f - angle) / TAU;
		c *= 360f;
		if (c < 0) c += 360f;
		return new Vector3(c, s, l);
	}

	public static float acorrect (float c) {
		float a = 0.055f;
		if (c <= 0.04045f) {
			return c / 12.92f;
		} else {
			return Mathf.Pow((c + a) / (1 + a), 2.4f);
		}
	}

	public static  Vector3 rgb2xyz  (Vector3 rgb) {
		float rl = acorrect(rgb.x / 255.0f);
		float gl = acorrect(rgb.y / 255.0f);
		float bl = acorrect(rgb.z / 255.0f);
		float x = 0.4124f * rl + 0.3576f * gl + 0.1805f * bl;
		float y = 0.2126f * rl + 0.7152f * gl + 0.0722f * bl;
		float z = 0.0193f * rl + 0.1192f * gl + 0.9505f * bl;
		return  new Vector3(x, y, z);
	}

	public static float fcorrect (float t) {
		if (t > Mathf.Pow(6.0f / 29.0f, 3)) {
			return Mathf.Pow(t, 1f / 3f);
		} else {
			return (1f / 3f) * (29f / 6f) * (29f / 6f) * t + 4.0f / 29.0f;
		}
	}


	public static  Vector3 xyz2lab (Vector3 xyz)  {
		Vector3 ill = new Vector3(0.96421f, 1.00000f, 0.82519f);
		float l = 1.16f * fcorrect(xyz.y / ill[1]) - 0.16f;
		float a = 5f * (fcorrect(xyz.x / ill[0]) - fcorrect(xyz.y / ill[1]));
		float b = 2f * (fcorrect(xyz.y / ill[1]) - fcorrect(xyz.z / ill[2]));
		return new Vector3(l, a, b);
	}
	
	public static  Vector3 rgb2lab(Vector3 rgb) {
		Vector3 xyz = rgb2xyz(rgb);
		return xyz2lab(xyz);
	}
	
	public static bool checkLab(Vector3 lab, afunction checkColor){
		// It will be necessary to check if a Lab color exists in the rgb space.
		//first convert labtorgb
		Vector3 acolor = lab2rgb (lab);
		//Vector3 acolor = new Vector3(lab[0]*255, lab[1]*255, lab[2]*255);
		return !float.IsNaN(acolor[0]) && acolor[0]>=0 && acolor[1]>=0 && acolor[2]>=0 && acolor[0]<256 && acolor[1]<256 && acolor[2]<256 && checkColor(lab);
	}


	// K-Means Mode
	public static bool checkColor2(Vector3 lab, afunction checkColor){
		// Check that a color is valid: it must verify our checkColor condition, but also be in the color space
		//input is lab ?acolor = rgb2lab (acolor);
		Vector3 acolor = lab2hcl (lab);
		return !float.IsNaN(acolor[0]) && acolor[0]>=0 && acolor[1]>=0 && acolor[2]>=0 && acolor[0]<256 && acolor[1]<256 && acolor[2]<256 && checkColor(lab);
	}
	public static bool testfunction(Vector3 acolor){ // This function filters valid colors
		//go to hcl
		Vector3 hcl = lab2hcl (acolor);//.hcl();
		return hcl[0]>=0 && hcl[0]<=360
			&& hcl[1]>=0 && hcl[1]<=3
				&& hcl[2]>=0 && hcl[2]<=1.5f;
	}

	public static int GetRandomFromSample(int clusterId){
		int kk =  rnd.Next(0, kmeansClusters[clusterId].Count);
		//int k =(int) Random.value*;
		int N = kmeansClusters[clusterId][kk];
		//Vector3 c = colorSamples[N];
		return N;//lab2rgb(c)/255.0f;
	}

	public static int GetRandomUniqFromSample(int clusterId, List<int> current_colors){
		if (current_colors.Count == 0) {
			int nid = GetRandomFromSample (clusterId);
			Debug.Log ("found random nid " + nid.ToString ());
			return nid;
		}
		List<int> freeSample = kmeansClusters [clusterId].Where ( val => !current_colors.Contains(val)).ToList();
		int kk = rnd.Next(0, freeSample.Count);
		int N = freeSample [kk];
		return N;//lab2rgb(c)/255.0f;
	}

	public static int GetDistinctFromSample(int clusterId, List<int> current_colors){
		int nColors =  kmeansClusters[clusterId].Count;
		//fareset way 
		float d = 0.0f;
		int nid = 0;
		if (current_colors.Count == 0) {
			nid = GetRandomFromSample (clusterId);
			Debug.Log ("found nid " + nid.ToString ());
			return nid;
		}
		//pick the one that maximized the distance with other ?
		//make the distance matrice 
		List<int> freeSample = kmeansClusters [clusterId].Where ( val => !current_colors.Contains(val)).ToList();
		float[,] distances = new float[freeSample.Count, current_colors.Count];
		float[] D = new float[freeSample.Count];
		for (int i=0;i<freeSample.Count;i++){
			for (int j=0;j<current_colors.Count;j++){
					float dist = trichromaticDistance(colorSamples[kmeansClusters [clusterId][i]],colorSamples[current_colors[j]]);
					distances[i,j]=dist;
					D[i]+=dist;
			}
		}
		//float max =  D.Max();
		int free_nid = System.Array.IndexOf(D, D.Max());
		nid = freeSample [free_nid];
		/*while (current_colors.Contains(nid)) 
		{
				D = D.Where(val => val != D.Max()).ToArray();
				nid = System.Array.IndexOf(D, D.Max());
				Debug.Log ("try another nid "+nid.ToString());
				if (D.Length==0) {
					Debug.Log ("break");
					break;
				}
		}*/
		Debug.Log ("found nid "+nid.ToString());
		return nid;//lab2rgb(c)/255.0f;
	}

	public static void initKmeans(List<Vector3> lcolors){
		kMeans = new List<Vector3>(lcolors); 
	}

	public static List<Vector3> generate(int colorsCount, afunction checkColor, bool forceMode, int quality, bool ultra_precision=false){
			// Default
		rnd = new System.Random();
		if(colorsCount == null)
				colorsCount = 8;
		if(checkColor == null)
			checkColor = testfunction;
		if(forceMode == null)
				forceMode = false;
		if(quality == null)
				quality = 50;
		ultra_precision = ultra_precision || false;
		int steps = 0;
		if(forceMode){
				// Force Vector Mode
				List<Vector3> colors =new List<Vector3>();
				// Init
				List<Vector3> vectors = new List<Vector3>();
				for(int i=0; i<colorsCount; i++){
					// Find a valid Lab color
					Vector3 acolor = new Vector3(Random.value,2*Random.value-1,2*Random.value-1);
					while(!checkLab(acolor,checkColor)){
						acolor = new Vector3(Random.value,2*Random.value-1,2*Random.value-1);
					}
					colors.Add(acolor);
				}
				
				// Force vector: repulsion
				float repulsion = 0.3f;
				float speed = 0.05f;
				steps = quality * 20;
				while(steps > 0){
					// Init
					for(int i=0; i<colors.Count; i++){
						vectors.Add (new Vector3(0,0,0));
						//vectors[i] = {dl:0, da:0, db:0};
					}
					// Compute Force
					for(int i=0; i<colors.Count; i++){
						Vector3 colorA = colors[i];
						for(int j=0; j<i; j++){
							Vector3 colorB = colors[j];
							
							// repulsion force
							float dl = colorA[0]-colorB[0];
							float da = colorA[1]-colorB[1];
							float db = colorA[2]-colorB[2];
							float d = Mathf.Sqrt(Mathf.Pow(dl, 2)+Mathf.Pow(da, 2)+Mathf.Pow(db, 2));
							if(d>0){
								float force = repulsion/Mathf.Pow(d,2);
								
								vectors[i][0] += dl * force / d;
								vectors[i][1] += da * force / d;
								vectors[i][2] += db * force / d;
								
								vectors[j][0] -= dl * force / d;
								vectors[j][1] -= da * force / d;
								vectors[j][2] -= db * force / d;
							} else {
								// Jitter
								vectors[j][0] += 0.02f - 0.04f * Random.value;
								vectors[j][1] += 0.02f - 0.04f * Random.value;
								vectors[j][2] += 0.02f - 0.04f * Random.value;
							}
						}
					}
					// Apply Force
					for(int i=0; i<colors.Count; i++){
						Vector3 acolor = colors[i];
						float displacement = speed * Mathf.Sqrt(Mathf.Pow(vectors[i].x, 2)+Mathf.Pow(vectors[i].y, 2)+Mathf.Pow(vectors[i].z, 2));
						if(displacement>0){
							float ratio = speed * Mathf.Min(0.1f, displacement)/displacement;
							Vector3 candidateLab = new Vector3(acolor[0] + vectors[i].x*ratio, acolor[1] + vectors[i].y*ratio, acolor[2] + vectors[i].z*ratio);
						if(checkLab(candidateLab,checkColor)){
								colors[i] = candidateLab;
							}
						}
					}
					steps-=1;
				}
				IEnumerable<Vector3>  crgb = from c in colors select lab2rgb(c);
				return crgb.ToList();
				//return colors.AsEnumerable.Select(c=>lab2rgb(c));
				//return colors;//colors.map(function(lab){return chroma.lab(lab[0], lab[1], lab[2]);});
			} else {
				bool init=true;
				if (kMeans == null){
					kMeans = new List<Vector3>();
					init = false;
				}
				//List<Vector3> kMeans = new List<Vector3>();
				kmeansClusters = new Dictionary<int,List<int> >();
				for(int i=0; i<colorsCount; i++){
					Vector3 lab = new Vector3(Random.value,2*Random.value-1,2*Random.value-1);
					while(!checkColor2(lab,checkColor)){//chroma.lab(lab)
						lab = new Vector3(Random.value,2*Random.value-1,2*Random.value-1);
					}
					if (!init)kMeans.Add(lab);
					kmeansClusters.Add (i,new List<int>());
				}
				
				colorSamples = new List<Vector3>();
				samplesClosest = new List<int>();
				if(ultra_precision){
					for(float l=0; l<=1; l+=0.01f){
					for(float a=-1; a<=1; a+=0.05f){
						for(float b=-1; b<=1; b+=0.05f){
							if(checkColor2(new Vector3(l, a, b),checkColor)){//chroma.lab(l, a, b)
								colorSamples.Add(new Vector3(l, a, b));
								samplesClosest.Add(-1);
								}
							}
						}
					}
				} else {
				for(float l=0; l<=1; l+=0.05f){
					for(float a=-1; a<=1; a+=0.1f){
						for(float b=-1; b<=1; b+=0.1f){
							if(checkColor2(new Vector3(l, a, b),checkColor)){
								colorSamples.Add(new Vector3(l, a, b));
								samplesClosest.Add(-1);
								}
							}
						}
					}
				}
				
				
				// Steps
				steps = quality;
				while(steps > 0){
					// kMeans -> Samples Closest
					for(int i=0; i<colorSamples.Count; i++){
						Vector3 lab = colorSamples[i];
						float minDistance = 1000000;
						for(int j=0; j<kMeans.Count; j++){
							Vector3 kMean = kMeans[j];
							float distance = Mathf.Sqrt(Mathf.Pow(lab[0]-kMean[0], 2) + Mathf.Pow(lab[1]-kMean[1], 2) + Mathf.Pow(lab[2]-kMean[2], 2));
							if(distance < minDistance){
								minDistance = distance;
								samplesClosest[i] = j;
							}
						}
					}
					
					// Samples -> kMeans
					List<Vector3> freeColorSamples = new List<Vector3>(colorSamples);//colorSamples.slice(0);//copy?
					for(int j=0; j<kMeans.Count; j++){
						int count = 0;
						Vector3 candidateKMean = new Vector3(0, 0, 0);
						Debug.Log ("J "+j.ToString());
						Debug.Log ("Dic "+kmeansClusters.ToString());
						for(int i=0; i<colorSamples.Count; i++){
							if(samplesClosest[i] == j){
								count++;
								candidateKMean[0] += colorSamples[i][0];
								candidateKMean[1] += colorSamples[i][1];
								candidateKMean[2] += colorSamples[i][2];
								kmeansClusters[j].Add (i);
							}
						}
						if(count!=0){
							candidateKMean[0] /= count;
							candidateKMean[1] /= count;
							candidateKMean[2] /= count;
						}
						//chroma.lab ?
						if(count!=0 && checkColor2(new Vector3(candidateKMean[0], candidateKMean[1], candidateKMean[2]),checkColor) && candidateKMean.magnitude!=0){
							kMeans[j] = candidateKMean;
						} else {
							// The candidate kMean is out of the boundaries of the color space, or unfound.
							if(freeColorSamples.Count>0){
								// We just search for the closest FREE color of the candidate kMean
								float minDistance = 10000000000;
								int closest = -1;
								for(int i=0; i<freeColorSamples.Count; i++){
									float distance = Mathf.Sqrt(Mathf.Pow(freeColorSamples[i][0]-candidateKMean[0], 2) + Mathf.Pow(freeColorSamples[i][1]-candidateKMean[1], 2) + Mathf.Pow(freeColorSamples[i][2]-candidateKMean[2], 2));
									if(distance < minDistance){
										minDistance = distance;
										closest = i;
									}
								}
								kMeans[j] = colorSamples[closest];
								
							} else {
								// Then we just search for the closest color of the candidate kMean
								float minDistance = 10000000000;
								int closest = -1;
								for(int i=0; i<colorSamples.Count; i++){
									float distance = Mathf.Sqrt(Mathf.Pow(colorSamples[i][0]-candidateKMean[0], 2) + Mathf.Pow(colorSamples[i][1]-candidateKMean[1], 2) + Mathf.Pow(colorSamples[i][2]-candidateKMean[2], 2));
									if(distance < minDistance){
										minDistance = distance;
										closest = i;
									}
								}
								kMeans[j] = colorSamples[closest];
							}
						}
						//Array.ForEach<int>(intArray, PrintSquare);
					IEnumerable<Vector3> freeColorSamples1 =
						from color in freeColorSamples
							where color[0] != kMeans[j][0]
							|| color[1] != kMeans[j][1]
							|| color[2] != kMeans[j][2]
							select color;
					freeColorSamples = freeColorSamples1.ToList();
					//freeColorSamples = freeColorSamples.filter(function(color){
					//		return color[0] != kMeans[j][0]
					//		|| color[1] != kMeans[j][1]
					//		|| color[2] != kMeans[j][2];
					//	});
					}
				steps-=1;
				}
			IEnumerable<Vector3>  krgb = from c in kMeans select lab2rgb(c)/255.0f;
			return krgb.ToList();// kMeans.map(function(lab){return chroma.lab(lab[0], lab[1], lab[2]);});lab->torgb?
			}
		}
		
	public static List<Vector3> diffSort (List<Vector3>  colorsToSort){
			// Sort
			List<Vector3> diffColors = new List<Vector3> ();
			diffColors.Add (colorsToSort[0]);
			colorsToSort.RemoveAt(0);
			while(colorsToSort.Count > 0){
				int index = -1;
				float maxDistance = -1;
				for(int candidate_index=0; candidate_index<colorsToSort.Count; candidate_index++){
					float d = 1000000000;
					for(int i=0; i<diffColors.Count; i++){
						Vector3 colorA = colorsToSort[candidate_index];//.lab();//to lab
						Vector3 colorB = diffColors[i];//.lab();//to lab
						float dl = colorA[0]-colorB[0];
						float da = colorA[1]-colorB[1];
						float db = colorA[2]-colorB[2];
						d = Mathf.Min(d, Mathf.Sqrt(Mathf.Pow(dl, 2)+Mathf.Pow(da, 2)+Mathf.Pow(db, 2)));
					}
					if(d > maxDistance){
						maxDistance = d;
						index = candidate_index;
					}
				}
				Vector3 color = colorsToSort[index];
				diffColors.Add(color);
			colorsToSort.RemoveAt(index);
			//colorsToSort = colorsToSort.filter(function(c,i){return i!=index;});
			}
			return diffColors;
		}
	// Generate colors (as Chroma.js objects)

	public static void Example(){
		List<Vector3> colors = generate(
		5, // Colors
		testfunction,
		false, // Using Force Vector instead of k-Means
		50 // Steps (quality)
		);
		// Sort colors by differenciation first
		colors = diffSort(colors);
	}

	public static float  trichromaticDistance(Vector3 lab1, Vector3 lab2){
		return Mathf.Sqrt(Mathf.Pow(lab1[0]-lab2[0], 2) + Mathf.Pow(lab1[1]-lab2[1], 2) + Mathf.Pow(lab1[2]-lab2[2], 2));
	}
	
	public static float  redgreenDeficiencyDistance(Vector3 lab1, Vector3 lab2){
		// The a* is the red-green contrast channel in CIE LAB, so we just omit this channel in distance computing!
		return Mathf.Sqrt(Mathf.Pow(lab1[0]-lab2[0], 2) + /* Mathf.pow(lab1[1]-lab2[1], 2) +*/ Mathf.Pow(lab1[2]-lab2[2], 2));
	}

	public static float getColorDistance (Vector3 lab1, Vector3 lab2){
		return trichromaticDistance (lab1, lab2);
		// Proposition for color-blind compliant distance:
		// return 0.3 * trichromaticDistance(lab1, lab2) + 0.7 * redgreenDeficiencyDistance(lab1, lab2);
	}


}
