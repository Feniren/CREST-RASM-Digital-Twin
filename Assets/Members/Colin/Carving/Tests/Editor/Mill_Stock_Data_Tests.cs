using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class Mill_Stock_Data_Tests
{
    // 20 x 10 x 20 mm at 1 mm cells and 1 mm slabs: 20x20 cells, 10 slabs.
    static Mill_Stock_Data Make() => new Mill_Stock_Data(new Vector3(0.02f, 0.01f, 0.02f), 0.001f, 0.001f);

    static Tool_Profile Tool(float d, float l, Tool_Profile.Tip tip) => new Tool_Profile { Diameter = d, Length = l, Tip_Shape = tip };

    static void Plunge(Mill_Stock_Data s)
    {
        s.Carve(Tool(0.003f, 0.03f, Tool_Profile.Tip.Flat), new Vector3(0.01f, 0.02f, 0.01f), new Vector3(0.01f, 0.005f, 0.01f));
    }

    static void Ball_Slot(Mill_Stock_Data s)
    {
        s.Carve(Tool(0.006f, 0.03f, Tool_Profile.Tip.Ball), new Vector3(0.005f, 0.007f, 0.01f), new Vector3(0.015f, 0.007f, 0.01f));
    }

    static int Empty_Cells(Scalar_Field_2D f)
    {
        int n = 0;
        for (int iz = 1; iz <= f.Nz; iz++)
            for (int ix = 1; ix <= f.Nx; ix++)
                if (f[ix, iz] <= 0f) n++;
        return n;
    }

    static int Contour_Segments(Scalar_Field_2D f)
    {
        var segs = new List<Vector2>();
        Marching_Squares.Contour(f, segs);
        return segs.Count / 2;
    }

    static int Triangles(Mill_Stock_Data s, int k, bool cull)
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();
        s.Build_Slab_Mesh(k, verts, tris, cull);
        return tris.Count / 3;
    }

    [Test]
    public void Construction_Rounds_To_Whole_Cells()
    {
        var s = Make();
        Assert.That(s.Nx, Is.EqualTo(20));
        Assert.That(s.Nz, Is.EqualTo(20));
        Assert.That(s.Slab_Count, Is.EqualTo(10));
        Assert.That(s.Slab_Top(9), Is.EqualTo(0.01f).Within(1e-7f));
        for (int k = 0; k < s.Slab_Count; k++) Assert.That(s.Is_Dirty(k), Is.True);
    }

    [Test]
    public void Plunge_Empties_The_Column_Only_Where_The_Tool_Went()
    {
        var s = Make();
        Plunge(s);
        Assert.That(s.Last_Sub_Steps, Is.InRange(30, 31), "15 mm of travel at half-cell sub-steps");

        for (int k = 0; k < s.Slab_Count; k++)
        {
            bool cut = s.Slab_Centre(k) >= 0.005f;
            var f = s.Slabs[k];
            // padded cell (10,10) is centred 0.7 mm from the tool axis; (3,3) is far away
            if (cut) Assert.That(f[10, 10], Is.LessThan(0f), "under the tool, slab " + k);
            else Assert.That(f[10, 10], Is.GreaterThan(0f), "below the tool tip, slab " + k);
            Assert.That(f[3, 3], Is.GreaterThan(0f), "far cell, slab " + k);
        }
    }

    [Test]
    public void Ball_Slot_Widens_With_Height()
    {
        var s = Make();
        Ball_Slot(s);
        Assert.That(Empty_Cells(s.Slabs[6]), Is.EqualTo(0), "slab below the tip is untouched");
        int lower = Empty_Cells(s.Slabs[7]);
        int upper = Empty_Cells(s.Slabs[9]);
        Assert.That(lower, Is.GreaterThan(0));
        Assert.That(upper, Is.GreaterThan(lower), "the ball's radius grows with height");
    }

    [Test]
    public void Slab_Meshes_Are_Closed_Without_Culling()
    {
        var s = Make();
        var verts = new List<Vector3>();
        var tris = new List<int>();
        for (int k = 0; k < s.Slab_Count; k++)
        {
            verts.Clear(); tris.Clear();
            s.Build_Slab_Mesh(k, verts, tris, false);
            Mesh_Check.Assert_Watertight(verts, tris, "fresh slab " + k);
        }

        Plunge(s);
        Ball_Slot(s);
        for (int k = 0; k < s.Slab_Count; k++)
        {
            verts.Clear(); tris.Clear();
            s.Build_Slab_Mesh(k, verts, tris, false);
            Mesh_Check.Assert_Watertight(verts, tris, "carved slab " + k);
        }
    }

    [Test]
    public void Culling_Hides_Covered_Caps_Only()
    {
        var s = Make();

        int walls5 = 2 * Contour_Segments(s.Slabs[5]);
        Assert.That(Triangles(s, 5, true), Is.EqualTo(walls5), "interior slab keeps walls only");

        int walls9 = 2 * Contour_Segments(s.Slabs[9]);
        int top_all = Triangles(s, 9, false);
        Assert.That(Triangles(s, 9, true), Is.EqualTo(walls9 + (top_all - walls9) / 2), "top slab keeps its top cap, drops its bottom cap");

        int walls0 = 2 * Contour_Segments(s.Slabs[0]);
        int bottom_all = Triangles(s, 0, false);
        Assert.That(Triangles(s, 0, true), Is.EqualTo(walls0 + (bottom_all - walls0) / 2), "bottom slab keeps its bottom cap");

        Plunge(s);   // hole through slabs 5..9; slab 4 now shows a floor
        Assert.That(Triangles(s, 4, true), Is.GreaterThan(2 * Contour_Segments(s.Slabs[4])), "hole floor is emitted on the slab below");
    }

    [Test]
    public void Dirty_Marks_The_Stamped_Slab_And_Its_Neighbours()
    {
        var s = Make();
        s.Clear_Dirty();
        var tip = new Vector3(0.01f, 0.0074f, 0.01f);
        s.Carve(Tool(0.003f, 0.0005f, Tool_Profile.Tip.Flat), tip, tip);   // slab 7 only (centre 7.5 mm, h = 0.1 mm; slab 8 is 1.1 mm up)
        for (int k = 0; k < s.Slab_Count; k++)
            Assert.That(s.Is_Dirty(k), Is.EqualTo(k >= 6 && k <= 8), "slab " + k);

        s.Clear_Dirty();
        s.Carve(Tool(0.003f, 0.0005f, Tool_Profile.Tip.Flat), tip, tip);
        for (int k = 0; k < s.Slab_Count; k++)
            Assert.That(s.Is_Dirty(k), Is.False, "re-stamping changes nothing, slab " + k);
    }
}
