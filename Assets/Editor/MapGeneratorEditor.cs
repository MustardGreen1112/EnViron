using System;
using UnityEditor;
using UnityEngine;
using VascularGenerator.DataStructures;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System.Linq;

[System.Serializable]
internal struct CellProportionPair
{
    public GameObject prefab;
    [Range(0.0f, 1.0f)]
    public float weight;
}
[System.Serializable]
internal class Cell
{
    public string cellName;
    public CellProportionPair cell;
    public CellProportionPair cell_variant;
    public CellProportionPair cell_variant_1;
}

[System.Serializable]
internal class Noise
{
    public Texture2D noise;
    public int noiseSize;
    public float noiseVariation;
    public float minValue;
}

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
    [SerializeField] private string splinesJsonName;
    private Tree<VascularSegment> _inletSegments;
    private Dictionary<int, CatmullRomSpline> catmullSplineDictionary;
    [SerializeField] private Noise sectionNoise;
    [SerializeField] private Noise cellNoise;
    [SerializeField] private Cell[] cells;
    private const string EditorPrefsKey = "MapGeneratorKey";

    // Shown on GUI of editor window.
    private void OnGUI()
    {
        GUILayout.Label("Map Generator", EditorStyles.boldLabel);

        // Serialize the fields.
        SerializedObject serializedObject = new SerializedObject(this);
        SerializedProperty treeJsonName = serializedObject.FindProperty("treeJsonName");
        SerializedProperty splinesJsonName = serializedObject.FindProperty("splinesJsonName");

        SerializedProperty cellNoise = serializedObject.FindProperty("cellNoise");
        SerializedProperty cellsProperty = serializedObject.FindProperty("cells");
        SerializedProperty sectionNoise = serializedObject.FindProperty("sectionNoise");

        // Visualize the field we need. 
        EditorGUILayout.PropertyField(treeJsonName, new GUIContent("Tree Json Name"), true);
        EditorGUILayout.PropertyField(splinesJsonName, new GUIContent("Splines Json Name"), true);

        EditorGUILayout.PropertyField(cellsProperty, new GUIContent("Cells"), true);
        EditorGUILayout.PropertyField(sectionNoise, new GUIContent("Section Noise"), true);
        EditorGUILayout.PropertyField(cellNoise, new GUIContent("Cell Noise"), true);

        if (GUILayout.Button("Generate Map"))
        {
            _inletSegments = LoadVascularTreeFromJson();
            List<List<VascularSegment>> splines = GenerateSpline(_inletSegments);

            // Update the curve id in the tree. 
            catmullSplineDictionary = BuildCurves(splines);

            // Visualize the segments
            //VisualizePoints(splines);
            VisualizeSpline(catmullSplineDictionary);

            // Store the updated VascularSegments' curveID in the Json file of the tree. 
            SaveTreeWithCurveID(_inletSegments, this.treeJsonName);

            // Store the curve dictionary in a new Json file. 
            SaveCurveDictionary(catmullSplineDictionary, this.splinesJsonName);
        }

        if (GUILayout.Button("Load Curves"))
        {
            Dictionary<int, CatmullRomSpline> loadedCurves = LoadCurveDictionary(treeJsonName.stringValue);

            if (loadedCurves.Count > 0)
            {
                Debug.Log($"Successfully loaded {loadedCurves.Count} curves");

                // Example: visualize loaded curves
                foreach (var kvp in loadedCurves)
                {
                    Debug.Log($"Curve ID: {kvp.Key}, Points: {kvp.Value.GetPoints().Length}");
                }
            }
            VisualizeSpline(loadedCurves);
        }

        if(GUILayout.Button("Generate cells!"))
        {
            GenerateCells();
        }

        // Apply any modifications to the editor window. 
        serializedObject.ApplyModifiedProperties();
    }

    void GenerateCells()
    {
        Debug.Log("Generating tunnel...");
        Debug.Log("Getting noise texture...");
        sectionNoise.noise = GenerateNoiseTexture(sectionNoise.noiseSize, true);
        cellNoise.noise = GenerateNoiseTexture(cellNoise.noiseSize, true);
        Debug.Log("Generating cells...");
        foreach(int id in catmullSplineDictionary.Keys)
        {
            CatmullRomSpline catspline = catmullSplineDictionary[id];
            GenerateCells(catspline, id);
        }
    }

    void GenerateCells(CatmullRomSpline spline, int id)
    {
        Cell cell = Array.Find(cells, obj => obj.cellName == spline.GetTag());
    }

    void VisualizeSpline(Dictionary<int, CatmullRomSpline> catmullSplineDictionary)
    {
        foreach (int id in catmullSplineDictionary.Keys)
        {
            GameObject spline = new GameObject("Spline_" + id);
            var catSpline = catmullSplineDictionary[id];
            MonoCatSpline monospline = spline.AddComponent<MonoCatSpline>();
            monospline.spline = catSpline;
        }
    }

    /// <summary>
    /// Save the tree with updated curve IDs
    /// </summary>
    private void SaveTreeWithCurveID(Tree<VascularSegment> tree, string originalFileName)
    {
        // Remove .json extension if present
        string baseFileName = originalFileName.Replace(".json", "");
        string newFileName = baseFileName + "_withCurveID.json";
        string path = Path.Combine(Application.dataPath, "Resources", newFileName);

        try
        {
            // Serialize the tree structure
            string json = JsonConvert.SerializeObject(tree, Formatting.Indented, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            });

            File.WriteAllText(path, json);
            AssetDatabase.Refresh();
            Debug.Log($"Tree with curve IDs saved to: {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save tree with curve IDs: {e.Message}");
        }
    }

    private void SaveCurveDictionary(Dictionary<int, CatmullRomSpline> curveDictionary, string originalFileName)
    {
        string baseFileName = originalFileName.Replace(".json", "");
        string newFileName = baseFileName + "_curves.json";
        string path = Path.Combine(Application.dataPath, "Resources", newFileName);

        try
        {
            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            // Convert dictionary to serializable format
            CurveDictionaryData data = new CurveDictionaryData
            {
                curves = curveDictionary.Select(kvp => new CurveEntry
                {
                    curveId = kvp.Key,
                    splineData = kvp.Value.ToSerializable()
                }).ToList()
            };

            string json = JsonConvert.SerializeObject(data, Formatting.Indented);

            File.WriteAllText(path, json);
            AssetDatabase.Refresh();
            Debug.Log($"Curve dictionary saved to: {path}");
            Debug.Log($"Saved {curveDictionary.Count} curves");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save curve dictionary: {e.Message}\n{e.StackTrace}");
        }
    }

    private Dictionary<int, CatmullRomSpline> LoadCurveDictionary(string fileName)
    {
        string baseFileName = fileName.Replace(".json", "");
        string curvesFileName = baseFileName + "_curves.json";
        string path = Path.Combine(Application.dataPath, "Resources", curvesFileName);

        try
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"Curves file not found at: {path}");
                return new Dictionary<int, CatmullRomSpline>();
            }

            string json = File.ReadAllText(path);
            CurveDictionaryData data = JsonConvert.DeserializeObject<CurveDictionaryData>(json);

            Dictionary<int, CatmullRomSpline> curveDictionary = new Dictionary<int, CatmullRomSpline>();

            foreach (var entry in data.curves)
            {
                CatmullRomSpline spline = entry.splineData.FromSerializable();
                curveDictionary.Add(entry.curveId, spline);
            }

            Debug.Log($"Loaded {curveDictionary.Count} curves from: {path}");
            return curveDictionary;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load curve dictionary: {e.Message}\n{e.StackTrace}");
            return new Dictionary<int, CatmullRomSpline>();
        }
    }


    // Build curves and update id in both the curve and VascularSegment.
    Dictionary<int, CatmullRomSpline> BuildCurves(List<List<VascularSegment>> splines)
    {
        Dictionary<int, CatmullRomSpline> dic = new Dictionary<int, CatmullRomSpline>();
        for( int i = 0; i < splines.Count; i++)
        {
            List<VascularSegment> spline = splines[i];
            CatmullRomSpline catSpline = new CatmullRomSpline(spline, i);
            dic.Add(i, catSpline);
            // Update the id of curve in each segment. 
            foreach( var segment in spline)
            {
                segment.curveID = i;
            }
        }
        return dic;
    }


    void VisualizePoints(List<List<VascularSegment>> splines)
    {
        // Visualize the segments
        foreach (List<VascularSegment> spline in splines)
        {
            GameObject parent = new GameObject("Spline");
            Undo.RegisterCreatedObjectUndo(parent, "Create Spline");

            for (int i = 0; i < spline.Count; i++)
            {
                VascularSegment segment = spline[i];

                // First segment: add start point
                if (i == 0)
                {
                    GameObject startPoint = new GameObject($"Point_{i}");
                    startPoint.transform.position = new Vector3(
                        (float)segment.startPoint[0],
                        0f,
                        (float)segment.startPoint[1]
                    );
                    startPoint.transform.SetParent(parent.transform);
                    Undo.RegisterCreatedObjectUndo(startPoint, "Create Spline Point");
                }

                // All segments: add end point
                GameObject endPoint = new GameObject($"Point_{i + 1}");
                endPoint.transform.position = new Vector3(
                    (float)segment.endPoint[0],
                    0f,
                    (float)segment.endPoint[1]
                );
                endPoint.transform.SetParent(parent.transform);
                Undo.RegisterCreatedObjectUndo(endPoint, "Create Spline Point");
            }
        }
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
        try
        {
            Debug.Log("Reading json file...");
            inletSegments = generator.LoadJSON("Assets/Scripts/VascularGeneration/jsons/" + treeJsonName + ".txt");
            Debug.Log("Read successfully!");
            return inletSegments;
        }
        catch (Exception e)
        {
            Debug.LogError("Cannot find or read the json file. Please check MapGeneratorEditor for more details. ");
            Debug.LogError(e.Message);
            return null;
        }
    }

    private List<List<VascularSegment>> GenerateSpline(Tree<VascularSegment> inletSegments)
    {
        Debug.Log("Starting generating spline!");
        // Maintain a list for when we need to go back. Vector2Int(segment ID, leftChildCount waiting to be processed).
        Stack<KeyValuePair<Tree<VascularSegment>, int>> nodesContainsChildren = new Stack<KeyValuePair<Tree<VascularSegment>, int>>();
        // Maintain a list forming the spline. 
        List<VascularSegment> splinePoints = new List<VascularSegment>();

        List<List<VascularSegment>> splines = new List<List<VascularSegment>>();
        GenerateSpline(inletSegments, nodesContainsChildren, splinePoints, splines);
        Debug.Log("Successfully generated the splines. The total numbers of the splines are : " + splines.Count);
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
        if (inletSegments.IsLeaf())
        {
            // If you reach a leaf, add the point to the current spline points list, go back to previous bifurcation nodes, 
            // then create a new list of spline points starting from the parent. 
            splinePoints.Add(inletSegments.value);
            //if (inletSegments.parent != null)
            //{
            //    splinePoints.Add(inletSegments.parent.value);
            //}
            splines.Add(splinePoints);

            KeyValuePair<Tree<VascularSegment>, int> preParentCountsPair;
            if (nodesContainsChildren.TryPop(out preParentCountsPair))
            {
                List<VascularSegment> newSplinePoints = new List<VascularSegment>();
                int leftNodes = preParentCountsPair.Value;
                Tree<VascularSegment> previousParent = preParentCountsPair.Key;
                int totalChildren = previousParent.children.Count;
                inletSegments.visited = true;

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
        if (inletSegments.children.Count > 1)
        {
            splinePoints.Add(inletSegments.value);
            if (inletSegments.visited == false)
            {
                //Add to the stack if there's child(ren) in the node. Store the left numbers as totalchildren - 1 because when we 
                // encountered the node, we are already processing one branch of it. 
                nodesContainsChildren.Push(new KeyValuePair<Tree<VascularSegment>, int>(inletSegments, inletSegments.children.Count - 1));
                GenerateSpline(inletSegments.GetChildren()[0], nodesContainsChildren, splinePoints, splines);
                inletSegments.visited = true;
            }
        }
    }

    #region Generate Gaussian Noise Texture Asset

    private Texture2D GenerateNoiseTexture(int size, bool saveIntoAssetFile)
    {
        Texture2D noise = new Texture2D(size, size, TextureFormat.RGFloat, false, true);
        noise.filterMode = FilterMode.Point;
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                Vector2 rv = NormalRandom();
                noise.SetPixel(i, j, new Vector4(rv.x, rv.y));
            }
        }
        noise.Apply();

#if UNITY_EDITOR
        if (saveIntoAssetFile)
        {
            string filename = "GaussianNoiseTexture" + size.ToString() + "x" + size.ToString();
            string path = "Assets/Resources/GaussianNoiseTextures/";
            AssetDatabase.CreateAsset(noise, path + filename + ".asset");
            Debug.Log("Texture \"" + filename + "\" was created at path \"" + path + "\".");
        }
#endif
        return noise;
    }

    /// <summary>
    /// Build a noise texture with standard normal distribution. 
    /// </summary>
    /// <returns></returns>
    private Vector2 NormalRandom()
    {
        float u1 = UnityEngine.Random.value;
        float u2 = UnityEngine.Random.value;

        float radius = Mathf.Sqrt(-2.0f * Mathf.Log(u1));
        float theta = 2.0f * Mathf.PI * u2;

        Vector2 rv2 = new Vector2(
            radius * Mathf.Cos(theta),
            radius * Mathf.Sin(theta)
        );
        return rv2;
    }
    #endregion
}