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
	public float thickness_default;
	public bool showPixelInfo;
	public float ratio=0.0f;
	private float thickness=8.0f;
	//use a cube, 
	//if we use a cylinder we need to / 2.0 the length for the cyl_len
	// Update is called once per frame
	void Update () {
		//start length is 1000
		TextMesh atext = textHolder.GetComponent<TextMesh> ();
		float gscale = DisplaySettings.Instance.Scale;
		float scale = length;//(length * 2.0f) / gscale;
		float cyl_len = (length ) * gscale;
		//transform.LookAt (camera.transform.position);
		float d = Vector3.Distance (Vector3.zero, camera.transform.position);
		//transform.localPosition = camera.ScreenToWorldPoint(new Vector3(0,0,-camera.transform.position.z));//Vector3.zero;//
		//transform.localPosition = new Vector3(-100,0,d);
		transform.localScale = new Vector3 (thickness, cyl_len, thickness);
		transform.localRotation = Quaternion.AngleAxis (90.0f, Vector3.forward);
		transform.position = camera.ScreenToWorldPoint (new Vector3 (0, Screen.height, d));//+new Vector3(cyl_len*4,Screen.height-thickness*8,0);

		float pixelsize = Vector3.Distance(camera.WorldToScreenPoint(transform.GetChild(0).position),
		                                   camera.WorldToScreenPoint(transform.GetChild(1).position));

		//transform.position = transform.position - camera.ScreenToWorldPoint(new Vector3 (cyl_len*(scale / pixelsize)/4.0f, 0.0f, d));

		textHolder.transform.parent = null;
		textHolder.transform.localScale = Vector3.one;
		textHolder.transform.parent = transform;
		ratio = pixelsize / scale;
		//characterSize = targetSizeInWorldUnits*10.0f*1/fontSize;
		//fontsize = targetSizeInWorldUnits*10.0f*1/characterSize
		if (showPixelInfo)
			atext.text = scale.ToString () + "A " + Mathf.Ceil (pixelsize).ToString () + "px\n(" + (ratio).ToString () + "A/px)"+" "+d.ToString();
		else
			atext.text = scale.ToString () + "A "+d.ToString();
		//adapt scaleBar to scene scale
		//Debug.Log ("want " + ratio.ToString()+" "+((1/ratio)*(atext.fontSize))/200.0f);
		atext.characterSize=(((1/ratio)*(atext.fontSize))/1000.0f);
		//also change the scale on x and z
		thickness = gscale*(1/ratio) * thickness_default;
		if  ((ratio > 0.0025f)&&(ratio < 0.055f))
		{
			//atext.fontSize
			length = 10000;
			//atext.characterSize=60;
		}else if  ((ratio > 0.055f)&&(ratio < 0.25f)) {
			length = 1000;
			//atext.characterSize=40;
		}else if ((ratio > 0.25f)&&(ratio < 0.5f)) {
			length = 100;
			//atext.characterSize=20;
		}
 		return;

	}
}
