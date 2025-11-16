using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meta.XR.MRUtilityKit.SceneDecorator;
using Unity.Collections;
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
    private bool isForward;
    Tree<VascularSegment> currentNode;
    VascularSegment currentSegment;
    private bool isTraveling;
    Vector3 startPoint;
    Vector3 endPoint;

    private MovementController virusController;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initialize location info
        tree = mapgen.segmentTreeRoot;
        currentNode = tree.GetRandomNode();
        currentSegment = currentNode.GetValue();
        startPoint = ConvertToVector(currentSegment.startPoint);
        endPoint = ConvertToVector(currentSegment.endPoint);

        transform.SetPositionAndRotation(endPoint, transform.rotation);
        

        virusController = virus.GetComponent<MovementController>();

        currentMode = "Roaming";
        antibodiesAttachedToVirus = 0;
        passedTime = 0;

        isForward = true;
        isTraveling = false;

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
            if (!isTraveling)
            {
                int numOfConnectedEdges = 0;

                // Add 1 to the count to account for going up the parent
                if (isForward) { numOfConnectedEdges = currentNode.GetChildren().Count + 1; }
                else 
                {
                    // Handle root edge case 
                    if (currentNode.GetParent() == null) 
                    { 
                        numOfConnectedEdges = currentNode.GetParent().GetChildren().Count; 
                    }
                    else { numOfConnectedEdges = currentNode.GetParent().GetChildren().Count + 1; }
                }

                // If regular roaming, pick randomly, with bias away from an edge that we just came from
                if (currentMode == "Roaming") { (isForward, currentNode) = PickRandomConnectedEdge(numOfConnectedEdges); }

                // If attack roaming, pick edge that brings us closest to player
                if (currentMode == "AttackRoaming") { (isForward, currentNode) = PickBestConnectedEdge(); }

                currentSegment = currentNode.GetValue();
                startPoint = ConvertToVector(currentSegment.startPoint);
                endPoint = ConvertToVector(currentSegment.endPoint);
                
                isTraveling = true;
                StartCoroutine(TravelEdge(isForward));
            }
        }
    }

    /* Helper functions */

    /*
     * Picks connected edge semi-randomly (weighted against picking current edge)
     */
    (bool, Tree<VascularSegment>) PickRandomConnectedEdge(int numOfConnectedEdges)
    {
        List<Tree<VascularSegment>> neighbors = new();

        // Used to determine whether chosen edge goes forward or backward from current intersection
        bool nextIsForward = true;

        if (isForward) // if at the end of a segment
        {
            neighbors.Append(currentNode);
            foreach (Tree<VascularSegment> neighbor in currentNode.GetChildren())
            {
                neighbors.Append(neighbor);
                neighbors.Append(neighbor);
            }
        }
        else // if at the start of a segment
        {
            if (currentNode.parent == null) {}
            else 
            {
            neighbors.Append(currentNode.parent);
            neighbors.Append(currentNode.parent);
            foreach (Tree<VascularSegment> neighbor in currentNode.GetParent().GetChildren())
            {
                neighbors.Append(neighbor);
                if (neighbor == currentNode) { neighbors.Append(neighbor); }
            }
            }
        }

        int chosenIndex = rand.Next(numOfConnectedEdges);
        Tree<VascularSegment> nextNode = neighbors[chosenIndex];

        // Used to determine whether chosen edge goes forward or backward from current intersection
        if (isForward)
        {
            if ( nextNode == currentNode) { nextIsForward = false; }
        }
        else
        {
            if ( nextNode == currentNode.GetParent() ) { nextIsForward = false; }
        }

        return (nextIsForward, nextNode);
    }

    /*
     * Picks connected edge, minimizing distance from other end to player
     */
    (bool, Tree<VascularSegment>) PickBestConnectedEdge()
    {
        float bestDistance = -1f;
        Tree<VascularSegment> bestNode = null;

        bool nextIsForward = false;

        if (isForward) // if at the end of a segment
        {
            // Special case for first neighbor checked (self)
            bestNode = currentNode;
            bestDistance = Vector3.Distance(ConvertToVector(bestNode.GetValue().startPoint), virus.transform.position);

            foreach (Tree<VascularSegment> neighbor in currentNode.GetChildren())
            {
                float candidateDistance = Vector3.Distance(ConvertToVector(neighbor.GetValue().endPoint), virus.transform.position);
                if (candidateDistance < bestDistance)
                {
                    bestNode = neighbor;
                    bestDistance = candidateDistance;
                    nextIsForward = true;
                }
            }
        }
        else // if at the start of a segment
        {
            // Special case for the first neighbor checked (parent)
            bestNode = currentNode.GetParent();
            bestDistance = Vector3.Distance(ConvertToVector(bestNode.GetValue().startPoint), virus.transform.position);

            foreach (Tree<VascularSegment> neighbor in currentNode.GetParent().GetChildren())
            {
                float candidateDistance = Vector3.Distance(ConvertToVector(neighbor.GetValue().endPoint), virus.transform.position);
                if (candidateDistance < bestDistance)
                {
                    bestNode = neighbor;
                    bestDistance = candidateDistance;
                    nextIsForward = true;
                }
            }
        }

        return (nextIsForward, bestNode);
    }

    /*
     * Converts Chase's segment data of floats into a Vector3
     */
    Vector3 ConvertToVector(double[] doubles)
    {
        float x = (float)doubles[0];
        float z = (float)doubles[1];
        return new Vector3(x, 0f, z);
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
            if (isForward) { newLocation = Vector3.Lerp(startPoint, endPoint, t); }
            else { newLocation = Vector3.Lerp(startPoint, endPoint, 1 - t); }
            transform.localPosition = newLocation;

            yield return null;
        }

        isTraveling = false;
    }
}
