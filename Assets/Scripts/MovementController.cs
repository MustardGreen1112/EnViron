using UnityEngine;

public class MovementController : MonoBehaviour
{
    [SerializeField] private GameObject modelVirus;
    private Vector3 startingPosition;
    private Rigidbody rigid;

    void Start()
    {
        startingPosition = modelVirus.GetComponent<Transform>().localPosition;
        rigid = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        Vector3 displacement = modelVirus.GetComponent<Transform>().localPosition - startingPosition;

        // Move this object according to the displacement of the model virus
        rigid.AddForce(displacement, ForceMode.Acceleration);
    }
}
