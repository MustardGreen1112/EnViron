using UnityEngine;

// a single Catmull–Rom curve
public struct CatmullRomCurve
{
    public Vector3 p0, p1, p2, p3;
    public float alpha;

    public CatmullRomCurve(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float alpha)
    {
        (this.p0, this.p1, this.p2, this.p3) = (p0, p1, p2, p3);
        this.alpha = alpha;
    }

    // Evaluates a point at the given t-value from 0 to 1
    public Vector3 GetPoint(float t)
    {
        // calculate knots
        const float k0 = 0;
        float k1 = GetKnotInterval(p0, p1);
        float k2 = GetKnotInterval(p1, p2) + k1;
        float k3 = GetKnotInterval(p2, p3) + k2;

        // evaluate the point
        float u = Mathf.LerpUnclamped(k1, k2, t);
        Vector3 A1 = Remap(k0, k1, p0, p1, u);
        Vector3 A2 = Remap(k1, k2, p1, p2, u);
        Vector3 A3 = Remap(k2, k3, p2, p3, u);
        Vector3 B1 = Remap(k0, k2, A1, A2, u);
        Vector3 B2 = Remap(k1, k3, A2, A3, u);
        return Remap(k1, k2, B1, B2, u);
    }

    static Vector3 Remap(float a, float b, Vector3 c, Vector3 d, float u)
    {
        return Vector3.LerpUnclamped(c, d, (u - a) / (b - a));
    }

    float GetKnotInterval(Vector3 a, Vector3 b)
    {
        return Mathf.Pow(Vector3.SqrMagnitude(a - b), 0.5f * alpha);
    }

    public Vector3 GetTangent(float t)
    {
        const float k0 = 0;
        float k1 = GetKnotInterval(p0, p1);
        float k2 = k1 + GetKnotInterval(p1, p2);
        float k3 = k2 + GetKnotInterval(p2, p3);

        float u = Mathf.Lerp(k1, k2, t);
        float du = (k2 - k1);

        // A’s
        Vector3 A1 = Remap(k0, k1, p0, p1, u);
        Vector3 A2 = Remap(k1, k2, p1, p2, u);
        Vector3 A3 = Remap(k2, k3, p2, p3, u);

        Vector3 A1p = (p1 - p0) / (k1 - k0) * du;
        Vector3 A2p = (p2 - p1) / (k2 - k1) * du;
        Vector3 A3p = (p3 - p2) / (k3 - k2) * du;

        // B’s
        float s1 = (u - k0) / (k2 - k0);
        float s2 = (u - k1) / (k3 - k1);
        float s1p = du / (k2 - k0);
        float s2p = du / (k3 - k1);

        Vector3 B1 = A1 * (1 - s1) + A2 * s1;
        Vector3 B2 = A2 * (1 - s2) + A3 * s2;

        Vector3 B1p = A1p * (1 - s1) + A2p * s1 + (A2 - A1) * s1p;
        Vector3 B2p = A2p * (1 - s2) + A3p * s2 + (A3 - A2) * s2p;

        // final
        float s = (u - k1) / (k2 - k1);
        float sp = du / (k2 - k1);

        Vector3 C = B1 * (1 - s) + B2 * s;
        Vector3 Cp = B1p * (1 - s) + B2p * s + (B2 - B1) * sp;

        return Cp;
    }
}