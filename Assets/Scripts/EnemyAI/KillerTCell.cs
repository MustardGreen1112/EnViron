using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meta.XR.MRUtilityKit.SceneDecorator;
using UnityEngine;
using VascularGenerator.DataStructures;

/*
 * T Cell class
 *  -Governs four modes:
 *    -Roaming: Aimlessly (with preference towards not backtracking) moves from intersection to intersection of map
 *      -Default mode

 *    -AttackRoaming: Moves along map lines, this time towards player
 *      -Triggered by being within a certain radius of player

 *    -Attack: Moves toward players
 *      -Triggered by having LOS with the player
 *      -If the virus loses LOS with the player for a certain amount of time, enters "return" mode

 *    -Return: Goes back to nearest blood vessel intersection
 *      -Cannot enter "attack roaming" or "attack" mode from this mode
 *      -Once returned to an intersection, enters "roaming" mode
 */
public class KillerTCell : MonoBehaviour
{
    /* Params (editable from inspector) */
    [SerializeField] float chaseSpeed = 5f;
    [SerializeField] float returnSpeed = 2.5f;
    [SerializeField] float maxTimeNoLOS = 2f; // in seconds
    [SerializeField] float roamTravelTime = 4f;
    [SerializeField] int startingIntersection = 0;
    [SerializeField] int randomModifier = 0; // added to random seed; allows individual predictable randomness
    [SerializeField] GameObject virus;

    /* External information (passed at start) */
    [SerializeField] Intersection[] intersections; // replace with database of intersections here
    [SerializeField] Edge[] edges; // replace with database of curves here
    // include setters for both of these somewhere in the code below
    public MapGenerator mapgen;
    private Tree<VascularSegment> tree;

    /* Internal variables */
    private string currentMode;
    private int antibodiesAttachedToVirus;
    private float passedTime;
    private System.Random rand;

    // Navigation variables
    private int destinationIntersection;
    private int currentEdge;
    private bool onEdge;

    private MovementController virusController;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tree = mapgen.segmentTreeRoot;
        Tree<VascularSegment> randNode = tree.GetRandomNode();
        VascularSegment randSegment = randNode.GetValue();
        double[] startPoint = randSegment.startPoint;
        double[] endPoint = randSegment.endPoint;
        double radius = randSegment.radius;
        

        virusController = virus.GetComponent<MovementController>();

        currentMode = "Roaming";
        antibodiesAttachedToVirus = 0;
        passedTime = 0;

        destinationIntersection = startingIntersection;
        currentEdge = -1;
        onEdge = false;

        if (ProjectWideConsts.randomSeed == 0)
        {
            rand = new System.Random();
        } 
        else
        {
            rand = new System.Random(ProjectWideConsts.randomSeed + randomModifier);
        }
    }

    // Update is called once per frame
    void Update()
    {


        if (currentMode == "Roaming" || currentMode == "AttackRoaming")
        {
            // If not on an edge already, pick an edge and begin traveling it
            if (!onEdge)
            {
                bool isForward = false;

                int numOfConnectedEdges = intersections[destinationIntersection].inEdges.Length + 
                intersections[destinationIntersection].outEdges.Length;

                // If regular roaming, pick randomly, with bias away from an edge that we just came from
                if (currentMode == "Roaming") { (isForward, currentEdge) = PickRandomConnectedEdge(numOfConnectedEdges); }

                // If attack roaming, pick edge that brings us closest to player
                if (currentMode == "AttackRoaming") { (isForward, currentEdge) = PickBestConnectedEdge(); }
                
                onEdge = true;
                StartCoroutine(TravelEdge(isForward));
            }
        }
    }

    /* Helper functions */

    /*
     * Picks connected edge semi-randomly (weighted against picking current edge)
     */
    (bool, int) PickRandomConnectedEdge(int numOfConnectedEdges)
    {
        List<int> IDs = new();

        // Used to determine whether chosen edge goes forward or backward from current intersection
        bool isForward = true;
        int backEdgesAmt = 0;

        foreach (Edge edge in intersections[destinationIntersection].inEdges) 
        {
            IDs.Append<int>(edge.ID);
            backEdgesAmt += 1;
            if (edge.ID != currentEdge) { 
                IDs.Append<int>(edge.ID);
                backEdgesAmt += 1;
            }
        }

        foreach (Edge edge in intersections[destinationIntersection].outEdges) 
        {
            IDs.Append<int>(edge.ID);
            if (edge.ID != currentEdge) { IDs.Append<int>(edge.ID); }
        }

        // Used to determine whether chosen edge goes forward or backward from current intersection
        int chosenIndex = rand.Next(IDs.Count);
        if (chosenIndex < backEdgesAmt) { isForward = false; } 

        return (isForward, IDs[chosenIndex]);
    }

    /*
     * Picks connected edge, minimizing distance from other end to player
     */
    (bool, int) PickBestConnectedEdge()
    {
        int bestEdge = -1;
        float bestDistance = -1f;

        bool isForward = false;

        foreach (Edge edge in intersections[destinationIntersection].inEdges)
        {
            // Special case for first edge checked
            if (bestEdge == -1) 
            { 
                bestEdge = edge.ID; 
                bestDistance = Vector3.Distance(edge.edge[0], virus.transform.position);
            }
            else
            {
                float candidateDistance = Vector3.Distance(edge.edge[0], virus.transform.position);
                if (candidateDistance < bestDistance)
                {
                    bestEdge = edge.ID;
                    bestDistance = candidateDistance;
                }
            }
        }

        foreach (Edge edge in intersections[destinationIntersection].outEdges)
        {
           float candidateDistance = Vector3.Distance(edge.edge[1], virus.transform.position);
            if (candidateDistance < bestDistance)
            {
                bestEdge = edge.ID;
                bestDistance = candidateDistance;
                isForward = true;
            } 
        }

        return (isForward, bestEdge);
    }

    /*
     * Coroutine that causes white blood cell to drift across blood vessel segment
     * Stops when either motion is complete or if the mode changes off of a "roaming" state
     * Can go forward or backward across a segment depending on forward parameter
     */
    private IEnumerator TravelEdge(bool isForward)
    {
        float timer = 0f;

        while (timer < roamTravelTime)
        {
            if (currentMode != "Roaming" && currentMode != "AttackRoaming") { break; }
            timer += Time.deltaTime;

            float t = timer / roamTravelTime;
            Vector3 newLocation; 
            if (isForward) { newLocation = Vector3.Lerp(edges[currentEdge].edge[0], edges[currentEdge].edge[1], t); }
            else { newLocation = Vector3.Lerp(edges[currentEdge].edge[0], edges[currentEdge].edge[1], 1 - t); }
            transform.localPosition = newLocation;

            yield return null;
        }

        onEdge = false;
    }
}
