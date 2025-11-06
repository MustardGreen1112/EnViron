using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    [SerializeField] private GameObject modelVirus;

    // Movement scalars
    [SerializeField] private float movementScalar = 7f;       // determines what the force on the virus will
                                                              // be relative to the displacement of the model
    [SerializeField] private float releasePenalty = 0.7f;     // linear damping applied when object is released

    private Vector3 startingPosition; // starting position of model virus to return to
    private Rigidbody rigid;          // rigidbody of this object
    private bool isHolding;           // is the player holding the model virus?

    // Slow down animation params
    public bool isNearWall;          // is this virus near a wall?
    public int wallsEntered;         // variable used to ensure isNearWall is only false when not near any wall object
    public Animator linDampAnimator;  // animator to drive linear damping near walls

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
        isNearWall = false;
        wallsEntered = 0;

        animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }

    void Update()
    {
        // Hold the grab button (the bumper on the side of the controller) to make the grab start
        // Also clears all linear damping, allowing the virus to move freely
        if (OVRInput.GetDown(bumperButton))
        {
            isHolding = true;
            if (!isNearWall) { rigid.linearDamping = 0; }
            StopAllCoroutines();
        }

        // Release it to end, and begin slowing down the virus by increasing linear damping
        if (OVRInput.GetUp(bumperButton))
        {
            isHolding = false;
            if (!isNearWall) { rigid.linearDamping = releasePenalty; }
            StartCoroutine(DriftBack());
        }
    }

    void FixedUpdate()
    {
        // When the model virus is held, apply a force to the virus the player is inside proportional
        // to the displacement of the model from its original location
        if (isHolding)
        {
            Vector3 displacement = modelVirus.GetComponent<Transform>().localPosition - startingPosition;

            // Move this object according to the displacement of the model virus
            if (linDampAnimator.GetCurrentAnimatorStateInfo(0).IsName("virusNormal")) { rigid.AddForce(displacement * movementScalar, ForceMode.Acceleration); }
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
            float movementProgress = animationCurve.Evaluate(t);
            Vector3 newLocation = Vector3.Lerp(vectorDroppedAtPosition, startingPosition, movementProgress);
            modelVirus.GetComponent<Transform>().localPosition = newLocation;

            yield return null;
        }
    }
}
