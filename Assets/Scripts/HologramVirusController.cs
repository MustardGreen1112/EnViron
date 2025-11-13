using UnityEngine;
using UnityEngine.VFX;

/*
 * Hologram Controller class
 *  -Every frame, renders line between hologram and model virus
 *  -Toggles visibility of hologram based on 
 */
public class HologramVirusController : MonoBehaviour
{
    [SerializeField] private GameObject hologram;
    [SerializeField] private GameObject modelVirus;
    [SerializeField] private GameObject introParticleSystem;

    private LineRenderer line;
    private VisualEffect particles;
    private ModelVirusController modelVirusController;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        line = gameObject.GetComponent<LineRenderer>();
        particles = introParticleSystem.GetComponent<VisualEffect>();
        modelVirusController = modelVirus.GetComponent<ModelVirusController>();
    }

    // Update is called once per frame
    void Update()
    {
        line.SetPosition(1, modelVirus.transform.localPosition);
        if (modelVirusController.GetIsHeld()) { hologram.SetActive(true); }
        else { hologram.SetActive(false); }
    }

    public void DeactivateParticles()
    {
        particles.SetFloat("spawnRate", 0);
    }  
}
