using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class Lathe_Stock_Data_Tests
{
    // 100 mm long, 20 mm radius, 1 mm cells: 100 x 20 cells.
    static Lathe_Stock_Data Make() => new Lathe_Stock_Data(0.1f, 0.02f, 0.001f);

    static Tool_Profile Tool(float d, float l, Tool_Profile.Tip tip) => new Tool_Profile { Diameter = d, Length = l, Tip_Shape = tip };

    static void Turn(Lathe_Stock_Data s)
    {
        s.Carve(Tool(0.004f, 0.02f, Tool_Profile.Tip.Flat), new Vector2(0.04f, 0.015f), new Vector2(0.08f, 0.015f));
    }

    [Test]
    public void Construction_Mirrors_The_Axis_Row()
    {
        var s = Make();
        Assert.That(s.Nz, Is.EqualTo(100));
        Assert.That(s.Nr, Is.EqualTo(20));
        var f = s.Field;
        Assert.That(f[5, 0], Is.EqualTo(f[5, 1]), "axis padding mirrors the first interior row");
        Assert.That(f[5, 0], Is.GreaterThan(0f));
        Assert.That(f[5, f.Height - 1], Is.LessThan(0f), "outer radius padding is empty");
        Assert.That(f[0, 5], Is.LessThan(0f), "z end padding is empty");
    }

    [Test]
    public void Turning_Pass_Cuts_Only_The_Swept_Range()
    {
        var s = Make();
        var before = (float[])s.Field.V.Clone();
        Turn(s);
        var f = s.Field;
        for (int iz = 1; iz <= f.Nx; iz++)     // field u = z
        {
            for (int ir = 1; ir <= f.Nz; ir++)  // field w = r
            {
                Vector2 c = f.Cell_Centre(iz, ir);   // (z, r)
                bool in_z = c.x > 0.039f && c.x < 0.081f;
                if (in_z && c.y > 0.0155f) Assert.That(f[iz, ir], Is.LessThan(0f), "cut at " + c);
                if (c.x < 0.037f || c.x > 0.083f) Assert.That(f[iz, ir], Is.EqualTo(before[ir * f.Width + iz]), "outside z range at " + c);
                if (c.y < 0.0135f) Assert.That(f[iz, ir], Is.EqualTo(before[ir * f.Width + iz]), "inside the new radius at " + c);
            }
        }
        Assert.That(f[50, 0], Is.EqualTo(f[50, 1]), "axis row still mirrored after a carve");
    }

    [Test]
    public void Revolved_Mesh_Is_Closed()
    {
        var s = Make();
        var verts = new List<Vector3>();
        var tris = new List<int>();

        s.Build_Mesh(16, verts, tris);
        Mesh_Check.Assert_Watertight(verts, tris, "fresh stock");

        Turn(s);
        verts.Clear(); tris.Clear();
        s.Build_Mesh(16, verts, tris);
        Mesh_Check.Assert_Watertight(verts, tris, "turned stock");
    }

    [Test]
    public void Parting_To_Centre_Closes_With_An_Apex()
    {
        var s = Make();
        s.Carve(Tool(0.004f, 0.03f, Tool_Profile.Tip.Flat), new Vector2(0.05f, 0.02f), new Vector2(0.05f, 0f));

        var verts = new List<Vector3>();
        var tris = new List<int>();
        s.Build_Mesh(12, verts, tris);
        Mesh_Check.Assert_Watertight(verts, tris, "parted stock");

        bool apex = false;
        foreach (var v in verts)
            if (Mathf.Abs(v.x) < 1e-9f && Mathf.Abs(v.y) < 1e-9f && v.z > 0.04f && v.z < 0.06f) { apex = true; break; }
        Assert.That(apex, Is.True, "the new faces meet the axis");
    }
}
