using UnityEngine; 
using UnityEditor;
using System; 
using System.Text.RegularExpressions;
  
public class SP_StartupWindowProcessor : AssetPostprocessor
{
	static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
	{
		string[] entries = System.Array.FindAll(importedAssets, name => name.Contains("SP_StartupWindow") && !name.EndsWith(".meta"));

		for (int i = 0; i < entries.Length; i++)
			if (SP_StartupWindow.Init(false))
				break;
	} 
}  
 
public class SP_StartupWindow : EditorWindow
{
	public static string identifier = "TH_SSAOPro";
	static string pathChangelog = "Assets/SSAO Pro/Changelog.txt";
	static string pathImages = "Assets/SSAO Pro/Scripts/Editor/Startup/Images/";

	Texture2D headerPic;
	string changelogText = "";
	Vector2 changelogScroll = Vector2.zero;
	GUIStyle richLabelStyle;
	GUIStyle richButtonStyle;
	GUIStyle iconButtonStyle;
	Texture2D iconTypogenic;
	Texture2D iconColorful;
	Texture2D iconChromatica;
	Texture2D iconSSAOPro;
	 
	[MenuItem("Help/About SSAO Pro", false, 0)]
	public static void MenuInit()
	{
		SP_StartupWindow.Init(true);
	}

	[MenuItem("Help/SSAO Pro Manual", false, 0)]
	public static void MenuManual()
	{
		Application.OpenURL("http://thomashourdel.com/ssaopro/doc/");
	}

	public static void FindAssets()
	{
		// Get the relative data path
		string[] results = AssetDatabase.FindAssets("SSAOPro_v2", null);
		if (results.Length > 0)
		{
			string p = AssetDatabase.GUIDToAssetPath(results[0]);
			p = System.IO.Path.GetDirectoryName(p);
			p = p.Substring(0, p.LastIndexOf('/'));
			pathChangelog = p + "/Changelog.txt";
			pathImages = p + "/Scripts/Editor/Startup/Images/";
		}
	}

	public static bool Init(bool forceOpen)
	{
		FindAssets();

		// First line in the changelog is the version string
		string version = ((TextAsset)Resources.LoadAssetAtPath(pathChangelog, typeof(TextAsset))).text.Split('\n')[0];

		if (forceOpen || EditorPrefs.GetString(identifier) != version)
		{
			SP_StartupWindow window;
			window = EditorWindow.GetWindow<SP_StartupWindow>(true, "About SSAO Pro", true);
			window.minSize = new Vector2(530, 650);
			window.maxSize = new Vector2(530, 650);
			window.ShowUtility();

			EditorPrefs.SetString(identifier, version);

			return true;
		}
		else return false;
	}

	void OnEnable()
	{
		FindAssets();

		string versionColor = EditorGUIUtility.isProSkin ? "#ffffffee" : "#000000ee";
		changelogText = ((TextAsset)Resources.LoadAssetAtPath(pathChangelog, typeof(TextAsset))).text;
		changelogText = Regex.Replace(changelogText, @"^[0-9].*", "<color=" + versionColor + "><size=13><b>Version $0</b></size></color>", RegexOptions.Multiline);
		changelogText = Regex.Replace(changelogText, @"^-.*", "  $0", RegexOptions.Multiline);

		headerPic = (Texture2D)Resources.LoadAssetAtPath(pathImages + "header.jpg", typeof(Texture2D));
		iconTypogenic = (Texture2D)Resources.LoadAssetAtPath(pathImages + "icon-typogenic.png", typeof(Texture2D));
		iconColorful = (Texture2D)Resources.LoadAssetAtPath(pathImages + "icon-colorful.png", typeof(Texture2D));
		iconChromatica = (Texture2D)Resources.LoadAssetAtPath(pathImages + "icon-chromatica.png", typeof(Texture2D));
		iconSSAOPro = (Texture2D)Resources.LoadAssetAtPath(pathImages + "icon-ssaopro.png", typeof(Texture2D));
	}

