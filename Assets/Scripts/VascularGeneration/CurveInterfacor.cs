using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

//a class that I made to interface with the stored JSON of Song's curves after game start as his stuff is an editorWindow
public class CurveInterfacor
{
    public static Dictionary<int, CatmullRomSpline> LoadCurveDictionary(string fileName)
    {
        string baseFileName = fileName.Replace(".json", "");
        string curvesFileName = baseFileName + "_curves.json";
        string path = Path.Combine(Application.dataPath, "Resources", fileName);
        path = fileName;

        try
        {
            // if (!File.Exists(path))
            // {
            //     Debug.LogError($"Curves file not found at: {path}");
            //     return new Dictionary<int, CatmullRomSpline>();
            // }

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
}
