using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

/*
 * Model Virus Controller class
 *  -Freezes rotation of model virus
 *  -When player lets go of model virus, drives its animation back to original position
 */

public class ModelVirusController : MonoBehaviour
{
    /* Variables */
    private Quaternion startingRotation;
    private Vector3 startingPosition;
    private bool isHeld;

    /* Params (editable from inspector) */
    [SerializeField] private float driftDuration = 0.75f;
    
    [SerializeField] private OVRInput.Controller controller;
    [SerializeField] private OVRInput.RawButton grabButton;

    private AnimationCurve easeInOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initialize info about object
        startingRotation = transform.localRotation;
        startingPosition = transform.localPosition;
        isHeld = false;
    }

    // Update is called once every frame
    void Update()
    {
        // Hold the grab button (the bumper on the side of the controller) to make the grab start
        // Also clears all linear damping, allowing the virus to move freely
        if (OVRInput.GetDown(grabButton))
        {
            isHeld = true;
            StopAllCoroutines();
        }

        // Release it to end, and begin slowing down the virus by increasing linear damping
        if (OVRInput.GetUp(grabButton))
        {
            isHeld = false;
            StartCoroutine(DriftBack());
        }
    }

    // LateUpdate is called once per frame, after physics updates
    void LateUpdate()
    {
        // Rotation lock
        transform.rotation = startingRotation;
    }

    /* Setters/getters */
    public Vector3 GetDisplacement()
    {
        return transform.localPosition - startingPosition;
    }

    public bool GetIsHeld()
    {
        return isHeld;
    }

    /*
     * Coroutine that causes model virus to drift back to original position
     * Stops when either motion is complete or if grabbed in the middle of the animation
     */
    private IEnumerator DriftBack()
    {
        float timer = 0f;
        Vector3 droppedAtPosition = transform.localPosition;

        while (timer < driftDuration)
        {
            if (isHeld) { break; }
            timer += Time.deltaTime;

            float t = timer / driftDuration;
            float movementProgress = easeInOutCurve.Evaluate(t);
            Vector3 newLocation = Vector3.Lerp(droppedAtPosition, startingPosition, movementProgress);
            transform.localPosition = newLocation;

            yield return null;
        }
    }
}
