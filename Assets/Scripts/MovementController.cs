using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

/*
 * (Virus) Movement Controller class
 *  -If the model virus is currently held, accelerates the virus forward every frame
 *      -Acceleration scales with virus displacement
 *  -When player lets go of model virus, increases virus linear damping, slowing it to a stop
 */
public class MovementController : MonoBehaviour
{
    /* Params (editable from inspector) */
    [SerializeField] private GameObject modelVirus;
    [SerializeField] private float movementScalar = 20f;
    [SerializeField] private float releaseLinDamping = 0.7f;  

    private Rigidbody rigid;          

    // Slow down animation params
    public bool isNearWall;          
    public int wallsEntered;         // variable used to ensure isNearWall is only false when not near any wall object
    public bool slowedDown;

    private ModelVirusController modelVirusController;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        modelVirusController = modelVirus.GetComponent<ModelVirusController>();
        rigid = GetComponent<Rigidbody>();

        isNearWall = false;
        slowedDown = false;
        wallsEntered = 0;
    }

    // Update is called once every frame
    void Update()
    {
        // If not being slowed, adjusts linear damping according to whether model virus is held
        if (!slowedDown)
        {
            if (modelVirusController.GetIsHeld()) { rigid.linearDamping = 0; }
            else { rigid.linearDamping = releaseLinDamping; }
        }
    }

    // FixedUpdate is called every fixed frame; used for physics updates primarily
    void FixedUpdate()
    {
        // Adds force to virus if model virus is held and virus is not slowed
        if (modelVirusController.GetIsHeld() && !slowedDown)
        {
            Vector3 displacement = modelVirusController.GetDisplacement();
            rigid.AddForce(displacement * movementScalar, ForceMode.Acceleration);
        }
    }

    /* Setters/getters */
    public void SetWallBool(bool wallBool)
    {
        isNearWall = wallBool;
    }

    public void SetWallCount(int wallCount)
    {
        wallsEntered = wallCount;
    }

    public void SetLinDamp(float linDamp)
    {
        rigid.linearDamping = linDamp;
    }

    public void SetSlowedDown(bool slowed)
    {
        slowedDown = slowed;
    }

    public bool GetWallBool()
    {
        return isNearWall;
    }

    public int GetWallCount()
    {
        return wallsEntered;
    }
}
