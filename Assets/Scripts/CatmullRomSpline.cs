using UnityEngine;

public class ParallelTransportFrames
{
    public Vector3[] positions;
    public Vector3[] tangents;
    public Vector3[] normals;
    public Vector3[] binormals;
    public int resolution;

    public ParallelTransportFrames(int resolution)
    {
        this.resolution = resolution;
    }
}

public class CatmullRomSpline : MonoBehaviour
{
    [Range(0, 1)]
    public float alpha = 0.5f;

    int PointCount => transform.childCount;
    public int SegmentCount => PointCount - 3;

    Vector3 GetPoint(int i) => transform.GetChild(i).position;

    CatmullRomCurve GetCurve(int i)
    {
        return new CatmullRomCurve(GetPoint(i), GetPoint(i + 1), GetPoint(i + 2), GetPoint(i + 3), alpha);
    }

    public Vector3 Eval(int seg, float t) => GetCurve(seg).GetPoint(t);
    public Vector3 EvalTangent(int seg, float t) => GetCurve(seg).GetTangent(t);

    public ParallelTransportFrames BuildFrames(int resolution = 10)
    {
        ParallelTransportFrames frames = new ParallelTransportFrames(resolution);

        // Total samples: first point + (SegmentCount * resolution - 1) more points
        // to avoid duplicates at segment boundaries
        int total = SegmentCount * resolution - (SegmentCount - 1);

        frames.positions = new Vector3[total];
        frames.tangents = new Vector3[total];
        frames.normals = new Vector3[total];
        frames.binormals = new Vector3[total];

        // Initial frame at the very start
        Vector3 P0 = Eval(0, 0);
        Vector3 T0 = EvalTangent(0, 0).normalized;

        // Choose initial normal perpendicular to tangent
        Vector3 up = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(T0, up)) > 0.95f)
            up = Vector3.right;

        Vector3 N0 = (up - Vector3.Dot(up, T0) * T0).normalized;
        Vector3 B0 = Vector3.Cross(T0, N0).normalized;

        frames.positions[0] = P0;
        frames.tangents[0] = T0;
        frames.normals[0] = N0;
        frames.binormals[0] = B0;

        Vector3 prevT = T0;
        Vector3 prevN = N0;
        Vector3 prevB = B0;

        int index = 1;

        for (int seg = 0; seg < SegmentCount; seg++)
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

                prevT = T;
                prevN = N;
                prevB = B;

                index++;
            }
        }

        return frames;
    }

    public void SampleFrame(ParallelTransportFrames frames, float t, out Vector3 N, out Vector3 B)
    {
        int total = frames.normals.Length;
        float f = t * (total - 1);
        int i0 = Mathf.FloorToInt(f);
        int i1 = Mathf.Min(i0 + 1, total - 1);
        float lerp = f - i0;

        N = Vector3.Slerp(frames.normals[i0], frames.normals[i1], lerp).normalized;
        B = Vector3.Slerp(frames.binormals[i0], frames.binormals[i1], lerp).normalized;
    }

    void OnDrawGizmos()
    {
        for (int i = 0; i < SegmentCount; i++) DrawCurveSegment(GetCurve(i));
        for (int j = 0; j < PointCount; j++) Gizmos.DrawSphere(transform.GetChild(j).position, 0.1f);
    }
    void DrawCurveSegment(CatmullRomCurve curve)
    {
        const int detail = 32; Vector3 prev = curve.p1;
        for (int i = 1; i < detail; i++)
        {
            float t = i / (detail - 1f); Vector3 pt = curve.GetPoint(t);
            Gizmos.DrawLine(prev, pt); prev = pt;
        }
    }
}