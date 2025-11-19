using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;

public class WinSceneManager : MonoBehaviour
{
    public GameObject player;
    public GameObject brain;
    public GameObject miniVirus;
    // public int maxNumberViruses;
    // int numberViruses=0;
    public float spawnRadius;
    public float speedRange; 
    public bool virusSpawning; 
    


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //putting the player a distance away from the brain and facing them towards it
        player.transform.position = new Vector3(1f, 1f, -8f);
        player.transform.LookAt(brain.transform);

        virusSpawning = true;
        StartCoroutine(CreateVirusSpawning());


    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpawnVirus()
    {
        
        Vector3 pos = new Vector3(UnityEngine.Random.Range(-spawnRadius, spawnRadius), UnityEngine.Random.Range(-spawnRadius, spawnRadius), UnityEngine.Random.Range(-spawnRadius, spawnRadius));
        GameObject newVirus = Instantiate(miniVirus, pos, quaternion.identity);
        MiniVirusController virusController = newVirus.GetComponent<MiniVirusController>();
        virusController.speed = UnityEngine.Random.Range(2f, Math.Max(5f, speedRange));
        virusController.target = brain.transform;
        
        // numberViruses++;
        // if(numberViruses >= maxNumberViruses)
        // {
        //     TurnOffVirusSpawning();
        // }
    }

    IEnumerator CreateVirusSpawning()
    {
        while (virusSpawning)
        {
            // Wait for a random time between minInterval and maxInterval
            float waitTime = UnityEngine.Random.Range(0.01f, 0.5f);
            yield return new WaitForSeconds(waitTime);

            // Call your function here
            SpawnVirus();
        }
    }

    public void TurnOffVirusSpawning()
    {
        virusSpawning = false;
    }

}
