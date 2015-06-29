using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
[ExecuteInEditMode]
public class ScaleBar : MonoBehaviour {
	// Use this for initialization
	public Camera camera;
	public GameObject textHolder;
	public float length;
	public float thickness;
	public bool showPixelInfo;
	//use a cube, 
	//if we use a cylinder we need to / 2.0 the length for the cyl_len
	// Update is called once per frame
	void Update () {
		float gscale = DisplaySettings.Instance.Scale;
		float scale = length;//(length * 2.0f) / gscale;
		float cyl_len = (length ) * gscale;
		transform.localScale = new Vector3 (thickness, cyl_len, thickness);
		transform.localRotation = Quaternion.AngleAxis (90.0f, Vector3.forward);
		//transform.LookAt (camera.transform.position);
		float d = Vector3.Distance (Vector3.zero, camera.transform.position);
		//transform.localPosition = camera.ScreenToWorldPoint(new Vector3(0,0,-camera.transform.position.z));//Vector3.zero;//
		//transform.localPosition = new Vector3(-100,0,d);
		transform.position = camera.ScreenToWorldPoint (new Vector3 (0, Screen.height, d));//+new Vector3(cyl_len*4,Screen.height-thickness*8,0);
		//left should always be in screen
		//Vector3 to = camera.WorldToScreenPoint (new Vector3(10,10,d));

		float pixelsize = Vector3.Distance(camera.WorldToScreenPoint(transform.GetChild(0).position),
		                                   camera.WorldToScreenPoint(transform.GetChild(1).position));

		//transform.position = transform.position - camera.ScreenToWorldPoint(new Vector3 (cyl_len*(scale / pixelsize)/4.0f, 0.0f, d));

		textHolder.transform.parent = null;
		textHolder.transform.localScale = Vector3.one;
		textHolder.transform.parent = transform;

		if (showPixelInfo)
			textHolder.GetComponent<TextMesh> ().text = scale.ToString () + "A " + Mathf.Ceil (pixelsize).ToString () + "px\n(" + (scale / pixelsize).ToString () + "A/px)";
		else
			textHolder.GetComponent<TextMesh> ().text = scale.ToString () + "A ";
		return;

	}
}
