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
    public bool markKeySegments;
    public Material keyMaterial;
    public GameObject buildingBlock;
    public Material buildingBlockMaterial;
    [System.NonSerialized] public GameObject mapParent; //this is the minimap object!!!!
    public RespawnManager respawnManager;
    public GameObject spawnerObject;
    [System.NonSerialized] public List<GameObject> spawnedObjects = new(); //the spawner game object instances
    public GameObject infectedPointMapMarker;
    [System.NonSerialized] public List<VirusGenController> spawnedObjectsControllers = new(); //the scripts for the spawnerGameObject instances
    [System.NonSerialized] public Tree<VascularSegment> brainSegment;
    public GameObject bloodBrainBarrierDivider;

    public float minimapScale = 0.005f;
    public float radiusScalar = 20; //scalar for radius

    public Tree<VascularSegment> segmentTreeRoot; //THIS IS THE MAP TREE STRUCTURE!!!!!!

    public bool useVolumeOptimization;
    public bool LoadFromDummyGraph;
    public string loadJSON_name;
    public bool storeGraphGenerated;
    public string OVERWRITE_JSONStorageName;
    public bool loadFullGraph;
    public string graphWithCurveIDName;
    public string curveIDDictionaryName;

    public Dictionary<int, CatmullRomSpline> curveDict; //THIS IS THE CURVE ID TO CURVE OBJECT DICTIONARY
    public Dictionary<int, GameObject> idToMiniMapDict = new(); //given a virusGenController Id, it returns the game object in the minimap that is associated with that spawn point
 

    int perfusionRadius = 400;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        Debug.Log("---Starting Program---");

        // Constants for realistic microvascular scale
        // double perfusionRadius = 100;                // in pixels (1 px = 1 cm → 1 m radius domain)
        int numberTerminalSegments = 90;
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
        keyPointMarker.SetActive(false);

        mapParent = new GameObject("mapParent");

        if (loadFullGraph)
        {
            curveDict = CurveInterfacor.LoadCurveDictionary("Assets/Resources/"+curveIDDictionaryName+".json");
            segmentTreeRoot = generator.LoadJSON("Assets/Resources/"+graphWithCurveIDName+".json");
        }
        else
        {
            if (LoadFromDummyGraph)
            {
                //loading from JSON
                segmentTreeRoot = generator.LoadJSON("Assets/Scripts/VascularGeneration/jsons/"+loadJSON_name+".txt");
            }
            else
            {
                segmentTreeRoot = generator.inletSegment;
            }
            

            //now we get a list of the nodes that will be brain + respawn nodes
            int keyNodeExclusionRadius = (int)(perfusionRadius/(2*(respawnCellTypes.Count+1)));
            List<Tree<VascularSegment>> keyNodes = GetKeyNodes(respawnCellTypes, keyNodeExclusionRadius);
            
            if (keyNodes != null)
            {
                //marking each key node with a tag 
                List<string> tags = new() { "brain" };
                foreach (string s in respawnCellTypes){tags.Add(s);}

                foreach(Tree<VascularSegment> node in keyNodes)
                {
                    string tag = tags[0];
                    tags.RemoveAt(0);
                    node.isKey = true;
                    node.tag = tag;

                    Vector3 startPoint = new Vector3((float)node.GetValue().startPoint[0], 0, (float)node.GetValue().startPoint[1]);
                    Vector3 endPoint = new Vector3((float)node.GetValue().endPoint[0], 0, (float)node.GetValue().endPoint[1]);
                    GameObject marker = Instantiate(keyPointMarker, startPoint+(endPoint-startPoint)/2, Quaternion.identity);
                    marker.transform.localScale = new Vector3(5,5,5);
                }

                //marking any nodes within the influenceRadius (defined by keyNodeExclusionRadius) of each key node with said node's tag
                double influenceRadius = keyNodeExclusionRadius;
                List<Tree<VascularSegment>> toVisit = new(){segmentTreeRoot};
                while(toVisit.Count > 0)
                {
                    Tree<VascularSegment> current = toVisit[0];
                    toVisit.RemoveAt(0);
                    //checking to see if this node is within  the influence raidus
                    foreach (Tree<VascularSegment> keyNode in keyNodes)
                    {
                        // double[] currentMidpoint = current.GetValue().GetMidpoint();
                        double[] currentStartPoint = current.GetValue().startPoint;
                        double[] currentEndPoint = current.GetValue().endPoint;
                        
                        double[] keyMidpoint = keyNode.GetValue().GetMidpoint();
                        
                        double startPointDistanceSquared = Math.Pow(currentStartPoint[0]-keyMidpoint[0], 2) + Math.Pow(currentStartPoint[1]-keyMidpoint[1], 2);
                        double endPointDistanceSquared = Math.Pow(currentEndPoint[0]-keyMidpoint[0], 2) + Math.Pow(currentEndPoint[1]-keyMidpoint[1], 2);

                        if ( startPointDistanceSquared < Math.Pow(influenceRadius, 2) || endPointDistanceSquared < Math.Pow(influenceRadius, 2) )
                        {
                            current.tag = keyNode.tag;
                        }
                    }
                    //adding children to the queue
                    if (current.GetChildren().Count ==0){continue;}
                    foreach (Tree<VascularSegment> c in current.GetChildren()){toVisit.Add(c);}
                }
            }


            //storing the generated graph if so directed
            if (storeGraphGenerated && !LoadFromDummyGraph)
            {
                generator.StoreJSON(segmentTreeRoot, "Assets/Scripts/VascularGeneration/jsons/"+OVERWRITE_JSONStorageName+".txt");
            }        
        }

       

       
        //creating the respawn points
        List<Tree<VascularSegment>> spawnPointNodes = new();

        List<Tree<VascularSegment>> queue = new(){segmentTreeRoot};
        while (queue.Count > 0)
        {
            Tree<VascularSegment> current = queue[0];
            queue.RemoveAt(0);

            if (current.isKey)
            {
                if (current.tag == "brain")
                {
                    brainSegment = current;
                }
                else
                {
                    spawnPointNodes.Add(current);
                }
            }

            if (current.GetChildren().Count == 0){continue;}
            foreach (Tree<VascularSegment> c in current.GetChildren()){queue.Add(c);}
        }

        if(bloodBrainBarrierDivider != null)
        {
            //now we have a brain Node locaiton (brainSegment) and a list of key nodes (spawnPointNodes)
            CatmullRomSpline brainSegmentSpline = curveDict[brainSegment.GetValue().curveID];
            Vector3 brainBarrierInstancePoint = brainSegmentSpline.Eval(0.5f);
            GameObject brainBarrier = Instantiate(bloodBrainBarrierDivider, brainBarrierInstancePoint, quaternion.identity);
            brainBarrier.transform.localScale = brainBarrier.transform.localScale * (float)brainSegment.GetValue().radius * radiusScalar; // new Vector3((float)brainSegment.GetValue().radius, 1f, (float)brainSegment.GetValue().radius);
            brainBarrier.transform.LookAt(brainSegmentSpline.Eval(1f) - brainBarrierInstancePoint);
            brainBarrier.transform.Rotate(new Vector3(90f, 0f, 0f));
        }


        //now we will create spawner objects and keep track of them in a list, all from the list of spawnerNodes
        int id = 0;
        foreach (Tree<VascularSegment> spawnNode in spawnPointNodes)
        {
            //creating the actual spawner objects and keeping track of the scripts attached
            CatmullRomSpline spawnerSpline = curveDict[spawnNode.GetValue().curveID];
            Vector3 spawnerInstancePoint = spawnerSpline.Eval(0.5f);
            GameObject spawnPoint = Instantiate(spawnerObject, spawnerInstancePoint, Quaternion.identity);
            spawnedObjects.Add(spawnPoint);
            VirusGenController virusGen = spawnPoint.GetComponent<VirusGenController>();
            spawnedObjectsControllers.Add(virusGen);
            virusGen.id = id;
            id++;

            //creating the minimap objects
            Vector3 startPoint = new Vector3((float)spawnNode.GetValue().startPoint[0], 0, (float)spawnNode.GetValue().startPoint[1]);
            Vector3 endPoint = new Vector3((float)spawnNode.GetValue().endPoint[0], 0, (float)spawnNode.GetValue().endPoint[1]);
            GameObject infectedMarker = Instantiate(infectedPointMapMarker, startPoint+(endPoint-startPoint)/2, Quaternion.identity);
            float scalar = 4*(float)spawnNode.GetValue().radius*radiusScalar; //controls the size of the minimap virus
            infectedMarker.transform.localScale = new Vector3(scalar, scalar, scalar);
            idToMiniMapDict[virusGen.id] = infectedMarker; //saving the infected minimap marker object in the id to object dictionary
            infectedMarker.transform.SetParent(mapParent.transform);
            
        }

        if (respawnManager!= null)
        {
            respawnManager.respawnPointControls = spawnedObjectsControllers;
    
        }else{Debug.Log("RESPAWN MANAGER NOT SET");}
        


        //creating the map mesh
        CreateMesh(segmentTreeRoot);
        mapParent.transform.localScale = new Vector3(minimapScale, minimapScale, minimapScale); 
        

        Debug.Log("---Program Complete---");
        buildingBlock.SetActive(false);
        keyPointMarker.SetActive(false);
        bloodBrainBarrierDivider.SetActive(false);
        spawnerObject.SetActive(false);

    }

    // Update is called once per frame
    void Update()
    {
        //on update we loop through the respawn points, if it's active, we turn it on in the minimap, if its inactive, we turn it off in the minimap
        foreach (VirusGenController virusGen in spawnedObjectsControllers)
        {
            GameObject mapMarker = idToMiniMapDict[virusGen.id];
            if (virusGen.isRespawnPoint) //if the respawn point is active
            {   
               
                if (!mapMarker.activeSelf) //if the minimapmarker isn't active already
                {
                    Debug.Log("we Have a respawn point");
                    virusGen.TurnOnVirusGen(); //if it's off we turn it on here
                    mapMarker.SetActive(true);
                }
            }
            else //if the respawn point is inactive
            {
               if (mapMarker.activeSelf) //if the minimapmarker is active already
                {
                    virusGen.TurnOffVirusGen(); //if it is on we turn it off here
                    mapMarker.SetActive(false);
                } 
            }
        }
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
            segment.transform.localScale = new Vector3((float)currentSegment.GetValue().radius*radiusScalar, distance/2, (float)currentSegment.GetValue().radius*radiusScalar);
            segment.transform.rotation = Quaternion.FromToRotation(Vector3.up, (endPoint-startPoint).normalized);
            segment.transform.parent = mapParent.transform;
            if (buildingBlockMaterial != null)
            {
                segment.GetComponent<Renderer>().material = buildingBlockMaterial;
            }
            

            if (currentSegment.tag!="" && markKeySegments)
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
