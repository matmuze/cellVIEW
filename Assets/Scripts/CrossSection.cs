using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class CrossSection : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		//Debug.Log ("up vector is " + transform.up.ToString ());
		//Debug.Log ("distance is " + Vector3.Distance (transform.position, Vector3.zero));
		DisplaySettings.Instance.CrossSectionPlaneNormal = transform.up;
		DisplaySettings.Instance.CrossSectionPlaneDistance =-Vector3.Dot (transform.position, transform.up);
	}
}
