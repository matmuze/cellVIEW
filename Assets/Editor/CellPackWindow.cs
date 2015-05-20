//C# Example

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class CellPackWindow : EditorWindow
{
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
                    DisplaySettings.Instance.Scale = EditorGUILayout.Slider("Global scale", DisplaySettings.Instance.Scale, 0.01f, 5);
                    DisplaySettings.Instance.EnableDNAConstraints = EditorGUILayout.Toggle("Enable DNA Constraints", DisplaySettings.Instance.EnableDNAConstraints);
                    DisplaySettings.Instance.DistanceContraint = EditorGUILayout.Slider("Distance Constraint", DisplaySettings.Instance.DistanceContraint, 0.01f, 100);
                    DisplaySettings.Instance.AngularConstraint = EditorGUILayout.Slider("Angular Constraint", DisplaySettings.Instance.AngularConstraint, 0.01f, 100);
                    DisplaySettings.Instance.NumStepsPerSegment = EditorGUILayout.IntField("Num Steps Per Segment", DisplaySettings.Instance.NumStepsPerSegment);

                    DisplaySettings.Instance.EnableTwist = EditorGUILayout.Toggle("Enable Twist", DisplaySettings.Instance.EnableTwist);
                    DisplaySettings.Instance.TwistFactor = EditorGUILayout.Slider("Twist Factor", DisplaySettings.Instance.TwistFactor, 0.01f, 100);

                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space();
                }
                EditorGUILayout.EndVertical();
            }
        }
        EditorGUILayout.EndVertical();
    }
}