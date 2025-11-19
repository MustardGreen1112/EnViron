using UnityEngine;
using UnityEngine.SceneManagement;

public class BloodBrainBarrier : MonoBehaviour
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("triggered");
        if (other.CompareTag("Player"))
        {
            WinSceneChange();
        }
    }

    public void WinSceneChange()
    {
        SceneManager.LoadScene("WinScene");
    }
}
