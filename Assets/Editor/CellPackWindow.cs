//C# Example

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class CellPackWindow : EditorWindow
{
    //[MenuItem("cellPACK/Compute PDB clusters")]
    //public static void Foo()
    //{
    //    CellPackLoader.ComputeClusters();
    //}

    //[MenuItem("cellPACK/Bar")]
    //public static void Bar()
    //{
    //    var o = FindObjectsOfType(typeof(SceneManager));
    //    foreach (var oo in o)
    //    {
    //        Debug.Log(oo.name);
    //        //Debug.Log(oo.Test);
    //        DestroyImmediate(oo);
    //    }
    //}

    [MenuItem("cellPACK/Debug Commnad")]
    static void AddMoleculeInstance()
    {
        CellPackLoader.DebugAddInstance();
        EditorUtility.SetDirty(SceneManager.Instance);
    }

    // Add menu item named "My Window" to the Window menu
    [MenuItem("cellPACK/Load scene")]
    public static void LoadScene()
    {
        CellPackLoader.ClearScene();
        CellPackLoader.LoadScene();
        EditorUtility.SetDirty(SceneManager.Instance);
    }

    // Add menu item named "My Window" to the Window menu
    [MenuItem("cellPACK/Clear scene")]
    public static void ClearScene()
    {
        CellPackLoader.ClearScene();
        EditorUtility.SetDirty(SceneManager.Instance);
    }

    // Add menu item named "My Window" to the Window menu
    [MenuItem("cellPACK/Show Window")]
    public static void ShowWindow()
    {
        //Show existing window instance. If one doesn't exist, make one.
        GetWindow(typeof(CellPackWindow));
    }

    private bool showOptions;
    private bool showIngredients;
    private bool showLodOptions;
    private bool toggleSelectAll = true;

    private Vector2 _scrollPos;
    private string[] _contourOptionsLabels = new string[] { "Show Contour", "Hide Contour", "Contour Only" };

    void OnGUI()
    {
        EditorUtility.SetDirty(DisplaySettings.Instance);

        GUIStyle style_1 = new GUIStyle();
        style_1.margin = new RectOffset(10, 10, 10, 10);

        EditorGUILayout.BeginVertical(style_1, GUILayout.ExpandWidth(true));
        {
            if (GUILayout.Button("Load Scene"))
            {
                LoadScene();
            }

            if (GUILayout.Button("Clear Scene"))
            {
                ClearScene();
            }

            EditorGUILayout.Space();

            showOptions = EditorGUILayout.Foldout(showOptions, "Show Options");
            if (showOptions)
            {
                GUIStyle style_2 = new GUIStyle(GUI.skin.box);
                style_2.margin = new RectOffset(10, 10, 10, 10);
                style_2.padding = new RectOffset(10,10,10,10);

                EditorGUILayout.BeginVertical(style_2);
                {
                    EditorGUILayout.LabelField("Base Settings", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    DisplaySettings.Instance.Scale = EditorGUILayout.Slider("Global scale", DisplaySettings.Instance.Scale, 0.01f, 1);
                    DisplaySettings.Instance.ContourStrength = EditorGUILayout.Slider("Contour strength", DisplaySettings.Instance.ContourStrength, 0, 1);
                    DisplaySettings.Instance.ContourOptions = EditorGUILayout.Popup("Contours Options", DisplaySettings.Instance.ContourOptions, _contourOptionsLabels);
                    DisplaySettings.Instance.EnableShadows = EditorGUILayout.Toggle("Enable Shadows", DisplaySettings.Instance.EnableShadows);
                    DisplaySettings.Instance.EnableOcclusionCulling = EditorGUILayout.Toggle("Enable Object Culling", DisplaySettings.Instance.EnableOcclusionCulling);
                    DisplaySettings.Instance.DebugObjectCulling = EditorGUILayout.Toggle("Debug Object Culling", DisplaySettings.Instance.DebugObjectCulling);
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("Base Settings", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    DisplaySettings.Instance.EnableDNAConstraints = EditorGUILayout.Toggle("Enable DNA Constraints", DisplaySettings.Instance.EnableDNAConstraints);
                    DisplaySettings.Instance.DistanceContraint = EditorGUILayout.Slider("Distance Constraint", DisplaySettings.Instance.DistanceContraint, 0.01f, 100);
                    DisplaySettings.Instance.AngularConstraint = EditorGUILayout.Slider("Angular Constraint", DisplaySettings.Instance.AngularConstraint, 0.01f, 100);
                    DisplaySettings.Instance.NumStepsPerSegment = EditorGUILayout.IntField("Num Steps Per Segment", DisplaySettings.Instance.NumStepsPerSegment);

                    DisplaySettings.Instance.EnableTwist = EditorGUILayout.Toggle("Enable Twist", DisplaySettings.Instance.EnableTwist);
                    DisplaySettings.Instance.TwistFactor = EditorGUILayout.Slider("Twist Factor", DisplaySettings.Instance.TwistFactor, 0.01f, 100);
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space();

                    DisplaySettings.Instance.EnableBrownianMotion = EditorGUILayout.BeginToggleGroup("Brownian Motion", DisplaySettings.Instance.EnableBrownianMotion);
                    EditorGUI.indentLevel++;

                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndToggleGroup();
                    EditorGUILayout.Space();

                    DisplaySettings.Instance.EnableCrossSection = EditorGUILayout.BeginToggleGroup("Cross Section", DisplaySettings.Instance.EnableCrossSection);
                    EditorGUI.indentLevel++;
                    DisplaySettings.Instance.CrossSectionPlaneNormal = EditorGUILayout.Vector3Field("Plane Normal", DisplaySettings.Instance.CrossSectionPlaneNormal).normalized;
                    DisplaySettings.Instance.CrossSectionPlaneDistance = EditorGUILayout.Slider("Plane Distance", DisplaySettings.Instance.CrossSectionPlaneDistance, 50.0f, -50.0f);
                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndToggleGroup();
                    EditorGUILayout.Space();

                    DisplaySettings.Instance.EnableLod = EditorGUILayout.BeginToggleGroup("Level of Detail", DisplaySettings.Instance.EnableLod);
                    {
                        DisplaySettings.Instance.FirstLevelOffset = EditorGUILayout.FloatField("First Level Being Range", DisplaySettings.Instance.FirstLevelOffset);

                        EditorGUI.indentLevel++;
                            EditorGUILayout.LabelField("Level 0", EditorStyles.boldLabel);
                            EditorGUI.indentLevel++;
                                DisplaySettings.Instance.LodLevels[0] = EditorGUILayout.FloatField("End Range", DisplaySettings.Instance.LodLevels[0]);
                                DisplaySettings.Instance.LodLevels[1] = EditorGUILayout.FloatField("Min Radius", DisplaySettings.Instance.LodLevels[1]);
                                DisplaySettings.Instance.LodLevels[2] = EditorGUILayout.FloatField("Max Radius", DisplaySettings.Instance.LodLevels[2]);
                            EditorGUI.indentLevel--;

                            EditorGUILayout.LabelField("Level 1", EditorStyles.boldLabel);
                            EditorGUI.indentLevel++;
                                DisplaySettings.Instance.LodLevels[4] = EditorGUILayout.FloatField("End Range", DisplaySettings.Instance.LodLevels[4]);
                                DisplaySettings.Instance.LodLevels[5] = EditorGUILayout.FloatField("Min Radius", DisplaySettings.Instance.LodLevels[5]);
                                DisplaySettings.Instance.LodLevels[6] = EditorGUILayout.FloatField("Max Radius", DisplaySettings.Instance.LodLevels[6]);
                            EditorGUI.indentLevel--;
                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndToggleGroup();
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();

            if (SceneManager.Instance.IngredientNames.Count > 0)
            {
                showIngredients = EditorGUILayout.Foldout(showIngredients, "Show Ingredients");
                if (showIngredients)
                {
                    var setDirty = false;

                    var style4 = new GUIStyle();
                    style4.margin = new RectOffset(10, 10, 5, 5);
                    style4.padding = new RectOffset(10, 10, 0, 0);

                    EditorGUILayout.BeginVertical(style4, GUILayout.ExpandWidth(true));
                    var newToggleSelectAll = EditorGUILayout.ToggleLeft("Select All", toggleSelectAll);

                    if (newToggleSelectAll != toggleSelectAll)
                    {
                        for (int i = 0; i < SceneManager.Instance.IngredientToggleFlags.Count; i++)
                        {
                            SceneManager.Instance.IngredientToggleFlags[i] = Convert.ToInt32(newToggleSelectAll);
                        }

                        toggleSelectAll = newToggleSelectAll;
                        setDirty = true;
                    }

                    EditorGUILayout.EndVertical();

                    var style5 = new GUIStyle(GUI.skin.box);
                    style5.margin = new RectOffset(10, 10, 5, 10);
                    style5.padding = new RectOffset(10, 10, 10, 10);

                    _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, style5, GUILayout.ExpandWidth(true));
                    {
                        for (int i = 0; i < SceneManager.Instance.IngredientNames.Count; i++)
                        {
                            var toggle = Convert.ToBoolean(SceneManager.Instance.IngredientToggleFlags[i]);
                            var newToggle = EditorGUILayout.ToggleLeft(SceneManager.Instance.IngredientNames[i], toggle);
                            if (toggle != newToggle)
                            {
                                SceneManager.Instance.IngredientToggleFlags[i] = Convert.ToInt32(newToggle);
                                setDirty = true;
                            }

                            GUILayout.Space(3);
                        }
                    }

                    EditorGUILayout.EndScrollView();

                    if (setDirty)
                    {
                        EditorUtility.SetDirty(SceneManager.Instance);
                        SceneManager.Instance.UploadIngredientToggleData();
                    }
                }
            }

        }
        EditorGUILayout.EndVertical();
    }
}