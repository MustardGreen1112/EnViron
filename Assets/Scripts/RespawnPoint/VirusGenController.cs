using UnityEngine;
using UnityEngine.VFX;

/*
 * Virus Generation Controller class
 *   -Handles requests to activate and deactivate the virus generator it is a component of
 *   -Turns off virus generation of its own accord at random times (toggleable)
 */
public class VirusGenController : MonoBehaviour
{
    VisualEffect virusGen;
    public bool isRespawnPoint;

    [SerializeField] bool respawnDecay = false;
    [SerializeField] int respawnDecayChance = 1000;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        virusGen = gameObject.GetComponent<VisualEffect>();
        isRespawnPoint = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (isRespawnPoint && respawnDecay)
        {
            int randInt = Random.Range(0, respawnDecayChance);
            if (randInt == respawnDecayChance) { TurnOffVirusGen(); }
        }
    }

    public void TurnOnVirusGen()
    {
        virusGen.SetFloat("virusSpawn", 2);
    }

    public void TurnOffVirusGen()
    {
        virusGen.SetFloat("virusSpawn", 0);
    }

    public bool GetIsRespawn()
    {
        return isRespawnPoint;
    }
}
