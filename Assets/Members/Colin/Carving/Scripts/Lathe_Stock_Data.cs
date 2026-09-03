using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lathe stock as one 2D field over the (z, r) half-section: u = z along the axis (0..Length),
/// w = r (0..Radius). The stock is assumed to spin, so a tool at (z, r) removes everything at
/// radius >= r along its z extent. The marching-squares contour is revolved around local +Z.
///
/// Padding: empty beyond both z ends and beyond Radius, but the r<0 row mirrors the first
/// interior row so the contour never runs along the axis; crossings that land at negative r are
/// clamped to 0 by the revolve and collapse into a fan apex.
/// </summary>
public sealed class Lathe_Stock_Data
{
    public const int Max_Sub_Steps = 64;

    public readonly float Length;   // rounded to whole cells
    public readonly float Radius;   // rounded to whole cells
    public readonly float Cell;
    public readonly int Nz;
    public readonly int Nr;
    public readonly Scalar_Field_2D Field;

    public bool Dirty;
    public int Last_Sub_Steps { get; private set; }
    public bool Gap_Warning { get; private set; }

    static readonly List<Vector2> segs = new List<Vector2>();
    static float[] cos_table, sin_table;

    public Lathe_Stock_Data(float length, float radius, float cell)
    {
        if (cell <= 0f) throw new ArgumentException("cell must be positive");
        Nz = Mathf.Max(1, Mathf.RoundToInt(length / cell));
        Nr = Mathf.Max(1, Mathf.RoundToInt(radius / cell));
        Length = Nz * cell;
        Radius = Nr * cell;
        Cell = cell;
        Field = new Scalar_Field_2D(Nz, Nr, cell, Vector2.zero);
        Reset();
    }

    public void Reset()
    {
        Field.Fill_Solid_Rect();
        Mirror_Axis();
        Dirty = true;
    }

    void Mirror_Axis()
    {
        for (int ix = 0; ix < Field.Width; ix++) Field[ix, 0] = Field[ix, 1];
    }

    /// <summary>Carve the swept tool path; tips are (z, r) in stock-local metres, tool axis toward +r.</summary>
    public void Carve(Tool_Profile tool, Vector2 prev_tip, Vector2 curr_tip)
    {
        Vector2 d = curr_tip - prev_tip;
        float dist = d.magnitude;
        int n = dist <= 1e-9f ? 1 : Mathf.CeilToInt(dist / (Cell * 0.5f));
        if (n > Max_Sub_Steps) { n = Max_Sub_Steps; Gap_Warning = true; }
        Last_Sub_Steps = n;
        for (int s = 1; s <= n; s++) Stamp(tool, prev_tip + d * ((float)s / n));
    }

    void Stamp(Tool_Profile tool, Vector2 tip)
    {
        float r = tool.Radius;
        var bbox = new Rect(tip.x - r, tip.y, r * 2f, tool.Length + r);
        if (Field.Subtract(p => tool.Section_Sdf(p, tip), bbox))
        {
            Mirror_Axis();
            Dirty = true;
        }
    }

    /// <summary>Revolves the contour into a flat-shaded mesh (appended to verts/tris).</summary>
    public void Build_Mesh(int segments, List<Vector3> verts, List<int> tris)
    {
        segments = Mathf.Max(3, segments);
        if (cos_table == null || cos_table.Length != segments)
        {
            cos_table = new float[segments];
            sin_table = new float[segments];
            for (int j = 0; j < segments; j++)
            {
                float a = j * (2f * Mathf.PI / segments);
                cos_table[j] = Mathf.Cos(a);
                sin_table[j] = Mathf.Sin(a);
            }
        }

        segs.Clear();
        Marching_Squares.Contour(Field, segs);

        const float eps = 1e-9f;
        for (int i = 0; i < segs.Count; i += 2)
        {
            Vector2 a = segs[i], b = segs[i + 1];
            float ra = a.y < eps ? 0f : a.y;
            float rb = b.y < eps ? 0f : b.y;
            if (ra == 0f && rb == 0f) continue;

            for (int j = 0; j < segments; j++)
            {
                int j1 = (j + 1) % segments;
                var A0 = new Vector3(ra * cos_table[j], ra * sin_table[j], a.x);
                var A1 = new Vector3(ra * cos_table[j1], ra * sin_table[j1], a.x);
                var B0 = new Vector3(rb * cos_table[j], rb * sin_table[j], b.x);
                var B1 = new Vector3(rb * cos_table[j1], rb * sin_table[j1], b.x);
                // Segment a->b has solid on its left in (z, r); these windings face outward.
                if (ra > 0f) Add_Tri(A0, B0, A1, verts, tris);
                if (rb > 0f) Add_Tri(A1, B0, B1, verts, tris);
            }
        }
    }

    static void Add_Tri(Vector3 a, Vector3 b, Vector3 c, List<Vector3> verts, List<int> tris)
    {
        int i = verts.Count;
        verts.Add(a); verts.Add(b); verts.Add(c);
        tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
    }
}
