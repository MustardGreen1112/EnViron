using System;
using UnityEditor;
using UnityEngine;
using VascularGenerator.DataStructures;
using System.Collections.Generic;

public class MapGeneratorEditor : EditorWindow
{
    [MenuItem("Tools/Map Editor")]
    public static void ShowWindow()
    {
        GetWindow<MapGeneratorEditor>("Map Generator Editor");
    }


    // Input parameters. 
    [Header("This is the name of the json file we read from. The json should be stored in Scripts/VascularGeneration/jsons")]
    [SerializeField] private string treeJsonName;
    private Tree<VascularSegment> _inletSegments;
    private const string EditorPrefsKey = "MapGeneratorKey";

    // Shown on GUI of editor window.

    private void OnGUI()
    {
        GUILayout.Label("Map Generator", EditorStyles.boldLabel);

        // Serialize the fields.
        SerializedObject serializedObject = new SerializedObject(this);
        SerializedProperty treeJsonName = serializedObject.FindProperty("treeJsonName");

        // Visualize the field we need. 
        EditorGUILayout.PropertyField(treeJsonName, new GUIContent("Tree .Json Name"), true);

        if (GUILayout.Button("Generate Map")) {
            _inletSegments = LoadVascularTreeFromJson();
            List<List<VascularSegment>> splines = GenerateSpline(_inletSegments);
            Debug.Log(splines.Count);
            foreach(List<VascularSegment> spline in splines)
            {
                GameObject parent = new GameObject("Spline");
                foreach (VascularSegment segment in spline)
                {
                    Undo.RegisterCreatedObjectUndo(parent, "Create Spline");
                    GameObject go1 = new GameObject("Spline Start Point");
                    go1.transform.position = new Vector3((float)segment.startPoint[0], 0f, (float)segment.startPoint[1]);
                    go1.transform.SetParent(parent.transform);
                    Undo.RegisterCreatedObjectUndo(go1, "Create Spline Point");

                    GameObject go2 = new GameObject("Spline End Point");
                    go2.transform.position = new Vector3((float)segment.endPoint[0], 0f, (float)segment.endPoint[1]);

                    Vector3 dir = (go2.transform.position - go1.transform.position).normalized;
                    go2.transform.position -= dir * 0.1f;

                    go2.transform.SetParent(parent.transform);
                    Undo.RegisterCreatedObjectUndo(go2, "Create Spline Point");
                }
            }
        }

        // Apply any modifications to the editor window. 
        serializedObject.ApplyModifiedProperties();
    }


    // Store prefs temporaly.
    private void OnEnable()
    {
        LoadData();
    }

    private void OnDisable()
    {
        SaveData();
    }

    private void SaveData()
    {
        string jsonData = JsonUtility.ToJson(this);
        EditorPrefs.SetString(EditorPrefsKey, jsonData);
    }

    private void LoadData()
    {
        if (EditorPrefs.HasKey(EditorPrefsKey))
        {
            string jsonData = EditorPrefs.GetString(EditorPrefsKey);
            JsonUtility.FromJsonOverwrite(jsonData, this);
        }
    }

    // Functionalities. 

    /// <summary>
    /// Load tree from json.
    /// </summary>
    /// <returns></returns>

    private Tree<VascularSegment> LoadVascularTreeFromJson()
    {
        // Constants for realistic microvascular scale
        double perfusionRadius = 100;                // in pixels (1 px = 1 cm → 1 m radius domain)
        int numberTerminalSegments = 100;
        double terminalPressure = 20;               // 60 mmHg in Pascals
        double inletPressure = 400;                 // 100 mmHg in Pascals
        double inletFlow = 1000;                     // 500 μL/min in m³/s (approximate)
        double y = 3.0;                               // Murray's law exponent

        VascularGeneration generator = new VascularGeneration(
            perfusionRadius: (int)perfusionRadius,
            numberTerminalSegments: numberTerminalSegments,
            terminalPressure: terminalPressure,
            inletPressure: inletPressure,
            inletFlow: inletFlow,
            y: y,
            //these values are not used. 
            useVolumeOptimization: false,
            generateOnStart: false
        );

        Tree<VascularSegment> inletSegments;
        try {
            Debug.Log("Reading json file...");
            inletSegments = generator.LoadJSON("Assets/Scripts/VascularGeneration/jsons/" + treeJsonName + ".txt");
            Debug.Log("Read successfully!");
            return inletSegments;
        }
        catch (Exception e) { 
            Debug.LogError("Cannot find or read the json file. Please check MapGeneratorEditor for more details. ");
            Debug.LogError(e.Message);
            return null; }
    }

