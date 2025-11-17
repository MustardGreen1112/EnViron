using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;

[System.Serializable]
public class Edge
{
    public Vector3[] edge;
    public int2 vertices;
    public int ID;
}