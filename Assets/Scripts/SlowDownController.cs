using UnityEngine;

public class SlowDownController : MonoBehaviour
{
    [SerializeField] MovementController moveControl;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("wall") && !moveControl.isNearWall) { moveControl.isNearWall = true; }
        moveControl.wallsEntered += 1;
        moveControl.linDampAnimator.SetTrigger("slowVirus");
    }

    void OnTriggerExit(Collider other)
    {
        moveControl.wallsEntered -= 1;
        if (other.CompareTag("wall") && moveControl.wallsEntered == 0) { moveControl.isNearWall = false; }
    }
}
