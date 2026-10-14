using NUnit.Framework;
using UnityEngine;

public class Tool_Profile_Tests
{
    const float R = 0.003f;
    const float L = 0.030f;

    static Tool_Profile Make(Tool_Profile.Tip tip) => new Tool_Profile { Diameter = R * 2f, Length = L, Tip_Shape = tip };

    [Test]
    public void Flat_Is_Constant_Radius_Over_Its_Length()
    {
        var t = Make(Tool_Profile.Tip.Flat);
        Assert.That(t.Radius_At_Height(0f), Is.EqualTo(R).Within(1e-9f));
        Assert.That(t.Radius_At_Height(L), Is.EqualTo(R).Within(1e-9f));
        Assert.That(t.Radius_At_Height(-0.001f), Is.EqualTo(0f));
        Assert.That(t.Radius_At_Height(L + 0.001f), Is.EqualTo(0f));
    }

    [Test]
    public void Ball_Grows_As_A_Sphere_Then_Holds()
    {
        var t = Make(Tool_Profile.Tip.Ball);
        Assert.That(t.Radius_At_Height(0f), Is.EqualTo(0f).Within(1e-9f));
        Assert.That(t.Radius_At_Height(R * 0.5f), Is.EqualTo(R * Mathf.Sqrt(0.75f)).Within(1e-7f));
        Assert.That(t.Radius_At_Height(R), Is.EqualTo(R).Within(1e-9f));
        Assert.That(t.Radius_At_Height(R * 3f), Is.EqualTo(R).Within(1e-9f));
    }

    [Test]
    public void Drill_Opens_At_118_Degrees_Then_Holds()
    {
        var t = Make(Tool_Profile.Tip.Drill);
        float tan59 = Mathf.Tan(59f * Mathf.Deg2Rad);
        Assert.That(t.Radius_At_Height(0f), Is.EqualTo(0f).Within(1e-9f));
        Assert.That(t.Radius_At_Height(0.001f), Is.EqualTo(0.001f * tan59).Within(1e-7f));
        Assert.That(t.Radius_At_Height(R / tan59 + 0.0005f), Is.EqualTo(R).Within(1e-9f));
    }

    [Test]
    public void Section_Sdf_Signs_Follow_The_Tip_Shape()
    {
        var tip = Vector2.zero;
        var flat = Make(Tool_Profile.Tip.Flat);
        Assert.That(flat.Section_Sdf(new Vector2(0f, 0.01f), tip), Is.LessThan(0f), "flat: on the axis inside");
        Assert.That(flat.Section_Sdf(new Vector2(0.004f, 0.01f), tip), Is.GreaterThan(0f), "flat: beside the tool");
        Assert.That(flat.Section_Sdf(new Vector2(0f, -0.001f), tip), Is.GreaterThan(0f), "flat: below the tip");
        Assert.That(flat.Section_Sdf(new Vector2(0.0025f, 0.0005f), tip), Is.LessThan(0f), "flat: sharp corner is inside");

        var ball = Make(Tool_Profile.Tip.Ball);
        Assert.That(ball.Section_Sdf(new Vector2(0f, 0.0005f), tip), Is.LessThan(0f), "ball: just above the tip");
        Assert.That(ball.Section_Sdf(new Vector2(0.0025f, 0.0005f), tip), Is.GreaterThan(0f), "ball: corner is rounded off");
        Assert.That(ball.Section_Sdf(new Vector2(0f, L + 0.001f), tip), Is.GreaterThan(0f), "ball: clipped at length");

        var drill = Make(Tool_Profile.Tip.Drill);
        Assert.That(drill.Section_Sdf(new Vector2(0f, 0.001f), tip), Is.LessThan(0f), "drill: on the axis inside");
        Assert.That(drill.Section_Sdf(new Vector2(0.002f, 0.0005f), tip), Is.GreaterThan(0f), "drill: outside the flank");
        Assert.That(drill.Section_Sdf(new Vector2(0.002f, 0.01f), tip), Is.LessThan(0f), "drill: inside the shank");
    }
}
