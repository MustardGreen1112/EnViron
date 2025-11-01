using UnityEngine;

public class VirusRotationLock : MonoBehaviour
{
    Quaternion rotation;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rotation = transform.rotation;
    }

    // LateUpdate is called once per frame, after physics updates
    void LateUpdate()
    {
        transform.rotation = rotation;
    }
}
