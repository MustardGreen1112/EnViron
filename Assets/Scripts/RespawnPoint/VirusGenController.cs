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
    public int id;
    // private static int idMaster = 0;
    [SerializeField] bool respawnDecay = false;
    [SerializeField] int respawnDecayChance = 1000; // 1/respawnDecayChance is the respawn decay chance at at every update frame

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        virusGen = gameObject.GetComponent<VisualEffect>();
        isRespawnPoint = false;

        // id = idMaster;
        // idMaster++;
    }

    // Update is called once per frame
    void Update()
    {
        if (isRespawnPoint && respawnDecay)
        {
            int randInt = Random.Range(0, respawnDecayChance);
            if (randInt == respawnDecayChance) { TurnOffVirusGen(); }
        }

        if (!isRespawnPoint && virusGen.GetFloat("virusSpawn")!=0)
        {
            TurnOffVirusGen();
        }
    }

    public void TurnOnVirusGen()
    {
        virusGen.SetFloat("virusSpawn", 2);
        isRespawnPoint = true;
    }

    public void TurnOffVirusGen()
    {
        virusGen.SetFloat("virusSpawn", 0);
        isRespawnPoint = false;
    }

    public bool GetIsRespawn()
    {
        return isRespawnPoint;
    }
}
