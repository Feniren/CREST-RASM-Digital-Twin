using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marching squares over a Scalar_Field_2D (solid where v > 0).
///
/// Canonical cell points: 0..3 = corners c0 (u0,w0), c1 (u1,w0), c2 (u1,w1), c3 (u0,w1);
/// 4..7 = zero crossings on edges c0-c1, c1-c2, c3-c2, c0-c3. Each crossing is interpolated from
/// the same two corners in the same order whichever cell asks, so shared edges yield bitwise
/// identical points and meshes built from adjacent cells are watertight.
///
/// Contour segments run with SOLID ON THE LEFT in the (u right, w up) frame. Fill polygons are
/// counter-clockwise in that frame, so every contour segment is also a directed edge of a fill
/// triangle. Saddles (cases 5 and 10) are resolved by the sign of the cell-centre average,
/// identically for contour and fill.
/// </summary>
public static class Marching_Squares
{
    static readonly int[][] Contour_Table =
    {
        /* 0 */ new int[0],
        /* 1 */ new[] { 4, 7 },
        /* 2 */ new[] { 5, 4 },
        /* 3 */ new[] { 5, 7 },
        /* 4 */ new[] { 6, 5 },
        /* 5 */ new[] { 4, 7, 6, 5 },   // disconnected saddle
        /* 6 */ new[] { 6, 4 },
        /* 7 */ new[] { 6, 7 },
        /* 8 */ new[] { 7, 6 },
        /* 9 */ new[] { 4, 6 },
        /* 10 */ new[] { 5, 4, 7, 6 },  // disconnected saddle
        /* 11 */ new[] { 5, 6 },
        /* 12 */ new[] { 7, 5 },
        /* 13 */ new[] { 4, 5 },
        /* 14 */ new[] { 7, 4 },
        /* 15 */ new int[0],
    };
    static readonly int[] Contour_5_Connected = { 4, 5, 6, 7 };
    static readonly int[] Contour_10_Connected = { 5, 6, 7, 4 };

    static readonly int[][][] Fill_Table =
    {
        /* 0 */ new int[0][],
        /* 1 */ new[] { new[] { 0, 4, 7 } },
        /* 2 */ new[] { new[] { 4, 1, 5 } },
        /* 3 */ new[] { new[] { 0, 1, 5, 7 } },
        /* 4 */ new[] { new[] { 5, 2, 6 } },
        /* 5 */ new[] { new[] { 0, 4, 7 }, new[] { 5, 2, 6 } },
        /* 6 */ new[] { new[] { 4, 1, 2, 6 } },
        /* 7 */ new[] { new[] { 0, 1, 2, 6, 7 } },
        /* 8 */ new[] { new[] { 7, 6, 3 } },
        /* 9 */ new[] { new[] { 0, 4, 6, 3 } },
        /* 10 */ new[] { new[] { 4, 1, 5 }, new[] { 7, 6, 3 } },
        /* 11 */ new[] { new[] { 0, 1, 5, 6, 3 } },
        /* 12 */ new[] { new[] { 7, 5, 2, 3 } },
        /* 13 */ new[] { new[] { 0, 4, 5, 2, 3 } },
        /* 14 */ new[] { new[] { 4, 1, 2, 3, 7 } },
        /* 15 */ new[] { new[] { 0, 1, 2, 3 } },
    };
    static readonly int[][] Fill_5_Connected = { new[] { 0, 4, 5, 2, 6, 7 } };
    static readonly int[][] Fill_10_Connected = { new[] { 4, 1, 5, 6, 3, 7 } };

    static readonly Vector2[] pts = new Vector2[8];

    public static int Case(float v0, float v1, float v2, float v3)
    {
        return (v0 > 0f ? 1 : 0) | (v1 > 0f ? 2 : 0) | (v2 > 0f ? 4 : 0) | (v3 > 0f ? 8 : 0);
    }

    /// <summary>
    /// Evaluates one cell. Appends contour segments as consecutive point pairs and fill
    /// triangles as consecutive CCW triples. Either list may be null.
    /// </summary>
    public static void Evaluate(Vector2 c0, Vector2 c1, Vector2 c2, Vector2 c3,
                                float v0, float v1, float v2, float v3,
                                List<Vector2> segments, List<Vector2> fill)
    {
        int c = Case(v0, v1, v2, v3);
        if (c == 0) return;

        pts[0] = c0; pts[1] = c1; pts[2] = c2; pts[3] = c3;
        pts[4] = (v0 > 0f) != (v1 > 0f) ? Cross(c0, v0, c1, v1) : c0;
        pts[5] = (v1 > 0f) != (v2 > 0f) ? Cross(c1, v1, c2, v2) : c1;
        pts[6] = (v3 > 0f) != (v2 > 0f) ? Cross(c3, v3, c2, v2) : c3;
        pts[7] = (v0 > 0f) != (v3 > 0f) ? Cross(c0, v0, c3, v3) : c0;

        bool centre_solid = (v0 + v1 + v2 + v3) > 0f;

        if (segments != null)
        {
            int[] cont = c == 5 && centre_solid ? Contour_5_Connected
                       : c == 10 && centre_solid ? Contour_10_Connected
                       : Contour_Table[c];
            for (int i = 0; i < cont.Length; i++) segments.Add(pts[cont[i]]);
        }

        if (fill != null)
        {
            int[][] polys = c == 5 && centre_solid ? Fill_5_Connected
                          : c == 10 && centre_solid ? Fill_10_Connected
                          : Fill_Table[c];
            for (int p = 0; p < polys.Length; p++)
            {
                int[] poly = polys[p];
                for (int i = 1; i + 1 < poly.Length; i++)
                {
                    fill.Add(pts[poly[0]]);
                    fill.Add(pts[poly[i]]);
                    fill.Add(pts[poly[i + 1]]);
                }
            }
        }
    }

    /// <summary>Evaluates the marching cell whose corners are the centres of padded cells (ix,iz)..(ix+1,iz+1).</summary>
    public static void Cell(Scalar_Field_2D f, int ix, int iz, List<Vector2> segments, List<Vector2> fill)
    {
        Evaluate(f.Cell_Centre(ix, iz), f.Cell_Centre(ix + 1, iz), f.Cell_Centre(ix + 1, iz + 1), f.Cell_Centre(ix, iz + 1),
                 f[ix, iz], f[ix + 1, iz], f[ix + 1, iz + 1], f[ix, iz + 1],
                 segments, fill);
    }

    public static void Contour(Scalar_Field_2D f, List<Vector2> segments)
    {
        for (int iz = 0; iz < f.Height - 1; iz++)
            for (int ix = 0; ix < f.Width - 1; ix++)
                Cell(f, ix, iz, segments, null);
    }

    public static void Fill(Scalar_Field_2D f, List<Vector2> triangles)
    {
        for (int iz = 0; iz < f.Height - 1; iz++)
            for (int ix = 0; ix < f.Width - 1; ix++)
                Cell(f, ix, iz, null, triangles);
    }

    static Vector2 Cross(Vector2 a, float va, Vector2 b, float vb)
    {
        return a + (b - a) * (va / (va - vb));
    }
}
