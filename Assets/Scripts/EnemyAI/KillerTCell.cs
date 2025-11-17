using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] float detectionRadius = 15f;
    [SerializeField] float chaseSpeed = 1f; // speeds in units per seocnd
    [SerializeField] float returnSpeed = 2.5f;
    [SerializeField] float maxTimeNoLOS = 3f; // in seconds
    [SerializeField] float roamTravelTimeScale = 0.2f; // for every 1 unit between the two points of a segment, the travel time increases by this amount
    [SerializeField] int randomModifier = 0; // added to random seed; allows individual predictable randomness
    [SerializeField] GameObject virus;
    [SerializeField] GameObject virusVisibleBox;

    // Map info
    public MapGenerator mapgen;

    /* Internal variables */
    private string currentMode;
    private float passedTime;
    private System.Random rand;
    private float chaseMovementPerFrame;

    // Navigation variables
    private Tree<VascularSegment> tree;
    private bool isForward;
    private Tree<VascularSegment> currentNode;
    private VascularSegment currentSegment;
    private bool isTraveling;
    private Vector3 startPoint;
    private Vector3 endPoint;
    private float adjustedTravelTime;

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

        transform.position = endPoint;
        

        virusController = virus.GetComponent<MovementController>();

        currentMode = "Roaming";
        passedTime = 0;
        adjustedTravelTime = roamTravelTimeScale * Vector3.Distance(startPoint, endPoint);

        isForward = true;
        isTraveling = false;

        chaseMovementPerFrame = chaseSpeed*Time.deltaTime;

        if (ProjectWideConsts.randomSeed == -1)
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
        passedTime += Time.deltaTime;

        // Virus vision check
        if (currentMode != "Return")
        {
            RaycastHit hitInfo;
            LayerMask layerMask = LayerMask.GetMask("Default", "virusVision");
            if (Physics.Linecast(transform.position, virusVisibleBox.transform.position, out hitInfo, layerMask))
            {
                if (hitInfo.collider.transform.CompareTag("Player")) 
                { 
                    currentMode = "Attack";
                    passedTime = 0f; 
                }
            }
        }

        // Reset to Roaming if virus hasn't been seen in awhile
        if (currentMode == "Attack" && passedTime > maxTimeNoLOS) 
        { 
            currentMode = "Roaming";
            transform.position = endPoint;
            isForward = true;
        }

        if (currentMode == "Roaming" || currentMode == "AttackRoaming")
        {
            // If virus is within detection radius (increases with number of antibodies),
            // move to AttackRoaming mode
            float adjustedRadius = (1 + virusController.GetAntibodiesAttached()) * detectionRadius;
            bool isClose = Vector3.Distance(transform.position, virus.transform.position) < (1 + adjustedRadius);

            if (isClose) { currentMode = "AttackRoaming"; }
            else { currentMode = "Roaming"; }

            // If not on an edge already, pick an edge and begin traveling it
            if (!isTraveling)
            {
                // If regular roaming, pick randomly, with bias away from an edge that we just came from
                if (currentMode == "Roaming") { (isForward, currentNode) = PickRandomConnectedEdge(); }

                // If attack roaming, pick edge that brings us closest to player
                if (currentMode == "AttackRoaming") { (isForward, currentNode) = PickBestConnectedEdge(); }

                currentSegment = currentNode.GetValue();
                startPoint = ConvertToVector(currentSegment.startPoint);
                endPoint = ConvertToVector(currentSegment.endPoint);

                adjustedTravelTime = roamTravelTimeScale * Vector3.Distance(startPoint, endPoint);
                
                isTraveling = true;
                StartCoroutine(TravelEdge(isForward));
            }
        }

        if (currentMode == "Attack")
        {
            transform.position = Vector3.MoveTowards(transform.position, virusVisibleBox.transform.position, chaseMovementPerFrame);
        }
    }

    /* Helper functions */

    /*
     * Picks connected edge semi-randomly (weighted against picking current edge)
     */
    (bool, Tree<VascularSegment>) PickRandomConnectedEdge()
    {
        List<Tree<VascularSegment>> neighbors = new();

        // Used to determine whether chosen edge goes forward or backward from current intersection
        bool nextIsForward = true;

        if (isForward) // if at the end of a segment
        {
            neighbors.Add(currentNode);
            foreach (Tree<VascularSegment> neighbor in currentNode.GetChildren())
            {
                neighbors.Add(neighbor);
                neighbors.Add(neighbor);
            }
        }
        else // if at the start of a segment
        {
            if (currentNode.parent == null) { neighbors.Add(currentNode); }
            else 
            {
            neighbors.Add(currentNode.parent);
            neighbors.Add(currentNode.parent);
            foreach (Tree<VascularSegment> neighbor in currentNode.GetParent().GetChildren())
            {
                neighbors.Add(neighbor);
                if (neighbor != currentNode) { neighbors.Add(neighbor); }
            }
            }
        }

        int chosenIndex = rand.Next(neighbors.Count);
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
            bestDistance = Vector3.Distance(ConvertToVector(bestNode.GetValue().startPoint), virusVisibleBox.transform.position);

            foreach (Tree<VascularSegment> neighbor in currentNode.GetChildren())
            {
                float candidateDistance = Vector3.Distance(ConvertToVector(neighbor.GetValue().endPoint), virusVisibleBox.transform.position);
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
            // Special case if no parent (root)
            if (currentNode.GetParent() == null)
            {
                bestNode = currentNode;
                bestDistance = Vector3.Distance(ConvertToVector(bestNode.GetValue().endPoint), virusVisibleBox.transform.position);
                nextIsForward = true;
            }
            
            else 
            {
            // Special case for the first neighbor checked (parent)
            bestNode = currentNode.GetParent();
            bestDistance = Vector3.Distance(ConvertToVector(bestNode.GetValue().startPoint), virusVisibleBox.transform.position);

            foreach (Tree<VascularSegment> neighbor in currentNode.GetParent().GetChildren())
            {
                float candidateDistance = Vector3.Distance(ConvertToVector(neighbor.GetValue().endPoint), virusVisibleBox.transform.position);
                if (candidateDistance < bestDistance)
                {
                    bestNode = neighbor;
                    bestDistance = candidateDistance;
                    nextIsForward = true;
                }
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

        while (timer < adjustedTravelTime)
        {
            if (currentMode != "Roaming" && currentMode != "AttackRoaming") { break; }
            timer += Time.deltaTime;

            float t = timer / adjustedTravelTime;
            Vector3 newLocation; 
            if (isForward) { newLocation = Vector3.Lerp(startPoint, endPoint, t); }
            else { newLocation = Vector3.Lerp(startPoint, endPoint, 1 - t); }
            transform.localPosition = newLocation;

            yield return null;
        }

        isTraveling = false;
    }
}
