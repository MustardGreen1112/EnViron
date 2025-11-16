using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using VascularGenerator.DataStructures;

public class MapGenerator : MonoBehaviour
{
    public GameObject buildingBlock;
    GameObject parent;
    float scaler = 10; //scalar for radius

    public Tree<VascularSegment> segmentTreeRoot; //THIS IS THE MAP TREE STRUCTURE!!!!!!
    public bool LoadFromDummyGraph;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        Debug.Log("---Starting Program---");

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
            y: y
        );
        

        parent = new GameObject("map");
        Tree<VascularSegment> tree;
        if (LoadFromDummyGraph)
        {
            //loading from JSON
            tree = generator.LoadJSON("Jsons/dummyGraph.txt");
            CreateMesh(tree);
        }
        else
        {
            segmentTreeRoot = generator.inletSegment;
            CreateMesh(segmentTreeRoot);
        }
        
        
        buildingBlock.SetActive(false);

        Debug.Log("---Program Complete---");


    }

    // Update is called once per frame
    void Update()
    {
        
    }


    private void CreateMesh(Tree<VascularSegment> tree)
    {
        List<Tree<VascularSegment>> toVisit = new(){tree};

        while (toVisit.Count > 0)
        {
            //getting the current segment
            Tree<VascularSegment> currentSegment = toVisit[0];
            toVisit.RemoveAt(0);

            //creating an instance of the building block and scaling accoridng to the current segment
            Vector3 startPoint = new Vector3((float)currentSegment.GetValue().startPoint[0], 0, (float)currentSegment.GetValue().startPoint[1]);
            Vector3 endPoint = new Vector3((float)currentSegment.GetValue().endPoint[0], 0, (float)currentSegment.GetValue().endPoint[1]);
            float distance = Vector3.Distance(startPoint, endPoint);

            

            GameObject segment  = Instantiate(buildingBlock, startPoint+(endPoint-startPoint)/2, Quaternion.identity);
            segment.transform.localScale = new Vector3((float)currentSegment.GetValue().radius*scaler, distance/2, (float)currentSegment.GetValue().radius*scaler);
            segment.transform.rotation = Quaternion.FromToRotation(Vector3.up, (endPoint-startPoint).normalized);
            segment.transform.parent = parent.transform;
            //adding the children to the queue
            if(currentSegment.GetChildren().Count==0){continue;}
            foreach (Tree<VascularSegment> c in currentSegment.GetChildren()){toVisit.Add(c);}
            
        }
        
    }
}
