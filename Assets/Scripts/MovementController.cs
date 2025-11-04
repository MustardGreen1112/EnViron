using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    [SerializeField] private GameObject modelVirus;
    [SerializeField] private float movementScalar;
    private Vector3 startingPosition;
    private Rigidbody rigid;
    private bool isHolding;

    // Drift back animation params
    [SerializeField] private float driftDuration = 3f;
    private AnimationCurve animationCurve;

    // Controls
    [SerializeField] private OVRInput.Controller controller;
    [SerializeField] private OVRInput.RawButton bumperButton;
    void Start()
    {
        startingPosition = modelVirus.GetComponent<Transform>().localPosition;
        rigid = GetComponent<Rigidbody>();
        isHolding = false;

        animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }

    void Update()
    {
        if (OVRInput.GetDown(bumperButton))
        {
            isHolding = true;
            StopAllCoroutines();
        }

        if (OVRInput.GetUp(bumperButton))
        {
            isHolding = false;
            StartCoroutine(DriftBack());
        }
    }

    void FixedUpdate()
    {
        if (isHolding)
        {
            Vector3 displacement = modelVirus.GetComponent<Transform>().localPosition - startingPosition;

            // Move this object according to the displacement of the model virus
            rigid.AddForce(displacement * movementScalar, ForceMode.Acceleration);
        }
    }

    private IEnumerator DriftBack()
    {
        float timer = 0f;
        Vector3 vectorDroppedAtPosition = modelVirus.GetComponent<Transform>().localPosition;

        while (timer < driftDuration)
        {
            if (isHolding) { break; }
            timer += Time.deltaTime;

            float t = timer / driftDuration;
            Vector3 newLocation = Vector3.Lerp(vectorDroppedAtPosition, startingPosition, t);
            modelVirus.GetComponent<Transform>().localPosition = newLocation;

            yield return null;
        }
    }
}
