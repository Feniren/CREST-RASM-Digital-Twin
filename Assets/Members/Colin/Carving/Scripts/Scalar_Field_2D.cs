using System;
using UnityEngine;

/// <summary>
/// Regular 2D scalar field with a one-cell padding ring. Positive = solid, negative = empty,
/// iso-surface at 0. Values are clamped to ±Cell: the linear zero crossing stays exact near the
/// surface and the interior costs nothing. Padded index Padding is the first interior cell; the
/// ring outside it is padding. Stamps touch interior cells only, so the padding keeps the block
/// boundary where Fill_Solid_Rect put it.
/// </summary>
public sealed class Scalar_Field_2D
{
    /// <summary>Signed distance to a tool, positive OUTSIDE it. Implemented by structs so a stamp allocates nothing.</summary>
    public interface I_Sdf
    {
        float Evaluate(Vector2 p);
    }

    public const int Padding = 1;          // padding cells on each side of the interior
    const float Centre_Offset = 0.5f;      // a cell's centre sits half a cell past its min corner
    const int Grow_Cells = 1;              // stamps cover the bbox plus this ring so the clamped band is complete

    public readonly int Nx;        // interior cells along u
    public readonly int Nz;        // interior cells along w
    public readonly int Width;     // Nx + 2 * Padding
    public readonly int Height;    // Nz + 2 * Padding
    public readonly float Cell;
    public readonly Vector2 Origin; // min corner of the interior, field units
    public readonly float[] V;

    public Scalar_Field_2D(int nx, int nz, float cell, Vector2 origin)
    {
        if (nx < 1 || nz < 1) throw new ArgumentException("Scalar_Field_2D needs at least one interior cell");
        if (cell <= 0f) throw new ArgumentException("Scalar_Field_2D cell must be positive");
        Nx = nx;
        Nz = nz;
        Width = nx + 2 * Padding;
        Height = nz + 2 * Padding;
        Cell = cell;
        Origin = origin;
        V = new float[Width * Height];
    }

    public float this[int ix, int iz]
    {
        get => V[iz * Width + ix];
        set => V[iz * Width + ix] = value;
    }

    public Vector2 Interior_Size => new Vector2(Nx * Cell, Nz * Cell);

    /// <summary>Centre of padded cell (ix, iz). Interior cell Padding spans [Origin, Origin + Cell].</summary>
    public Vector2 Cell_Centre(int ix, int iz)
    {
        return new Vector2(Origin.x + (ix - Padding + Centre_Offset) * Cell, Origin.y + (iz - Padding + Centre_Offset) * Cell);
    }

    public float Clamp(float d) => Mathf.Clamp(d, -Cell, Cell);

    /// <summary>
    /// Solid rectangle over the interior: clamped signed distance to the interior boundary.
    /// Edge interior cells get +Cell/2 and the padding ring -Cell/2, so the interpolated zero
    /// crossing lands exactly on the boundary.
    /// </summary>
    public void Fill_Solid_Rect()
    {
        Vector2 half = Interior_Size * 0.5f;
        Vector2 centre = Origin + half;
        for (int iz = 0; iz < Height; iz++)
        {
            for (int ix = 0; ix < Width; ix++)
            {
                Vector2 p = Cell_Centre(ix, iz);
                float qx = Mathf.Abs(p.x - centre.x) - half.x;
                float qy = Mathf.Abs(p.y - centre.y) - half.y;
                float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
                float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
                V[iz * Width + ix] = Clamp(-(outside + inside));
            }
        }
    }

    /// <summary>
    /// v = min(v, sdf(centre)) over interior cells inside bbox (grown by Grow_Cells). Returns true
    /// if any value changed. T is a struct so the call is specialised and allocation-free.
    /// </summary>
    public bool Subtract<T>(in T sdf, Rect bbox) where T : struct, I_Sdf
    {
        if (!Cell_Range(bbox, out int ix0, out int ix1, out int iz0, out int iz1)) return false;
        bool changed = false;
        for (int iz = iz0; iz <= iz1; iz++)
        {
            for (int ix = ix0; ix <= ix1; ix++)
            {
                int i = iz * Width + ix;
                float d = Clamp(sdf.Evaluate(Cell_Centre(ix, iz)));
                if (d < V[i]) { V[i] = d; changed = true; }
            }
        }
        return changed;
    }

    /// <summary>Disc stamp for the mill hot path: v = min(v, |p - centre| - radius). Returns true if any value changed.</summary>
    public bool Subtract_Disc(Vector2 centre, float radius)
    {
        var bbox = new Rect(centre.x - radius, centre.y - radius, radius * 2f, radius * 2f);
        if (!Cell_Range(bbox, out int ix0, out int ix1, out int iz0, out int iz1)) return false;
        bool changed = false;
        for (int iz = iz0; iz <= iz1; iz++)
        {
            float dz = Origin.y + (iz - Padding + Centre_Offset) * Cell - centre.y;
            for (int ix = ix0; ix <= ix1; ix++)
            {
                float dx = Origin.x + (ix - Padding + Centre_Offset) * Cell - centre.x;
                float d = Clamp(Mathf.Sqrt(dx * dx + dz * dz) - radius);
                int i = iz * Width + ix;
                if (d < V[i]) { V[i] = d; changed = true; }
            }
        }
        return changed;
    }

    // Interior index range whose cell centres can lie within bbox grown by Grow_Cells. False if none.
    bool Cell_Range(Rect bbox, out int ix0, out int ix1, out int iz0, out int iz1)
    {
        int last_x = Nx + Padding - 1;
        int last_z = Nz + Padding - 1;
        ix0 = Mathf.FloorToInt((bbox.xMin - Origin.x) / Cell + Padding - Centre_Offset) - Grow_Cells;
        ix1 = Mathf.CeilToInt((bbox.xMax - Origin.x) / Cell + Padding - Centre_Offset) + Grow_Cells;
        iz0 = Mathf.FloorToInt((bbox.yMin - Origin.y) / Cell + Padding - Centre_Offset) - Grow_Cells;
        iz1 = Mathf.CeilToInt((bbox.yMax - Origin.y) / Cell + Padding - Centre_Offset) + Grow_Cells;
        if (ix1 < Padding || iz1 < Padding || ix0 > last_x || iz0 > last_z) return false;
        ix0 = Mathf.Max(ix0, Padding);
        iz0 = Mathf.Max(iz0, Padding);
        ix1 = Mathf.Min(ix1, last_x);
        iz1 = Mathf.Min(iz1, last_z);
        return true;
    }
}
