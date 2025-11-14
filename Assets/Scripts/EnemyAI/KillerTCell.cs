using System.Collections.Generic;
using System.Linq;
using Oculus.Platform.Models;
using UnityEngine;

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
                int numOfConnectedEdges = intersections[destinationIntersection].inEdges.Length + 
                intersections[destinationIntersection].outEdges.Length;
                List<int> IDs = new();

                // If regular roaming, pick randomly, with bias away from an edge that we just came from
                if (currentMode == "Roaming")
                {
                    foreach (Edge edge in intersections[destinationIntersection].inEdges) {
                        IDs.Append<int>(edge.ID);
                        if (edge.ID != currentEdge) { IDs.Append<int>(edge.ID); }
                    }

                    foreach (Edge edge in intersections[destinationIntersection].outEdges) {
                        IDs.Append<int>(edge.ID);
                        if (edge.ID != currentEdge) { IDs.Append<int>(edge.ID); }
                    }
                }

                currentEdge = IDs[rand.Next(numOfConnectedEdges)];
            }
        }
    }
}
