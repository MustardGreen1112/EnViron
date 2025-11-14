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
    [SerializeField] GameObject virus;

    /* External information (passed at start) */
    [SerializeField] Intersection[] intersections; // replace with database of intersections here
    [SerializeField] Edge[] edges; // replace with database of curves here
    // include setters for both of these somewhere in the code below

    /* Internal variables */
    private string currentMode;
    private int antibodiesAttachedToVirus;
    private float passedTime;

    private MovementController virusController;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        virusController = virus.GetComponent<MovementController>();

        currentMode = "Roaming";
        antibodiesAttachedToVirus = 0;
        passedTime = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (currentMode == "Roaming" || currentMode == "AttackRoaming")
        {
            
        }
    }
}