    private List<List<VascularSegment>> GenerateSpline(Tree<VascularSegment> inletSegments)
    {
        // Maintain a list for when we need to go back. Vector2Int(segment ID, leftChildCount waiting to be processed).
        Stack<KeyValuePair<Tree<VascularSegment>, int>> nodesContainsChildren = new Stack<KeyValuePair<Tree<VascularSegment>, int>>();
        // Maintain a list forming the spline. 
        List<VascularSegment> splinePoints = new List<VascularSegment>();

        List<List<VascularSegment>> splines = new List<List<VascularSegment>>();
        GenerateSpline(inletSegments, nodesContainsChildren, splinePoints, splines);
        return splines;
    }

    /// <summary>
    /// Generate splines from the tree. Using DSP to iterate the tree. 
    /// </summary>
    /// <param name="inletSegments"></param>
    private void GenerateSpline(Tree<VascularSegment> inletSegments, 
        Stack<KeyValuePair<Tree<VascularSegment>, int>> nodesContainsChildren, 
        List<VascularSegment> splinePoints, List<List<VascularSegment>> splines)
    {
        Debug.Log("Starting generating spline!");
        Debug.Log(splines.Count);
        if (inletSegments.IsLeaf())
        {
            Debug.Log("reaching a leaf.");
            // If you reach a leaf, add the point to the current spline points list, go back to previous bifurcation nodes, 
            // then create a new list of spline points starting from the parent. 
            splinePoints.Add(inletSegments.value);
            if(inletSegments.parent != null)
            {
                splinePoints.Add(inletSegments.parent.value);
            }
            splines.Add(splinePoints);

            KeyValuePair<Tree<VascularSegment>, int> preParentCountsPair;
            if (nodesContainsChildren.TryPop(out preParentCountsPair))
            {
                List<VascularSegment> newSplinePoints = new List<VascularSegment>();
                int leftNodes = preParentCountsPair.Value;
                Tree<VascularSegment> previousParent = preParentCountsPair.Key;
                int totalChildren = previousParent.children.Count;
                inletSegments.visited = true;

                // Process the next child. 
                Debug.Log("processing:  " + (totalChildren - leftNodes));
                Debug.Log("total number of child for the parent is: " + totalChildren);

                GenerateSpline(previousParent.children[totalChildren - leftNodes], nodesContainsChildren, newSplinePoints, splines);

                leftNodes = preParentCountsPair.Value - 1;
                if (leftNodes > 0)
                {
                    nodesContainsChildren.Push(
                        new KeyValuePair<Tree<VascularSegment>, int>(previousParent, leftNodes)
                    );
                }
            }
        }

        // If this is a intermediate node, add it to the splinepoints list and go further to the next node. 
        if (inletSegments.children.Count == 1)
        {
            splinePoints.Add(inletSegments.value);
            inletSegments.visited = true;
            GenerateSpline(inletSegments.GetChildren()[0], nodesContainsChildren, splinePoints, splines);
        }

        // If this is a bifurcation node, add it to the splinepoints list and add it to the nodesContainsChildren list if needed. 
        if(inletSegments.children.Count > 1)
        {
            splinePoints.Add(inletSegments.value);
            if (!nodesContainsChildren.Contains(new KeyValuePair<Tree<VascularSegment>, int>(inletSegments, inletSegments.children.Count)) 
                && inletSegments.visited == false)
            {
                Debug.Log("Adding inspected nodes");
                //Add to the stack if there's child(ren) in the node. Store the left numbers as totalchildren - 1 because when we 
                // encountered the node, we are already processing one branch of it. 
                nodesContainsChildren.Push(new KeyValuePair<Tree<VascularSegment>, int>(inletSegments, inletSegments.children.Count-1));
                GenerateSpline(inletSegments.GetChildren()[0], nodesContainsChildren, splinePoints, splines);
                inletSegments.visited = true;
            }
        }

        //// Maintain a list for when we need to go back. Vector2Int(segment ID, leftChildCount waiting to be processed).
        //Stack<Vector2Int> nodesContainsChildren = new Stack<Vector2Int>();
        //// Maintain a list forming the spline. 
        //List<VascularSegment> splinePoints = new List<VascularSegment>();

        //// Add current node the the tree.
        //int childrenCount = inletSegments.children.Count;
        //splinePoints.Add(inletSegments.value);

        //if (inletSegments.children.Count != 0)
        //{
        //    nodesContainsChildren.Push(new Vector2Int(inletSegments.ID, childrenCount));
        //}
        //else
        //{

        }
}
