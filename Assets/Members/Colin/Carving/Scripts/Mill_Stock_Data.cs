using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stacked-slab stock for the mill: Slab_Count horizontal Scalar_Field_2D slabs over local XZ
/// (field u = x, w = z); slab k spans local y in [Slab_Bottom(k), Slab_Top(k)]. Local origin is
/// the block's min corner. Plain C# so it can be exercised without a scene.
///
/// Carving samples each slab at its centre height (±Pitch/2 depth error) and sub-steps the tool
/// motion so consecutive stamps overlap. Meshing per slab: marching-squares contour segments
/// become extruded side walls, and the fill becomes top/bottom caps. A cap cell is skipped when
/// the neighbouring slab is at least as solid at all four corners (its fill is then a superset of
/// ours, so our cap would be an interior face), which hides coplanar interior caps while keeping
/// thin exposed rings (a combined-field approach drops rings narrower than one cell).
/// </summary>
public sealed class Mill_Stock_Data
{
    public const int Max_Sub_Steps = 64;

    public readonly Vector3 Size;      // actual size (x and z rounded to whole cells)
    public readonly float Cell;
    public readonly float Pitch;
    public readonly int Nx;
    public readonly int Nz;
    public readonly int Slab_Count;
    public readonly Scalar_Field_2D[] Slabs;

    readonly bool[] dirty;
    static readonly List<Vector2> segs = new List<Vector2>();
    static readonly List<Vector2> fill = new List<Vector2>();

    public int Last_Sub_Steps { get; private set; }
    /// <summary>Set once a single Carve exceeded Max_Sub_Steps (a teleport left gaps).</summary>
    public bool Gap_Warning { get; private set; }

    public Mill_Stock_Data(Vector3 size, float cell, float pitch)
    {
        if (cell <= 0f || pitch <= 0f) throw new ArgumentException("cell and pitch must be positive");
        Nx = Mathf.Max(1, Mathf.RoundToInt(size.x / cell));
        Nz = Mathf.Max(1, Mathf.RoundToInt(size.z / cell));
        Slab_Count = Mathf.Max(1, Mathf.CeilToInt(size.y / pitch - 1e-4f));
        Size = new Vector3(Nx * cell, size.y, Nz * cell);
        Cell = cell;
        Pitch = pitch;
        Slabs = new Scalar_Field_2D[Slab_Count];
        dirty = new bool[Slab_Count];
        for (int k = 0; k < Slab_Count; k++) Slabs[k] = new Scalar_Field_2D(Nx, Nz, cell, Vector2.zero);
        Reset();
    }

    public float Slab_Bottom(int k) => k * Pitch;
    public float Slab_Top(int k) => Mathf.Min((k + 1) * Pitch, Size.y);
    public float Slab_Centre(int k) => 0.5f * (Slab_Bottom(k) + Slab_Top(k));

    public void Reset()
    {
        for (int k = 0; k < Slab_Count; k++)
        {
            Slabs[k].Fill_Solid_Rect();
            dirty[k] = true;
        }
    }

    public bool Is_Dirty(int k) => dirty[k];
    public void Clear_Dirty() => Array.Clear(dirty, 0, dirty.Length);

    /// <summary>Carve the swept tool path from prev_tip to curr_tip (stock-local metres, tool axis +Y).</summary>
    public void Carve(Tool_Profile tool, Vector3 prev_tip, Vector3 curr_tip)
    {
        Vector3 d = curr_tip - prev_tip;
        float dist = d.magnitude;
        int n = dist <= 1e-9f ? 1 : Mathf.CeilToInt(dist / (Cell * 0.5f));
        if (n > Max_Sub_Steps) { n = Max_Sub_Steps; Gap_Warning = true; }
        Last_Sub_Steps = n;
        for (int s = 1; s <= n; s++) Stamp(tool, prev_tip + d * ((float)s / n));
    }

    void Stamp(Tool_Profile tool, Vector3 tip)
    {
        var tip_xz = new Vector2(tip.x, tip.z);
        for (int k = 0; k < Slab_Count; k++)
        {
            float h = Slab_Centre(k) - tip.y;
            if (h < 0f || h > tool.Length) continue;
            float r = tool.Radius_At_Height(h);
            if (r <= 0f) continue;
            if (Slabs[k].Subtract_Disc(tip_xz, r)) Mark_Dirty(k);
        }
    }

