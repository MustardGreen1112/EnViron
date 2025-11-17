using UnityEngine;
using System.Collections.Generic;
using VascularGenerator.DataStructures;
using System;
using System.Linq;

public class ParallelTransportFrames
{
    public Vector3[] positions;
    public Vector3[] tangents;
    public Vector3[] normals;
    public Vector3[] binormals;
    public float[] radius;
    public int resolution;

    public ParallelTransportFrames(int resolution)
    {
        this.resolution = resolution;
    }
}
// Serializable wrapper classes
[Serializable]
public class CatmullRomSplineData
{
    public float alpha;
    public Vector3Serializable[] points;
    public float[] radius;
    public int segmentCount;
    public int curveID;
    public string tag;
}

[Serializable]
public class Vector3Serializable
{
    public float x, y, z;

    public Vector3Serializable(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

[Serializable]
public class CurveDictionaryData
{
    public List<CurveEntry> curves;
}

[Serializable]
public class CurveEntry
{
    public int curveId;
    public CatmullRomSplineData splineData;
}

public static class CatmullRomSplineExtensions
{
    public static CatmullRomSplineData ToSerializable(this CatmullRomSpline spline)
    {
        return new CatmullRomSplineData
        {
            alpha = spline.GetAlpha(),
            points = spline.GetPoints().Select(p => new Vector3Serializable(p)).ToArray(),
            radius = spline.GetRadius(),
            segmentCount = spline.GetSegmentCount(),
            curveID = spline.GetCurveID(),
            tag = spline.GetTag()
        };
    }

    public static CatmullRomSpline FromSerializable(this CatmullRomSplineData data)
    {
        Vector3[] points = data.points.Select(p => p.ToVector3()).ToArray();
        return new CatmullRomSpline(points, data.radius,data.curveID, data.tag, data.alpha);
    }
}

public class CatmullRomSpline
{
    [Range(0, 1)]
    public float alpha = 0.5f;
    private Vector3[] points;
    private float[] radius;
    private int segmentCount;
    private int curveID;
    private string tag;

    public Vector3[] GetPoints() => points;
    public int GetSegmentCount() => segmentCount;
    public int GetCurveID() => curveID;
    public float GetAlpha() => alpha;
    public float[] GetRadius() => radius;
    public string GetTag() => tag;

    public CatmullRomSpline(List<VascularSegment> segments, int curveID)
    {
        points = new Vector3[segments.Count + 3];
        radius = new float[segments.Count + 3];
        // Direction pointing back at the origin. Adding extra control point. 
        Vector3 dir_back = new Vector3((float)segments[0].startPoint[0], 0.0f, (float)segments[0].startPoint[1]) -
            new Vector3((float)segments[0].endPoint[0], 0.0f, (float)segments[0].endPoint[1]);
        dir_back = dir_back.normalized;
        points[0] = new Vector3((float)segments[0].startPoint[0], 0.0f, (float)segments[0].startPoint[1]) + 0.1f * dir_back;
        radius[0] = (float)segments[0].radius;
        int segCount = segments.Count;
        for (int i = 0; i < segCount; i++)
        {
            points[i + 1] = new Vector3((float)segments[i].startPoint[0], 0.0f, (float)segments[i].startPoint[1]);
            radius[i + 1] = (float)segments[i].radius;
        }
        points[segCount + 1] = new Vector3((float)segments[segCount - 1].endPoint[0], 0.0f, (float)segments[segCount - 1].endPoint[1]);
        // Direction pointing forward at the leaf node. Adding extra control point. 
        Vector3 dir_forward = new Vector3((float)segments[segCount - 1].endPoint[0], 0.0f, (float)segments[segCount - 1].endPoint[1]) -
                new Vector3((float)segments[segCount - 1].startPoint[0], 0.0f, (float)segments[segCount - 1].startPoint[1]);
        dir_forward = dir_forward.normalized;
        points[segCount + 2] = new Vector3((float)segments[segCount - 1].endPoint[0], 0.0f, (float)segments[segCount - 1].endPoint[1]) + 0.1f * dir_forward;
        radius[segCount + 2] = (float)segments[segCount - 1].radius;
        segmentCount = points.Length - 3;
        alpha = 0.5f;
        this.curveID = curveID;
    }

    public CatmullRomSpline(Vector3[] points, float[] radius, int curveID, string tag, float alpha = 0.5f)
    {
        this.points = points;
        this.curveID = curveID;
        this.alpha = alpha;
        this.segmentCount = points.Length - 3;
        this.tag = tag;
        this.radius = radius;
    }


    Vector3 GetPoint(int i)
    {
        return points[i];
    }

    CatmullRomCurve GetCurve(int i)
    {
        return new CatmullRomCurve(GetPoint(i), GetPoint(i + 1), GetPoint(i + 2), GetPoint(i + 3), alpha);
    }
    public Vector3 Eval(float t)
    {
        int n = Mathf.FloorToInt(t * segmentCount);
        t -= (t * n);
        return Eval(n, t);
    }
    public Vector3 EvalTangent(float t)
    {
        int n = Mathf.FloorToInt(t * segmentCount);
        t -= (t * n);
        return EvalTangent(n, t);
    }
    private Vector3 Eval(int seg, float t) => GetCurve(seg).GetPoint(t);
    private Vector3 EvalTangent(int seg, float t) => GetCurve(seg).GetTangent(t);

    public ParallelTransportFrames BuildFrames(int resolution = 10)
    {
        ParallelTransportFrames frames = new ParallelTransportFrames(resolution);

        // Total samples: first point + (SegmentCount * resolution - 1) more points
        // to avoid duplicates at segment boundaries
        int total = segmentCount * resolution - (segmentCount - 1);

        frames.positions = new Vector3[total];
        frames.tangents = new Vector3[total];
        frames.normals = new Vector3[total];
        frames.binormals = new Vector3[total];
        frames.radius = new float[total];

        // Initial frame at the very start
        Vector3 P0 = Eval(0, 0);
        Vector3 T0 = EvalTangent(0, 0).normalized;

        // Choose initial normal perpendicular to tangent
        Vector3 up = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(T0, up)) > 0.95f)
            up = Vector3.right;

        Vector3 N0 = (up - Vector3.Dot(up, T0) * T0).normalized;
        Vector3 B0 = Vector3.Cross(T0, N0).normalized;
        float R0 = frames.radius[0];

        frames.positions[0] = P0;
        frames.tangents[0] = T0;
        frames.normals[0] = N0;
        frames.binormals[0] = B0;
        frames.radius[0] = R0;

        Vector3 prevT = T0;
        Vector3 prevN = N0;
        Vector3 prevB = B0;

        int index = 1;

        for (int seg = 0; seg < segmentCount; seg++)
        {
            // For first segment, start at 1 (we already have point 0)
            // For subsequent segments, start at 1 to skip the duplicate endpoint
            int startS = (seg == 0) ? 1 : 1;

            for (int s = startS; s < resolution; s++)
            {
                float t = s / (float)(resolution - 1);

                Vector3 P = Eval(seg, t);
                Vector3 T = EvalTangent(seg, t).normalized;

                frames.positions[index] = P;
                frames.tangents[index] = T;

                // Parallel transport the frame
                Vector3 axis = Vector3.Cross(prevT, T);
                float axisLen = axis.magnitude;

                Vector3 N, B;

                if (axisLen < 1e-6f)
                {
                    // Tangents are parallel, no rotation needed
                    N = prevN;
                    B = prevB;
                }
                else
                {
                    // Rotate the frame
                    axis = axis / axisLen; // normalize
                    float angle = Mathf.Asin(Mathf.Clamp(axisLen, -1f, 1f));
                    Quaternion q = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, axis);

                    N = (q * prevN).normalized;
                    B = (q * prevB).normalized;
                }

                frames.normals[index] = N;
                frames.binormals[index] = B;
                frames.radius[index] = Mathf.Lerp(radius[index], index + 1 >= points.Length ? radius[index] : radius[index + 1], t);

                prevT = T;
                prevN = N;
                prevB = B;

                index++;
            }
        }

