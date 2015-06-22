using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

//from http://stackoverflow.com/questions/43044/algorithm-to-randomly-generate-an-aesthetically-pleasing-color-palette
//inspired by http://threekings.tk/mirror/ryb_TR.pdf
//and http://afriggeri.github.io/RYB/
public static class ColorGenerator
{
	
	// RYB color space
	private static class RYB
	{
		private static readonly double[] White = { 1, 1, 1 };
		private static readonly double[] Red = { 1, 0, 0 };
		private static readonly double[] Yellow = { 1, 1, 0 };
		private static readonly double[] Blue = { 0.163, 0.373, 0.6 };
		private static readonly double[] Violet = { 0.5, 0, 0.5 };
		private static readonly double[] Green = { 0, 0.66, 0.2 };
		private static readonly double[] Orange = { 1, 0.5, 0 };
		private static readonly double[] Black = { 0.2, 0.094, 0.0 };
		
		public static double[] ToRgb(double r, double y, double b)
		{
			var rgb = new double[3];
			for (int i = 0; i < 3; i++)
			{
				rgb[i] = White[i]  * (1.0 - r) * (1.0 - b) * (1.0 - y) +
					Red[i]    * r         * (1.0 - b) * (1.0 - y) +
						Blue[i]   * (1.0 - r) * b         * (1.0 - y) +
						Violet[i] * r         * b         * (1.0 - y) +
						Yellow[i] * (1.0 - r) * (1.0 - b) *        y +
						Orange[i] * r         * (1.0 - b) *        y +
						Green[i]  * (1.0 - r) * b         *        y +
						Black[i]  * r         * b         *        y;
			}
			
			return rgb;
		}
	}
	
	private class Points : IEnumerable<double[]>
	{
		private readonly int pointsCount;
		private double[] picked;
		private int pickedCount;
		
		private readonly List<double[]> points = new List<double[]>();
		
		public Points(int count)
		{
			pointsCount = count;
		}
		
		private void Generate()
		{
			points.Clear();
			var numBase = (int)Mathf.Ceil(Mathf.Pow((float)pointsCount, 1.0f / 3.0f));
			var ceil = (int)Mathf.Pow((float)numBase, 3.0f);
			for (int i = 0; i < ceil; i++)
			{
				points.Add(new[]
				           {
					Mathf.Floor((float)(i/(double)(numBase*numBase)))/ (numBase - 1.0),
					Mathf.Floor((float)((i/(double)numBase) % numBase))/ (numBase - 1.0),
					Mathf.Floor((float)((double)(i % numBase)))/ (numBase - 1.0),
				});
			}
		}
		
		private double Distance(double[] p1)
		{
			double distance = 0;
			for (int i = 0; i < 3; i++)
			{
				distance += Mathf.Pow((float)(p1[i] - picked[i]) , 2.0f);
			}
			
			return distance;
		}
		
		private double[] Pick()
		{
			if (picked == null)
			{
				picked = points[0];
				points.RemoveAt(0);
				pickedCount = 1;
				return picked;
			}
			
			var d1 = Distance(points[0]);
			int i1 = 0, i2 = 0;
			foreach (var point in points)
			{
				var d2 = Distance(point);
				if (d1 < d2)
				{
					i1 = i2;
					d1 = d2;
				}
				
				i2 += 1;
			}
			
			var pick = points[i1];
			points.RemoveAt(i1);
			
			for (int i = 0; i < 3; i++)
			{
				picked[i] = (pickedCount * picked[i] + pick[i]) / (pickedCount + 1.0);
			}
			
			pickedCount += 1;
			return pick;
		}
		
		public IEnumerator<double[]> GetEnumerator()
		{
			Generate();
			for (int i = 0; i < pointsCount; i++)
			{
				yield return Pick();
			}
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
	
	public static IEnumerable<Color> Generate(int numOfColors)
	{
		var points = new Points(numOfColors);
		
		foreach (var point in points)
		{
			var rgb = RYB.ToRgb(point[0], point[1], point[2]);
			yield return new Color((float)rgb[0],(float)rgb[1],(float)rgb[2]);
			//yield return Color.FromArgb(
			//	(int)Mathf.Floor(255 * rgb[0]),
			//	(int)Mathf.Floor(255 * rgb[1]),
			//	(int)Mathf.Floor(255 * rgb[2]));
		}
	}
}