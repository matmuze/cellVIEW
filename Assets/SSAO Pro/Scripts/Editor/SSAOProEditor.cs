#if (UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6)
#define UNITY_4_X
#else
#define UNITY_5_X
#endif

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SSAOPro))]
public class SSAOProEditor : Editor
{
	static GUIContent[] c_aoModes = { new GUIContent("1.1 (Legacy)"), new GUIContent("1.2 (Recommended)") };

	SerializedProperty p_aoMode;
	SerializedProperty p_noiseTexture;

#if UNITY_4_X
	SerializedProperty p_useHighPrecisionDepthMap;
#endif

	SerializedProperty p_samples;
	SerializedProperty p_downsampling;
	SerializedProperty p_radius;
	SerializedProperty p_intensity;
	SerializedProperty p_distance;
	SerializedProperty p_bias;
	SerializedProperty p_lumContribution;
	SerializedProperty p_occlusionColor;
	SerializedProperty p_cutoffDistance;
	SerializedProperty p_cutoffFalloff;
	SerializedProperty p_blur;
	SerializedProperty p_blurDownsampling;
	SerializedProperty p_blurPasses;
	SerializedProperty p_blurBilateralThreshold;
	SerializedProperty p_debugAO;

	void OnEnable()
	{
		p_aoMode = serializedObject.FindProperty("Mode");
		p_noiseTexture = serializedObject.FindProperty("NoiseTexture");

#if UNITY_4_X
		p_useHighPrecisionDepthMap = serializedObject.FindProperty("UseHighPrecisionDepthMap");
#endif

		p_samples = serializedObject.FindProperty("Samples");
		p_downsampling = serializedObject.FindProperty("Downsampling");
		p_radius = serializedObject.FindProperty("Radius");
		p_intensity = serializedObject.FindProperty("Intensity");
		p_distance = serializedObject.FindProperty("Distance");
		p_bias = serializedObject.FindProperty("Bias");
		p_lumContribution = serializedObject.FindProperty("LumContribution");
		p_occlusionColor = serializedObject.FindProperty("OcclusionColor");
		p_cutoffDistance = serializedObject.FindProperty("CutoffDistance");
		p_cutoffFalloff = serializedObject.FindProperty("CutoffFalloff");
		p_blur = serializedObject.FindProperty("Blur");
		p_blurDownsampling = serializedObject.FindProperty("BlurDownsampling");
		p_blurPasses = serializedObject.FindProperty("BlurPasses");
		p_blurBilateralThreshold = serializedObject.FindProperty("BlurBilateralThreshold");
		p_debugAO = serializedObject.FindProperty("DebugAO");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		p_aoMode.enumValueIndex = EditorGUILayout.Popup(new GUIContent("SSAO Mode", "SSAO Algorithm"), p_aoMode.enumValueIndex, c_aoModes);

		// Near/far view plane check if Mode == AOMode.V1
		if (p_aoMode.enumValueIndex == (int)SSAOPro.AOMode.V11)
		{
			Camera camera = ((SSAOPro)target).GetComponent<Camera>();

			if (camera.nearClipPlane < 0.3 || (camera.farClipPlane - camera.nearClipPlane) > 5000)
				EditorGUILayout.HelpBox("Check your view planes ! You may experience artifacts and heavy self-shadowing with your current camera settings. Make sure you read the \"Random Notes\" section in the doc.", MessageType.Warning);
		}

		Texture2D noise = (Texture2D)p_noiseTexture.objectReferenceValue;

		EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("Noise Texture");
			EditorGUILayout.BeginHorizontal();
				noise = (Texture2D)EditorGUILayout.ObjectField(noise, typeof(Texture2D), false);
				if (GUILayout.Button("D", EditorStyles.miniButtonLeft)) noise = Resources.Load<Texture2D>("noise");
				if (GUILayout.Button("N", EditorStyles.miniButtonRight)) noise = null;
			EditorGUILayout.EndHorizontal();
		EditorGUILayout.EndHorizontal();

		p_noiseTexture.objectReferenceValue = noise;

#if UNITY_4_X
		EditorGUILayout.PropertyField(p_useHighPrecisionDepthMap, new GUIContent("High Precision Depth Map", "Use a higher precision depth map. Slower but higher quality. You don\'t need this with Unity 5 !"));
#endif

		EditorGUILayout.PropertyField(p_samples, new GUIContent("Sample Count", "Number of ambient occlusion samples (higher is slower)"));
		EditorGUILayout.PropertyField(p_downsampling, new GUIContent("Downsampling", "The resolution at which calculations should be performed (for example, a downsampling value of 2 will work at half the screen resolution)"));
		EditorGUILayout.PropertyField(p_intensity, new GUIContent("Intensity", "Occlusion multiplier (degree of darkness added by ambient occlusion)"));
		EditorGUILayout.PropertyField(p_radius, new GUIContent("Radius", "Sampling radius (in world unit)"));
		EditorGUILayout.PropertyField(p_distance, new GUIContent("Distance", "Distance between an occluded sample and its occluder"));
		EditorGUILayout.PropertyField(p_bias, new GUIContent("Bias", "Adds to the width of the occlusion cone (push this up to reduce artifacts)"));
		EditorGUILayout.PropertyField(p_lumContribution, new GUIContent("Lighting Contribution", "Uses the pixel luminosity to reduce ambient occlusion in bright areas"));
		EditorGUILayout.PropertyField(p_occlusionColor, new GUIContent("Occlusion Color", "Color to use for the occluded areas (pure black leads to better performances)"));

		EditorGUILayout.PropertyField(p_blur, new GUIContent("Blur Type", "Type of blur to apply to the ambient occlusion pass"));

		if (p_blur.intValue != (int)SSAOPro.BlurMode.None)
		{
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(p_blurDownsampling, new GUIContent("Blur Downsampling", "The resolution at which the blur should be performed, see the Downsampling parameter"));
			EditorGUILayout.PropertyField(p_blurPasses, new GUIContent("Blur Passes", "The number of blur passes to apply (more means smoother but slower)"));

			if (p_blur.intValue == (int)SSAOPro.BlurMode.HighQualityBilateral)
				EditorGUILayout.PropertyField(p_blurBilateralThreshold, new GUIContent("Depth Threshold", "Threshold used to compare the depth of each fragment (blur sharpness)"));

			EditorGUI.indentLevel--;
		}

		EditorGUILayout.LabelField("Distance Cutoff", EditorStyles.boldLabel);
		EditorGUI.indentLevel++;
		EditorGUILayout.PropertyField(p_cutoffDistance, new GUIContent("Max Distance", "Stops applying ambient occlusion for samples over this depth (in world unit)"));
		EditorGUILayout.PropertyField(p_cutoffFalloff, new GUIContent("Falloff", "Starts fading the ambient occlusion X units before the Max Distance has been reached (in world unit)"));
		EditorGUI.indentLevel--;

		p_debugAO.boolValue = GUILayout.Toggle(p_debugAO.boolValue, "Show AO", EditorStyles.miniButton);

		if (GUILayout.Button("About", EditorStyles.miniButton))
			SP_StartupWindow.Init(true);

		serializedObject.ApplyModifiedProperties();
	}
}
