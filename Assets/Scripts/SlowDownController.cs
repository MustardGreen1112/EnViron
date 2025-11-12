using System.Collections;
using UnityEngine;
using Unity.VisualScripting;

/*
 * Model Virus Controller class
 *  -When virus approaches wall, begins slow animation, raising its linear damping for a brief period
 */
public class SlowDownController : MonoBehaviour
{
    [SerializeField] private GameObject virus;

    // Wall slowdown animation params
    [SerializeField] private float nearWallLinDamping = 0.99f;
    [SerializeField] private float slowdownDuration = 3f;

    private MovementController moveControl;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        moveControl = virus.GetComponent<MovementController>();
    }

    // OnTriggerEnter is called when this object's Collider overlaps with another Collider,
    // at least one of them being set to trigger
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("wall"))
        {
            if (!moveControl.GetWallBool())
            {
                StopAllCoroutines();
                moveControl.SetWallBool(true);
                moveControl.SetSlowedDown(true);
                StartCoroutine(Slowdown());
            }
            moveControl.SetWallCount(moveControl.GetWallCount() + 1);
        }
    }

    // OnTriggerEnter is called when this object's Collider fully exits another Collider,
    // at least one of them being set to trigger
    void OnTriggerExit(Collider other)
    {
        if (moveControl.GetWallCount() != 0) { moveControl.SetWallCount(moveControl.GetWallCount() - 1); }
        if (other.CompareTag("wall") && moveControl.GetWallCount() == 0) { moveControl.SetWallBool(false); }
    }

    /*
     * Coroutine that causes virus to have greatly increased linear damping for a period of time
     */
    private IEnumerator Slowdown()
    {
        moveControl.SetLinDamp(nearWallLinDamping);
        yield return new WaitForSecondsRealtime(slowdownDuration);
        moveControl.SetLinDamp(0f);
        moveControl.SetSlowedDown(false);
    }
}
