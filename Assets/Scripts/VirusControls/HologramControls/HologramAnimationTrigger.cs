using UnityEngine;

public class HologramAnimationTrigger : MonoBehaviour
{
    Animator hologramAnimator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        hologramAnimator = GetComponentInChildren<Animator>();
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("trigger entered");
        if (other.CompareTag("Player")) { hologramAnimator.SetTrigger("mapControlSwitch"); }
    }
}
