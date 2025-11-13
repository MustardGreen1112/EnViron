using UnityEngine;
using UnityEngine.VFX;

/*
 * Model Virus Controller class
 *  -Directs functions from overarching hologram animator events to individual parts
 *  -Essentially a wrapper for the hologram virus DeactivateParticles call at the moment
 */
public class HologramAnimationController : MonoBehaviour
{
    [SerializeField] GameObject hologramVirusControlObj;
    HologramVirusController hologramVirusControl;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        hologramVirusControl = hologramVirusControlObj.GetComponent<HologramVirusController>();
    }

    public void DeactivateParticles()
    {
        hologramVirusControl.DeactivateParticles();
    }
}
