using System.Collections.Generic;
using System;
using Unity.VisualScripting;
using UnityEngine;
using VascularGenerator.DataStructures;
using Unity.Mathematics;

public class MapGenerator : MonoBehaviour
{
    public List<string> respawnCellTypes;
    public GameObject keyPointMarker;
    public Material keyMaterial;
    public GameObject buildingBlock;
    public Material buildingBlockMaterial;
    [System.NonSerialized] public GameObject mapParent; //this is the minimap object!!!!
    float scaler = 10; //scalar for radius

    public Tree<VascularSegment> segmentTreeRoot; //THIS IS THE MAP TREE STRUCTURE!!!!!!
    public bool useVolumeOptimization;
    public bool LoadFromDummyGraph;
    public string loadJSON_name;
    public bool storeGraphGenerated;
    public string OVERWRITE_JSONStorageName;
 

    int perfusionRadius = 100;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        Debug.Log("---Starting Program---");

        // Constants for realistic microvascular scale
        // double perfusionRadius = 100;                // in pixels (1 px = 1 cm → 1 m radius domain)
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
            useVolumeOptimization: useVolumeOptimization,
            generateOnStart: !LoadFromDummyGraph

        );
        
        buildingBlock.SetActive(true);
        mapParent = new GameObject("mapParent");
        if (LoadFromDummyGraph)
        {
            //loading from JSON
            segmentTreeRoot = generator.LoadJSON("Assets/Scripts/VascularGeneration/jsons/"+loadJSON_name+".txt");
        }
        else
        {
            segmentTreeRoot = generator.inletSegment;
            if (storeGraphGenerated)
            {
                generator.StoreJSON(segmentTreeRoot, "Assets/Scripts/VascularGeneration/jsons/"+OVERWRITE_JSONStorageName+".txt");
            }
        }
        
        

        


        //now we get a list of the nodes that will be brain + respawn nodes
        List<Tree<VascularSegment>> keyNodes = GetKeyNodes(respawnCellTypes, (int)(perfusionRadius/(4*(respawnCellTypes.Count+1))));
        
        if (keyNodes != null)
        {
            foreach(Tree<VascularSegment> node in keyNodes)
            {
                node.isKey = true;
                // GameObject marker = Instantiate(keyPointMarker, new Vector3((float)node.GetValue().GetMidpoint()[0], 0, (float)node.GetValue().GetMidpoint()[1]), Quaternion.identity);
            }
        }
        else
        {
            Debug.Log("was null");
        }

        if (storeGraphGenerated)
        {
            generator.StoreJSON(segmentTreeRoot, "Assets/Scripts/VascularGeneration/jsons/"+OVERWRITE_JSONStorageName+".txt");
        }

        //creating the map mesh
        CreateMesh(segmentTreeRoot);
        

        Debug.Log("---Program Complete---");
        buildingBlock.SetActive(false);
        keyPointMarker.SetActive(false);


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
            segment.transform.parent = mapParent.transform;
            if (buildingBlockMaterial != null)
            {
                segment.GetComponent<Renderer>().material = buildingBlockMaterial;
            }
            if (currentSegment.isKey)
            {
                segment.GetComponent<Renderer>().material = keyMaterial;
            }
            //adding the children to the queue
            if(currentSegment.GetChildren().Count==0){continue;}
            foreach (Tree<VascularSegment> c in currentSegment.GetChildren()){toVisit.Add(c);}
            
        }
        
    }



    private List<Tree<VascularSegment>> GetKeyNodes(List<string> respawnCellTypes, int radiusContraint)
    {
        int maxSignificance = segmentTreeRoot.GetNumberOfNodes();
        Debug.Log(maxSignificance);
        int currentSignificance = maxSignificance;
        bool segmentsFound = false;
        while (!segmentsFound && currentSignificance>0) 
        {
            //we get the significance list with the current significance value
            List<Tree<VascularSegment>> currentSignificanceNodes = segmentTreeRoot.GetKeyNodes(currentSignificance);
            //if we don't have enouch key nodes at the current significance level, we decrease teh level and try again
            if (currentSignificanceNodes.Count < respawnCellTypes.Count + 1) //have to have nubmer of respawn cell types +1 becuase we need a brain node too
            {
                currentSignificance--;
                continue;    
            }

            // now we make sure that that the current significance nodes don't overlap within the raidus
            List<Tree<VascularSegment>> viableNodes = new();
            bool[,] significanceMap = new bool[ perfusionRadius*2, perfusionRadius*2];
            System.Random random = new System.Random();

            while (viableNodes.Count < respawnCellTypes.Count + 1)
            {
                if (currentSignificanceNodes.Count == 0){ break; }//if we've checked all the nodes, break out of the loop

                //getting a random viable node from the list
                int randIndex = random.Next(currentSignificanceNodes.Count);
                Tree<VascularSegment> currentNode = currentSignificanceNodes[randIndex];
                currentSignificanceNodes.RemoveAt(randIndex);

                //gettign the midpoitn and actign based on its location
                double[] midpoint = currentNode.GetValue().GetMidpoint();
                midpoint[0] = midpoint[0]+perfusionRadius; //convert x and y to interface with the rectangular array coordinates
                midpoint[1] = perfusionRadius-midpoint[1]; //convert x and y to interface with the rectangular array coordinates
                if (significanceMap[(int)midpoint[1], (int)midpoint[0]]) //if the point is within the radius of an existing endpoint
                {
                    continue;
                }
                else //if it's not within the radius impact of an existing chosen point, we take the point and put it's radius impact in the significanceMap
                {
                    viableNodes.Add(currentNode);//accepting hte current node
                    //adding it's radius of exculsion to the significanceMap
                    for (int x = (int)midpoint[0] - radiusContraint; x < (int)midpoint[0] + radiusContraint; x++)
                    {
                        for (int y = (int)midpoint[1] - radiusContraint; y < (int)midpoint[1] + radiusContraint; y++)
                        {
                            //checking point validity
                            if (significanceMap.GetLength(0) <= x || significanceMap.GetLength(1) <= y || x < 0 || y < 0)
                            {
                                continue;
                            }

                            if (Math.Pow(x - midpoint[0], 2) + Math.Pow(y - midpoint[1], 2) <= Math.Pow(radiusContraint,2)) //if the distance of the midpoint to the point we're checking is less than the raidus constraint, it's influenced
                            {
                                significanceMap[y, x] = true;
                            }
                            
                        }

                    }
                }

            }
            if (viableNodes.Count >= respawnCellTypes.Count +1){return viableNodes;} //if it's of the right length we return it
            else{ currentSignificance--;} //otherwise we decrease the significance level and try again
            
        }
        return null;//if no list has been returned after the loop runs, it's not possible at the current radius constraint, so we return null

    }
}
