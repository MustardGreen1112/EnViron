using UnityEngine;

public class HologramAnimationTrigger : MonoBehaviour
{
    Animator hologramAnimator;
    public GameObject hologram;
    public MapGenerator mapgen;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        hologramAnimator = GetComponentInChildren<Animator>();
        mapgen.mapParent.transform.SetParent(transform);
        mapgen.mapParent.transform.position = hologram.transform.position;
        mapgen.mapParent.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) 
        { 
            hologramAnimator.SetTrigger("mapControlSwitch");
        }
    }

    public void ToggleMapVisible()
    {
        mapgen.mapParent.SetActive(!mapgen.mapParent.activeSelf);
    }
}
