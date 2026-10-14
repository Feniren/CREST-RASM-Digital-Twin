using NUnit.Framework;
using UnityEngine;

public class Scalar_Field_2D_Tests
{
    [Test]
    public void Fresh_Rect_Has_Solid_Interior_And_Empty_Padding()
    {
        var f = new Scalar_Field_2D(4, 3, 1f, Vector2.zero);
        f.Fill_Solid_Rect();

        Assert.That(f[1, 1], Is.EqualTo(0.5f).Within(1e-6f), "edge interior cell");
        Assert.That(f[2, 2], Is.EqualTo(1f).Within(1e-6f), "deep interior cell clamps to +cell");
        Assert.That(f[0, 1], Is.EqualTo(-0.5f).Within(1e-6f), "padding beside an edge cell");
        Assert.That(f[0, 0], Is.LessThan(0f), "corner padding");
        Assert.That(f[5, 4], Is.LessThan(0f), "far corner padding");
        Assert.That(f.Cell_Centre(1, 1), Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(f.Cell_Centre(0, 0), Is.EqualTo(new Vector2(-0.5f, -0.5f)));
    }

    [Test]
    public void Subtract_Disc_Empties_Cells_Within_Radius_Only()
    {
        var f = new Scalar_Field_2D(10, 10, 1f, Vector2.zero);
        f.Fill_Solid_Rect();
        var before = (float[])f.V.Clone();

        var centre = new Vector2(5f, 5f);
        Assert.That(f.Subtract_Disc(centre, 2.5f), Is.True);

        for (int iz = 1; iz <= 10; iz++)
        {
            for (int ix = 1; ix <= 10; ix++)
            {
                float dist = (f.Cell_Centre(ix, iz) - centre).magnitude;
                if (dist < 2.5f - 1e-6f)
                    Assert.That(f[ix, iz], Is.LessThan(0f), "inside at " + ix + "," + iz);
                else if (dist > 3.5f)
                    Assert.That(f[ix, iz], Is.EqualTo(before[iz * f.Width + ix]), "untouched at " + ix + "," + iz);
            }
        }

        Assert.That(f.Subtract_Disc(centre, 2.5f), Is.False, "re-stamping the same disc changes nothing");
    }

    [Test]
    public void Padding_Is_Never_Modified()
    {
        var f = new Scalar_Field_2D(6, 6, 1f, Vector2.zero);
        f.Fill_Solid_Rect();
        var before = (float[])f.V.Clone();

        f.Subtract_Disc(new Vector2(0f, 0f), 3f);   // overlaps the corner and the padding

        Assert.That(f[1, 1], Is.LessThan(0f));
        for (int iz = 0; iz < f.Height; iz++)
            for (int ix = 0; ix < f.Width; ix++)
                if (ix == 0 || iz == 0 || ix == f.Width - 1 || iz == f.Height - 1)
                    Assert.That(f[ix, iz], Is.EqualTo(before[iz * f.Width + ix]), "padding at " + ix + "," + iz);
    }
}