	void OnGUI()
	{
		if (richLabelStyle == null)
		{
			richLabelStyle = new GUIStyle(GUI.skin.label);
			richLabelStyle.richText = true;
			richLabelStyle.wordWrap = true;
			richButtonStyle = new GUIStyle(GUI.skin.button);
			richButtonStyle.richText = true;
			iconButtonStyle = new GUIStyle(GUI.skin.button);
			iconButtonStyle.normal.background = null;
			iconButtonStyle.imagePosition = ImagePosition.ImageOnly;
			iconButtonStyle.fixedWidth = 80;
			iconButtonStyle.fixedHeight = 80;
		}

		Rect headerRect = new Rect(0, 0, 530, 207);
		GUI.DrawTexture(headerRect, headerPic, ScaleMode.ScaleAndCrop, false);

#if (UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3)
		GUILayout.Space(204);
#else
		GUILayout.Space(214);
#endif

		GUILayout.BeginVertical();

			// Doc
			GUILayout.BeginHorizontal();

				if (GUILayout.Button("<b>Documentation</b>\n<size=9>Complete manual, examples, tips & tricks</size>", richButtonStyle, GUILayout.MaxWidth(260), GUILayout.Height(36)))
					Application.OpenURL("http://thomashourdel.com/ssaopro/doc/");

				if (GUILayout.Button("<b>Rate it</b>\n<size=9>Leave a review on the Asset Store</size>", richButtonStyle, GUILayout.Height(36)))
					Application.OpenURL("com.unity3d.kharma:content/22369");

			GUILayout.EndHorizontal();

			// Contact
			HR(4, 2);

			GUILayout.BeginHorizontal();

				if (GUILayout.Button("<b>E-mail</b>\n<size=9>thomas.hourdel@gmail.com</size>", richButtonStyle, GUILayout.MaxWidth(172), GUILayout.Height(36)))
					Application.OpenURL("mailto:thomas.hourdel@gmail.com");

				if (GUILayout.Button("<b>Twitter</b>\n<size=9>@Chman</size>", richButtonStyle, GUILayout.Height(36)))
					Application.OpenURL("http://twitter.com/Chman");

				if (GUILayout.Button("<b>Support Forum</b>\n<size=9>Unity Community</size>", richButtonStyle, GUILayout.MaxWidth(172), GUILayout.Height(36)))
					Application.OpenURL("http://forum.unity3d.com/threads/ssao-pro-high-quality-screen-space-ambient-occlusion.274003/");

			GUILayout.EndHorizontal();

			// Changelog
			HR(4, 0);

			changelogScroll = GUILayout.BeginScrollView(changelogScroll);
			GUILayout.Label(changelogText, richLabelStyle);
			GUILayout.EndScrollView();

			// Promo
			HR(0, 0);

			GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();

				if (GUILayout.Button(iconTypogenic, iconButtonStyle))
					Application.OpenURL("com.unity3d.kharma:content/19182");

				if (GUILayout.Button(iconChromatica, iconButtonStyle))
					Application.OpenURL("com.unity3d.kharma:content/20743");

				if (GUILayout.Button(iconColorful, iconButtonStyle))
					Application.OpenURL("com.unity3d.kharma:content/3842");

				if (GUILayout.Button(iconSSAOPro, iconButtonStyle))
					Application.OpenURL("com.unity3d.kharma:content/22369");

				GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

		GUILayout.EndVertical();
	}

	void HR(int prevSpace, int nextSpace)
	{
		GUILayout.Space(prevSpace);
		Rect r = GUILayoutUtility.GetRect(Screen.width, 2);
		Color og = GUI.backgroundColor;
		GUI.backgroundColor = Color.black;
		GUI.Box(r, "");
		GUI.backgroundColor = og;
		GUILayout.Space(nextSpace);
	}
}
