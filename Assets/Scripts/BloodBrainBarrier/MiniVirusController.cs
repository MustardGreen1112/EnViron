using UnityEngine;

public class MiniVirusController : MonoBehaviour
{
    public Transform target;          // Target to move toward
    public float speed = 5f;          // Movement speed units per second

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    
    void Update()
    {
        if (target == null) return;

        // Move towards target
        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        // destroy object when it reaches the target
        if (transform.position == target.position)
        {
            Destroy(gameObject);
        }
    }
}
