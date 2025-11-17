using System;
using UnityEditor;
using UnityEngine;

public class TunnelGenerator : EditorWindow
{
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
        public float noiseMean;
        public float minValue;
    }

    [SerializeField] private CatmullRomSpline spline;
    /// <summary>
    /// How many points splatted per catmull segment. 
    /// </summary>
    [SerializeField] private int sectionDensity = 10;
    /// <summary>
    /// Cell prefabs
    /// </summary>
    [SerializeField] private Cell[] cells;
    /// <summary>
    /// Controls the mean and variation of section (shell thichness and mean size). 
    /// </summary>
    [SerializeField] private Noise sectionNoise;
    /// <summary>
    /// Controls the mean and variation of cell size.
    /// </summary>
    [SerializeField] private Noise cellNoise;
    [SerializeField] private int seed = 42;

    private const string EditorPrefsKey = "TunnelGeneratorData";

    [MenuItem("Tools/Tunnel Generator")]
    public static void ShowWindow()
    {
        GetWindow<TunnelGenerator>("Tunnel Generator");
    }
    private void OnGUI()
    {
        GUILayout.Label("Tunnel Generator Settings", EditorStyles.boldLabel);

        SerializedObject serializedObject = new SerializedObject(this);
        SerializedProperty splineProperty = serializedObject.FindProperty("spline");
        SerializedProperty noiseSizeProperty = serializedObject.FindProperty("noiseSize");
        SerializedProperty sectionNoise = serializedObject.FindProperty("sectionNoise");
        SerializedProperty cellNoise = serializedObject.FindProperty("cellNoise");
        SerializedProperty noiseVariationProperty = serializedObject.FindProperty("noiseVariation");
        SerializedProperty cellsProperty = serializedObject.FindProperty("cells");
        SerializedProperty pointsProperty = serializedObject.FindProperty("points");
        SerializedProperty cellDensityProperty = serializedObject.FindProperty("cellDensity");

        EditorGUILayout.PropertyField(splineProperty, new GUIContent("Spline"), true);
        EditorGUILayout.PropertyField(cellsProperty, new GUIContent("Cells"), true);
        EditorGUILayout.PropertyField(sectionNoise, new GUIContent("Section Noise"), true);
        EditorGUILayout.PropertyField(cellNoise, new GUIContent("Cell Noise"), true);

        sectionDensity = EditorGUILayout.IntField("Section Density", sectionDensity);

        //if (GUILayout.Button("Generate Tunnel")) { GenerateTunnel(); }
        serializedObject.ApplyModifiedProperties();
    }

    //#region Visualize Points
    private void OnEnable()
    {
        LoadData();
    }

    private void OnDisable()
    {
        SaveData();
    }

    #region Generate Gaussian Noise Texture Asset
    private Texture2D GetNoiseTexture(int size, float std, float mean)
    {
#if UNITY_EDITOR
        return GenerateNoiseTexture(size, true);
#else
        string filename = "GaussianNoiseTexture" + size.ToString() + "x" + size.ToString();
        Texture2D noise = Resources.Load<Texture2D>("GaussianNoiseTextures/" + filename);
        return noise ? noise : GenerateNoiseTexture(size, true);
#endif
    }

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

    private void SaveData()
    {
        // Serialize data to JSON
        string jsonData = JsonUtility.ToJson(this);
        EditorPrefs.SetString(EditorPrefsKey, jsonData);
    }

    private void LoadData()
    {
        if (EditorPrefs.HasKey(EditorPrefsKey))
        {
            // Deserialize data from JSON
            string jsonData = EditorPrefs.GetString(EditorPrefsKey);
            JsonUtility.FromJsonOverwrite(jsonData, this);
        }
    }

    //void GenerateTunnel()
    //{
    //    Debug.Log("Generating tunnel...");
    //    Debug.Log("Getting noise texture...");
    //    sectionNoise.noise = GetNoiseTexture(sectionNoise.noiseSize, sectionNoise.noiseVariation, sectionNoise.noiseMean);
    //    cellNoise.noise = GetNoiseTexture(cellNoise.noiseSize, cellNoise.noiseVariation, cellNoise.noiseMean);
    //    Debug.Log("Generating cells...");
    //    GenerateTunnel(sectionDensity, seed);
    //}

    //void GenerateTunnel(int sectionDensity, int seed)
    //{
    //    var frames = spline.BuildFrames();
    //    if (GameObject.Find("Tunnel")) { DestroyImmediate(GameObject.Find("Tunnel")); }
    //    GameObject tunnel = new GameObject("Tunnel");
    //    float w0 = cells[0].cell.weight;
    //    float w1 = w0 + cells[0].cell_variant.weight;
    //    float w2 = w1 + cells[0].cell_variant_1.weight;
    //    for (int k = 0; k < sectionDensity * spline.SegmentCount; k++)
    //    {
    //        float globalT = UnityEngine.Random.value; // uniformly along spline length

    //        Vector3 N, B;
    //        spline.SampleFrame(frames, globalT, out N, out B);
    //        int n = Mathf.FloorToInt(globalT * spline.SegmentCount);
    //        float t = (globalT * spline.SegmentCount) - n;
    //        Vector3 P = spline.Eval(n, t);
    //        Vector3 T = spline.EvalTangent(n, t);

    //        // Sample offset direction
    //        float theta = UnityEngine.Random.value * 2f * Mathf.PI;
    //        Vector3 offset = Mathf.Cos(theta) * N + Mathf.Sin(theta) * B;

    //        // Sample point based on noise texture
    //        Color p = sectionNoise.noise.GetPixel(k / 2, k % 2);
    //        float d = Mathf.Max(p.g * Mathf.Sqrt(sectionNoise.noiseVariation) + sectionNoise.noiseMean, sectionNoise.minValue); // Map to [min_radius, mean + variation]
    //        Vector3 finalPos = P + offset * d;

    //        // Sample cell prefab based on weights.
    //        float rw = UnityEngine.Random.value;
    //        GameObject cellPrefab = rw < w0 ? cells[0].cell.prefab :
    //                             (rw < w1 ? cells[0].cell_variant.prefab : cells[0].cell_variant_1.prefab);

    //        // Set cell size. 
    //        float cellSize;
    //        if (k % 2 == 0) cellSize = cellNoise.noise.GetPixel((k + seed) % cellNoise.noiseSize, k % cellNoise.noiseSize).r;
    //        else cellSize = cellNoise.noise.GetPixel((k + seed) % cellNoise.noiseSize, k % cellNoise.noiseSize).g;
    //        cellSize = Mathf.Max(cellNoise.minValue, cellSize * cellNoise.noiseVariation + cellNoise.noiseMean);

    //        // Generate prefab. 
    //        GameObject cellInstance = (GameObject)PrefabUtility.InstantiatePrefab(cellPrefab);
    //        cellInstance.transform.position = finalPos;
    //        cellInstance.transform.parent = tunnel.transform;
    //        cellInstance.transform.localScale *= cellSize;

    //        // Set rotation
    //        Quaternion rotation = Quaternion.LookRotation(N);
    //        cellInstance.transform.rotation = rotation;
    //        cellInstance.name = cells[0].cellName + "_" + (k).ToString();
    //    }
    //}
}
