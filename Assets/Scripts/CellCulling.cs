using UnityEngine;
using DataStructures.ViliWonka.KDTree;
using System.Collections.Generic;

public class CellCulling : MonoBehaviour
{
    private KDTree kdtree;
    [Range(0f, 200.0f)]
    public float radius = 10.0f;
    private Dictionary<Vector3, GameObject> pgpairs;
    private Vector3[] pointCloud;
    KDQuery query;
    public int maxPointPerLeafNode = 128;
    public Transform playerTransform;
    private Vector3 previousPlayerLocation;
    /// <summary>
    /// The distance when we update the kdtree. 
    /// </summary>
    public float updateDistance = 0.75f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        query = new KDQuery();
        pgpairs = new Dictionary<Vector3, GameObject>();
        int pointCount = 0;
        foreach (Transform tunnel in transform)
        {
            pointCount += tunnel.childCount;
            foreach (Transform cell in tunnel.transform)
            {
                pgpairs.Add(cell.localPosition, cell.gameObject);
            }
        }
        pointCloud = new Vector3[pointCount];
        int index = 0;
        foreach(Vector3 position in pgpairs.Keys)
        {
            pointCloud[index] = position;
            index++;
        }
        kdtree = new KDTree(pointCloud, maxPointPerLeafNode);

        var resultIndices = new List<int>();
        query.Radius(kdtree, playerTransform.position, radius, resultIndices);
        foreach (GameObject go in pgpairs.Values) go.SetActive(false);
        foreach (int id in resultIndices) pgpairs[pointCloud[id]].SetActive(true);
        previousPlayerLocation = playerTransform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if(Vector3.Distance(playerTransform.position, previousPlayerLocation) <= updateDistance)
        {
            return;
        }
        else
        {
            var resultIndices = new List<int>();
            query.Radius(kdtree, playerTransform.position, radius, resultIndices);
            foreach (GameObject go in pgpairs.Values) go.SetActive(false);
            foreach (int id in resultIndices) pgpairs[pointCloud[id]].SetActive(true);
            previousPlayerLocation = playerTransform.position;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerTransform.position, radius);
    }
}
