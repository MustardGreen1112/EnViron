using UnityEditor;
using UnityEngine;
public class TunnelGenerator : EditorWindow
{
    [System.Serializable]
    internal class Cell
    {
        public string cellName;
        public GameObject cell;
    }

    internal class TunnelSection
    {
        public float radius;
        public Vector3 normal;
    }

    [System.Serializable]
    internal class Noise
    {
        public Texture2D noise;
        public int noiseSize;
        public float noiseVariation;
        public float noiseMean;
    }

    [SerializeField] private Transform[] points;
    [SerializeField] private int sectionDensity = 3;
    [SerializeField] private Cell[] cells;
    [SerializeField] private Noise sectionNoise;
    [SerializeField] private Noise cellNoise;
    [SerializeField] private int seed = 42;
    [SerializeField] private int cellPerSection = 20;

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
        SerializedProperty noiseSizeProperty = serializedObject.FindProperty("noiseSize");
        SerializedProperty sectionNoise = serializedObject.FindProperty("sectionNoise");
        SerializedProperty cellNoise = serializedObject.FindProperty("cellNoise");
        SerializedProperty noiseVariationProperty = serializedObject.FindProperty("noiseVariation");
        SerializedProperty cellsProperty = serializedObject.FindProperty("cells");
        SerializedProperty pointsProperty = serializedObject.FindProperty("points");
        SerializedProperty cellDensityProperty = serializedObject.FindProperty("cellDensity");
        SerializedProperty cellPerSectionProperty = serializedObject.FindProperty("cellPerSection");

        EditorGUILayout.PropertyField(cellsProperty, new GUIContent("Cells"), true);
        EditorGUILayout.PropertyField(pointsProperty, new GUIContent("Points"), true);
        EditorGUILayout.PropertyField(sectionNoise, new GUIContent("Section Noise"), true);
        EditorGUILayout.PropertyField(cellNoise, new GUIContent("Cell Noise"), true);

        sectionDensity = EditorGUILayout.IntField("Section Density", sectionDensity);
        cellPerSection = EditorGUILayout.IntField("Cells Per Section", cellPerSection);

        if (GUILayout.Button("Generate Tunnel")) { GenerateTunnel(); }
        serializedObject.ApplyModifiedProperties();
    }

    #region Visualize Points
    private void OnEnable()
    {
        LoadData();
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SaveData();
        SceneView.duringSceneGui -= OnSceneGUI;
    }
    private void OnSceneGUI(SceneView sceneView)
    {
        if (points == null || points.Length < 2) return;

        Handles.color = Color.green;
        for (int i = 0; i < points.Length - 1; i++)
        {
            if (points[i] != null && points[i + 1] != null)
            {
                Handles.DrawLine(points[i].position, points[i + 1].position);
            }
        }

        SceneView.RepaintAll();
    }
    #endregion

    #region Generate Gaussian Noise Texture Asset
    private Texture2D GetNoiseTexture(int size, float std, float mean)
    {
#if UNITY_EDITOR
        return GenerateNoiseTexture(size, std, mean, true);
#else
        string filename = "GaussianNoiseTexture" + size.ToString() + "x" + size.ToString();
        Texture2D noise = Resources.Load<Texture2D>("GaussianNoiseTextures/" + filename);
        return noise ? noise : GenerateNoiseTexture(size, true);
#endif
    }

    private Texture2D GenerateNoiseTexture(int size, float std, float mean, bool saveIntoAssetFile)
    {
        Texture2D noise = new Texture2D(size, size, TextureFormat.RGFloat, false, true);
        noise.filterMode = FilterMode.Point;
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                Vector2 rv = NormalRandom();
                rv *= std;
                rv += new Vector2(mean, mean);
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

    void GenerateTunnel()
    {
        Debug.Log("Generating tunnel...");
        Debug.Log("Getting noise texture...");
        sectionNoise.noise = GetNoiseTexture(sectionNoise.noiseSize, sectionNoise.noiseVariation, sectionNoise.noiseMean);
        cellNoise.noise = GetNoiseTexture(cellNoise.noiseSize, cellNoise.noiseVariation, cellNoise.noiseMean);
        Debug.Log("Generating cells...");
        GenerateTunnel(sectionDensity, seed);

    }

    void GenerateTunnel(int sectionDensity, int seed)
    {
        if (GameObject.Find("Tunnel")) { DestroyImmediate(GameObject.Find("Tunnel")); }
        GameObject tunnel = new GameObject("Tunnel");
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 startPoint = points[i].position;
            Vector3 endPoint = points[i + 1].position;
            Vector3 direction = (endPoint - startPoint).normalized;
            float segmentLength = Vector3.Distance(startPoint, endPoint);
            float interval = segmentLength / sectionDensity;
            for (int j = 0; j < sectionDensity; j++)
            {
                int idy = (i * sectionDensity + j + seed) / 2 / sectionNoise.noiseSize;
                int idx = (i * sectionDensity + j + seed) / 2 % sectionNoise.noiseSize;
                int idz = (i * sectionDensity + j + seed) % 2;
                Color p = sectionNoise.noise.GetPixel(idy, idx);
                // Set tunnel section radius and positions.
                float tunnelSectionRadius;
                tunnelSectionRadius = idz == 0 ? p.r : p.g;
                if (tunnelSectionRadius < 0.1f) tunnelSectionRadius = 0.1f;
                TunnelSection section = new TunnelSection() { normal = direction, radius = tunnelSectionRadius };
                Cell cell = cells[0];
                for (int k = 0; k < cellPerSection; k++)
                {
                    float theta_interval = 2.0f * Mathf.PI / (float)cellPerSection;
                    float theta_offset = UnityEngine.Random.Range(0.0f, theta_interval);
                    float theta = k * theta_interval + theta_offset;
                    Vector3 offset = new Vector3(
                        section.radius * Mathf.Cos(theta),
                        section.radius * Mathf.Sin(theta),
                        0.0f
                    );
                    Quaternion rotation = Quaternion.LookRotation(section.normal);
                    Vector3 position = startPoint + direction * (j * interval) + rotation * offset;
                    GameObject cellInstance = (GameObject)PrefabUtility.InstantiatePrefab(cell.cell);
                    cellInstance.transform.position = position;
                    cellInstance.transform.rotation = rotation;
                    cellInstance.transform.parent = tunnel.transform;
                    float cellSize;
                    if (k % 2 == 0)
                    {
                        cellSize = cellNoise.noise.GetPixel((i * sectionDensity + j + seed) % cellNoise.noiseSize, k % cellNoise.noiseSize).r;
                    }
                    else
                    {
                        cellSize = cellNoise.noise.GetPixel((i * sectionDensity + j + seed) % cellNoise.noiseSize, k % cellNoise.noiseSize).g;
                    }
                    cellSize = Mathf.Max(0.08f, cellSize);
                    cellInstance.transform.localScale *= cellSize;
                    cellInstance.name = cell.cellName + "_" + "section_" + (i * sectionDensity + j).ToString() + "_" + k.ToString();
                }
            }
        }
    }
}
