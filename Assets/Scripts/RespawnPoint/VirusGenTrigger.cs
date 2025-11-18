using Unity.XR.CoreUtils;
using UnityEngine;

public class VirusGenTrigger : MonoBehaviour
{
    VirusGenController virusGen;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        virusGen = GetComponent<VirusGenController>();
    }

    // OnTriggerEnter is called when this object's Collider overlaps with another Collider,
    // at least one of them being set to trigger
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("triggered");
        if (other.CompareTag("Player"))
        {
            virusGen.TurnOnVirusGen();
        }
        
    }
}