    void Mark_Dirty(int k)
    {
        dirty[k] = true;
        if (k > 0) dirty[k - 1] = true;
        if (k + 1 < Slab_Count) dirty[k + 1] = true;
    }

    /// <summary>
    /// Builds slab k's geometry (flat shaded, unshared vertices) into verts/tris (appended).
    /// Returns false if the slab produced no triangles. cull_caps=false emits every cap, which
    /// makes each slab a closed surface on its own — used by the watertight test.
    /// </summary>
    public bool Build_Slab_Mesh(int k, List<Vector3> verts, List<int> tris, bool cull_caps = true)
    {
        var f = Slabs[k];
        float y0 = Slab_Bottom(k);
        float y1 = Slab_Top(k);
        var above = k + 1 < Slab_Count ? Slabs[k + 1] : null;
        var below = k > 0 ? Slabs[k - 1] : null;
        int start = tris.Count;

        for (int iz = 0; iz < f.Height - 1; iz++)
        {
            for (int ix = 0; ix < f.Width - 1; ix++)
            {
                float v0 = f[ix, iz], v1 = f[ix + 1, iz], v2 = f[ix + 1, iz + 1], v3 = f[ix, iz + 1];
                if (v0 <= 0f && v1 <= 0f && v2 <= 0f && v3 <= 0f) continue;

                bool top = !cull_caps || !Covers(above, ix, iz, v0, v1, v2, v3);
                bool bottom = !cull_caps || !Covers(below, ix, iz, v0, v1, v2, v3);

                segs.Clear();
                fill.Clear();
                Marching_Squares.Evaluate(f.Cell_Centre(ix, iz), f.Cell_Centre(ix + 1, iz), f.Cell_Centre(ix + 1, iz + 1), f.Cell_Centre(ix, iz + 1),
                                          v0, v1, v2, v3, segs, top || bottom ? fill : null);

                for (int i = 0; i < segs.Count; i += 2) Add_Wall(segs[i], segs[i + 1], y0, y1, verts, tris);

                if (top)
                {
                    // fill is CCW in (x,z) which faces -Y; reverse for the +Y cap
                    for (int i = 0; i < fill.Count; i += 3)
                        Add_Tri(fill[i], fill[i + 2], fill[i + 1], y1, verts, tris);
                }
                if (bottom)
                {
                    for (int i = 0; i < fill.Count; i += 3)
                        Add_Tri(fill[i], fill[i + 1], fill[i + 2], y0, verts, tris);
                }
            }
        }
        return tris.Count > start;
    }

    // Linear interpolation of larger corner values is larger everywhere, so a neighbour that is
    // at least as solid at every corner has a fill region that contains this cell's fill.
    static bool Covers(Scalar_Field_2D n, int ix, int iz, float v0, float v1, float v2, float v3)
    {
        return n != null && n[ix, iz] >= v0 && n[ix + 1, iz] >= v1 && n[ix + 1, iz + 1] >= v2 && n[ix, iz + 1] >= v3;
    }

    // Segment a->b has solid on its left (viewed from +Y); the quad below faces the empty side.
    static void Add_Wall(Vector2 a, Vector2 b, float y0, float y1, List<Vector3> verts, List<int> tris)
    {
        int i = verts.Count;
        verts.Add(new Vector3(a.x, y0, a.y));
        verts.Add(new Vector3(a.x, y1, a.y));
        verts.Add(new Vector3(b.x, y1, b.y));
        verts.Add(new Vector3(b.x, y0, b.y));
        tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
        tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
    }

    static void Add_Tri(Vector2 a, Vector2 b, Vector2 c, float y, List<Vector3> verts, List<int> tris)
    {
        int i = verts.Count;
        verts.Add(new Vector3(a.x, y, a.y));
        verts.Add(new Vector3(b.x, y, b.y));
        verts.Add(new Vector3(c.x, y, c.y));
        tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
    }
}
