using System;
using UnityEngine;

/// <summary>
/// Dummy cutting tool: a cylinder of Diameter x Length with a tip shape. Heights h are measured
/// from the tip along the tool axis. All units metres. Signed distances are positive OUTSIDE the
/// tool so a field carve is min(field, sdf).
/// </summary>
[Serializable]
public sealed class Tool_Profile
{
    public enum Tip { Flat, Ball, Drill }

    public float Diameter = 0.006f;
    public float Length = 0.030f;
    public Tip Tip_Shape = Tip.Flat;

    // Included drill point angle; the flank trig below is its half angle.
    const float Drill_Point_Deg = 118f;
    static readonly float Drill_Tan = Mathf.Tan(Drill_Point_Deg * 0.5f * Mathf.Deg2Rad);
    static readonly float Drill_Sin = Mathf.Sin(Drill_Point_Deg * 0.5f * Mathf.Deg2Rad);
    static readonly float Drill_Cos = Mathf.Cos(Drill_Point_Deg * 0.5f * Mathf.Deg2Rad);

    public float Radius => Diameter * 0.5f;

    /// <summary>Radius of the tool's cross-section at height h from the tip; 0 outside [0, Length].</summary>
    public float Radius_At_Height(float h)
    {
        if (h < 0f || h > Length) return 0f;
        float r = Radius;
        switch (Tip_Shape)
        {
            case Tip.Ball:
                if (h >= r) return r;
                return Mathf.Sqrt(Mathf.Max(0f, r * r - (r - h) * (r - h)));
            case Tip.Drill:
                return Mathf.Min(r, h * Drill_Tan);
            default:
                return r;
        }
    }

    /// <summary>
    /// Lathe: cross-section in the plane containing the axis. p and tip are (z, r); the axis
    /// points toward +r. Flat and Drill use the max of half-plane distances (sign-exact, distance-exact
    /// near each face); Ball is an exact capsule clipped at Length.
    /// </summary>
    public float Section_Sdf(Vector2 p, Vector2 tip)
    {
        float ax = Mathf.Abs(p.x - tip.x);
        float h = p.y - tip.y;
        float r = Radius;
        float len = Length;
        switch (Tip_Shape)
        {
            case Tip.Ball:
            {
                float hc = Mathf.Clamp(h, r, Mathf.Max(r, len));
                float dh = h - hc;
                float d = Mathf.Sqrt(ax * ax + dh * dh) - r;
                return Mathf.Max(d, h - len);
            }
            case Tip.Drill:
            {
                float flank = ax * Drill_Cos - h * Drill_Sin;
                return Mathf.Max(Mathf.Max(flank, ax - r), Mathf.Max(-h, h - len));
            }
            default:
                return Mathf.Max(ax - r, Mathf.Max(-h, h - len));
        }
    }

    public override string ToString()
    {
        return $"Ø{Diameter * 1000f:0.#} mm {Tip_Shape}, L {Length * 1000f:0.#} mm";
    }
}
