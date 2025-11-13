using UnityEngine;
using UnityEngine.VFX;

/*
 * Virus Generation Controller class
 *   -Handles requests to activate and deactivate the virus generator it is a component of
 */
public class VirusGenController : MonoBehaviour
{
    VisualEffect virusGen;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        virusGen = gameObject.GetComponent<VisualEffect>();
    }

    public void TurnOnVirusGen()
    {
        virusGen.SetFloat("virusSpawn", 2);
    }

    public void TurnOffVirusGen()
    {
        virusGen.SetFloat("virusSpawn", 0);
    }
}
