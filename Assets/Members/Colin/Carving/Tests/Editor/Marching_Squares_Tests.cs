using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class Marching_Squares_Tests
{
    static readonly Vector2 C0 = new Vector2(0f, 0f);
    static readonly Vector2 C1 = new Vector2(1f, 0f);
    static readonly Vector2 C2 = new Vector2(1f, 1f);
    static readonly Vector2 C3 = new Vector2(0f, 1f);

    // Segments per case: single corners and single edges give one, saddles give two.
    static readonly int[] Expected_Segments = { 0, 1, 1, 1, 1, 2, 1, 1, 1, 1, 2, 1, 1, 1, 1, 0 };

    static void Run(float v0, float v1, float v2, float v3, out List<Vector2> segs, out List<Vector2> fill)
    {
        segs = new List<Vector2>();
        fill = new List<Vector2>();
        Marching_Squares.Evaluate(C0, C1, C2, C3, v0, v1, v2, v3, segs, fill);
    }

    static void Case_Values(int c, float solid, float empty, out float v0, out float v1, out float v2, out float v3)
    {
        v0 = (c & 1) != 0 ? solid : empty;
        v1 = (c & 2) != 0 ? solid : empty;
        v2 = (c & 4) != 0 ? solid : empty;
        v3 = (c & 8) != 0 ? solid : empty;
    }

    static float Signed_Area(Vector2 a, Vector2 b, Vector2 c)
    {
        return 0.5f * ((b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y));
    }

    [Test]
    public void Segment_Count_Matches_Table_For_All_Cases()
    {
        for (int c = 0; c < 16; c++)
        {
            Case_Values(c, 1f, -1f, out float v0, out float v1, out float v2, out float v3);
            Run(v0, v1, v2, v3, out var segs, out _);
            Assert.That(segs.Count / 2, Is.EqualTo(Expected_Segments[c]), "case " + c);
        }
    }

    [Test]
    public void Fill_Triangles_Are_Counter_Clockwise()
    {
        for (int c = 0; c < 16; c++)
        {
            foreach (float solid in new[] { 1f, 3f })   // 3 flips the saddle variant
            {
                Case_Values(c, solid, -1f, out float v0, out float v1, out float v2, out float v3);
                Run(v0, v1, v2, v3, out _, out var fill);
                for (int i = 0; i < fill.Count; i += 3)
                    Assert.That(Signed_Area(fill[i], fill[i + 1], fill[i + 2]), Is.GreaterThan(0f), "case " + c + " solid " + solid);
            }
        }
    }

    [Test]
    public void Every_Contour_Segment_Is_A_Directed_Fill_Edge()
    {
        for (int c = 0; c < 16; c++)
        {
            foreach (float solid in new[] { 1f, 3f })
            {
                Case_Values(c, solid, -1f, out float v0, out float v1, out float v2, out float v3);
                Run(v0, v1, v2, v3, out var segs, out var fill);
                for (int s = 0; s < segs.Count; s += 2)
                {
                    bool found = false;
                    for (int i = 0; i < fill.Count && !found; i += 3)
                        for (int e = 0; e < 3; e++)
                            if (fill[i + e] == segs[s] && fill[i + (e + 1) % 3] == segs[s + 1]) { found = true; break; }
                    Assert.That(found, Is.True, "case " + c + " solid " + solid + " segment " + s / 2 + " has solid on the wrong side");
                }
            }
        }
    }

    [Test]
    public void Saddle_Resolves_By_Centre_Average()
    {
        // centre = 0 -> disconnected: two corner triangles
        Run(1f, -1f, 1f, -1f, out var segs, out var fill);
        Assert.That(segs.Count / 2, Is.EqualTo(2));
        Assert.That(fill.Count / 3, Is.EqualTo(2));

        // centre = 1 -> connected hexagon: 4 fan triangles, segments cut off c1 and c3
        Run(3f, -1f, 3f, -1f, out segs, out fill);
        Assert.That(segs.Count / 2, Is.EqualTo(2));
        Assert.That(fill.Count / 3, Is.EqualTo(4));
        Assert.That(segs[0].y, Is.EqualTo(0f).Within(1e-6f), "first segment starts on the bottom edge");
        Assert.That(segs[1].x, Is.EqualTo(1f).Within(1e-6f), "first segment ends on the right edge");

        // same rule for case 10
        Run(-1f, 3f, -1f, 3f, out segs, out fill);
        Assert.That(fill.Count / 3, Is.EqualTo(4));
        Run(-1f, 1f, -1f, 1f, out segs, out fill);
        Assert.That(fill.Count / 3, Is.EqualTo(2));
    }

    [Test]
    public void Crossing_Interpolates_Linearly()
    {
        Run(1f, -3f, -1f, -1f, out var segs, out _);
        Assert.That(segs.Count, Is.EqualTo(2));
        Assert.That(segs[0], Is.EqualTo(new Vector2(0.25f, 0f)));
        Assert.That(segs[1], Is.EqualTo(new Vector2(0f, 0.5f)));
    }

    [Test]
    public void Fill_Area_Matches_Solid_Fraction()
    {
        Assert.That(Area(1f, 1f, 1f, 1f), Is.EqualTo(1f).Within(1e-6f), "full cell");
        Assert.That(Area(1f, 1f, -1f, -1f), Is.EqualTo(0.5f).Within(1e-6f), "bottom half");
        Assert.That(Area(1f, -1f, -1f, -1f), Is.EqualTo(0.125f).Within(1e-6f), "one corner");
        Assert.That(Area(3f, -1f, 3f, -1f), Is.EqualTo(0.9375f).Within(1e-6f), "connected saddle");
    }

    static float Area(float v0, float v1, float v2, float v3)
    {
        Run(v0, v1, v2, v3, out _, out var fill);
        float a = 0f;
        for (int i = 0; i < fill.Count; i += 3) a += Signed_Area(fill[i], fill[i + 1], fill[i + 2]);
        return a;
    }
}
