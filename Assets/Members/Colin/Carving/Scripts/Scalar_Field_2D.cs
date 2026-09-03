using System;
using UnityEngine;

/// <summary>
/// Regular 2D scalar field with a one-cell padding ring. Positive = solid, negative = empty,
/// iso-surface at 0. Values are clamped to ±Cell: the linear zero crossing stays exact near the
/// surface and the interior costs nothing. Padded index 1 is the first interior cell; indices 0
/// and N+1 are padding. Stamps touch interior cells only, so the padding keeps the block boundary
/// where Fill_Solid_Rect put it.
/// </summary>
public sealed class Scalar_Field_2D
{
    public readonly int Nx;        // interior cells along u
    public readonly int Nz;        // interior cells along w
    public readonly int Width;     // Nx + 2
    public readonly int Height;    // Nz + 2
    public readonly float Cell;
    public readonly Vector2 Origin; // min corner of the interior, field units
    public readonly float[] V;
    public bool Dirty;

    public Scalar_Field_2D(int nx, int nz, float cell, Vector2 origin)
    {
        if (nx < 1 || nz < 1) throw new ArgumentException("Scalar_Field_2D needs at least one interior cell");
        if (cell <= 0f) throw new ArgumentException("Scalar_Field_2D cell must be positive");
        Nx = nx;
        Nz = nz;
        Width = nx + 2;
        Height = nz + 2;
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

    /// <summary>Centre of padded cell (ix, iz). Interior cell 1 spans [Origin, Origin + Cell].</summary>
    public Vector2 Cell_Centre(int ix, int iz)
    {
        return new Vector2(Origin.x + (ix - 0.5f) * Cell, Origin.y + (iz - 0.5f) * Cell);
    }

    public bool Is_Solid(int ix, int iz) => V[iz * Width + ix] > 0f;

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
        Dirty = true;
    }

    /// <summary>
    /// v = min(v, sdf(centre)) over interior cells inside bbox (+1 cell). sdf is positive OUTSIDE
    /// the tool. Returns true if any value changed.
    /// </summary>
    public bool Subtract(Func<Vector2, float> tool_sdf, Rect bbox)
    {
        if (!Cell_Range(bbox, out int ix0, out int ix1, out int iz0, out int iz1)) return false;
        bool changed = false;
        for (int iz = iz0; iz <= iz1; iz++)
        {
            for (int ix = ix0; ix <= ix1; ix++)
            {
                int i = iz * Width + ix;
                float d = Clamp(tool_sdf(Cell_Centre(ix, iz)));
                if (d < V[i]) { V[i] = d; changed = true; }
            }
        }
        Dirty |= changed;
        return changed;
    }

    /// <summary>Allocation-free disc stamp: v = min(v, |p - centre| - radius). Returns true if any value changed.</summary>
    public bool Subtract_Disc(Vector2 centre, float radius)
    {
        var bbox = new Rect(centre.x - radius, centre.y - radius, radius * 2f, radius * 2f);
        if (!Cell_Range(bbox, out int ix0, out int ix1, out int iz0, out int iz1)) return false;
        bool changed = false;
        for (int iz = iz0; iz <= iz1; iz++)
        {
            float dz = Origin.y + (iz - 0.5f) * Cell - centre.y;
            for (int ix = ix0; ix <= ix1; ix++)
            {
                float dx = Origin.x + (ix - 0.5f) * Cell - centre.x;
                float d = Clamp(Mathf.Sqrt(dx * dx + dz * dz) - radius);
                int i = iz * Width + ix;
                if (d < V[i]) { V[i] = d; changed = true; }
            }
        }
        Dirty |= changed;
        return changed;
    }

    // Interior index range whose cell centres can lie within bbox grown by one cell. False if none.
    bool Cell_Range(Rect bbox, out int ix0, out int ix1, out int iz0, out int iz1)
    {
        ix0 = Mathf.FloorToInt((bbox.xMin - Origin.x) / Cell + 0.5f) - 1;
        ix1 = Mathf.CeilToInt((bbox.xMax - Origin.x) / Cell + 0.5f) + 1;
        iz0 = Mathf.FloorToInt((bbox.yMin - Origin.y) / Cell + 0.5f) - 1;
        iz1 = Mathf.CeilToInt((bbox.yMax - Origin.y) / Cell + 0.5f) + 1;
        if (ix1 < 1 || iz1 < 1 || ix0 > Nx || iz0 > Nz) return false;
        ix0 = Mathf.Max(ix0, 1);
        iz0 = Mathf.Max(iz0, 1);
        ix1 = Mathf.Min(ix1, Nx);
        iz1 = Mathf.Min(iz1, Nz);
        return true;
    }
}
