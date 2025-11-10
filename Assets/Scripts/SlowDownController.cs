using UnityEngine;

/*
 * Model Virus Controller class
 *  -When virus approaches wall, begins slow animation, raising its linear damping for a brief period
 */
public class SlowDownController : MonoBehaviour
{
    [SerializeField] private GameObject virus;
    MovementController moveControl;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        moveControl = virus.GetComponent<MovementController>();
    }

    // OnTriggerEnter is called when this object's Collider overlaps with another Collider,
    // at least one of them being set to trigger
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("wall") && !moveControl.isNearWall) { moveControl.SetWallBool(true); }
        moveControl.SetWallCount(moveControl.GetWallCount() + 1);
        moveControl.linDampAnimator.SetTrigger("slowVirus");
    }

    // OnTriggerEnter is called when this object's Collider fully exits another Collider,
    // at least one of them being set to trigger
    void OnTriggerExit(Collider other)
    {
        moveControl.SetWallCount(moveControl.GetWallCount() - 1); ;
        if (other.CompareTag("wall") && moveControl.GetWallCount() == 0) { moveControl.SetWallBool(false); }
    }


}