        return frames;
    }

    public void SampleFrame(ParallelTransportFrames frames, float t, out Vector3 N, out Vector3 B, out float radius)
    {
        int total = frames.normals.Length;
        float f = t * (total - 1);
        int i0 = Mathf.FloorToInt(f);
        int i1 = Mathf.Min(i0 + 1, total - 1);
        float lerp = f - i0;

        N = Vector3.Slerp(frames.normals[i0], frames.normals[i1], lerp).normalized;
        B = Vector3.Slerp(frames.binormals[i0], frames.binormals[i1], lerp).normalized;
        radius = Mathf.Lerp(frames.radius[i0], frames.radius[i1], lerp);
    }

    public void OnDrawGizmos(Transform t)
    {
        for (int i = 0; i < segmentCount; i++) DrawCurveSegment(GetCurve(i), t);
        for (int j = 0; j < points.Length; j++) Gizmos.DrawSphere(points[j] + t.position, 1.0f);
    }

    void DrawCurveSegment(CatmullRomCurve curve, Transform tr)
    {
        const int detail = 32; Vector3 prev = curve.p1;
        for (int i = 1; i < detail; i++)
        {
            float t = i / (detail - 1f); Vector3 pt = curve.GetPoint(t);
            Gizmos.DrawLine(prev + tr.position, pt + tr.position); 
            prev = pt;

        }
    }
}

public class MonoCatSpline : MonoBehaviour
{
    public CatmullRomSpline spline;
    void OnDrawGizmos()
    {
        spline.OnDrawGizmos(this.transform);
    }
}