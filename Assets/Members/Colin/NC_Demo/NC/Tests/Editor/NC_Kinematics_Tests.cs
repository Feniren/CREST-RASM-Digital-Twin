using NUnit.Framework;
using UnityEngine;

public class NC_Kinematics_Tests
{
    // The demo's work frame: local +X = world -Z (machine X), local +Z = world +X (machine Y), local +Y = up.
    static readonly Quaternion Work = Quaternion.Euler(0f, 90f, 0f);
    static readonly Vector3 Origin = new Vector3(1f, 2f, 3f);

    static void Near(Vector3 actual, Vector3 expected)
    {
        Assert.That(Vector3.Distance(actual, expected), Is.LessThan(1e-5f), "expected " + expected.ToString("F4") + " got " + actual.ToString("F4"));
    }

    [Test]
    public void Work_To_World_Maps_Program_Axes()
    {
        Near(NC_Kinematics.Work_To_World(new Vector3(10, 0, 0), Origin, Work), new Vector3(1f, 2f, 2.99f));
        Near(NC_Kinematics.Work_To_World(new Vector3(0, 10, 0), Origin, Work), new Vector3(1.01f, 2f, 3f));
        Near(NC_Kinematics.Work_To_World(new Vector3(0, 0, -5), Origin, Work), new Vector3(1f, 1.995f, 3f));
    }

    [Test]
    public void World_To_Work_Round_Trips()
    {
        var p = new Vector3(12.5f, -3f, 7f);
        var back = NC_Kinematics.World_To_Work(NC_Kinematics.Work_To_World(p, Origin, Work), Origin, Work);
        Assert.That(Vector3.Distance(back, p), Is.LessThan(1e-3f));
    }

    [Test]
    public void Tip_Error_Drives_The_Right_Axis_With_The_Right_Sign()
    {
        Vector3 err = NC_Kinematics.Tip_Error_World(new Vector3(10, 0, 0), Origin, Origin, Work);
        Near(err, new Vector3(0, 0, -0.01f));
        Assert.That(NC_Kinematics.Drive_Delta(err, Vector3.forward, false), Is.EqualTo(0.01f).Within(1e-6f), "the table carries the stock, so it moves opposite to the error");
        Assert.That(NC_Kinematics.Drive_Delta(err, Vector3.back, false), Is.EqualTo(-0.01f).Within(1e-6f), "a drive whose axis points the other way gets the opposite delta: the same physical motion");

        err = NC_Kinematics.Tip_Error_World(new Vector3(0, 0, -5), Origin, Origin, Work);
        Near(err, new Vector3(0, -0.005f, 0));
        Assert.That(NC_Kinematics.Drive_Delta(err, Vector3.up, true), Is.EqualTo(-0.005f).Within(1e-6f), "the spindle carries the tool, so it moves with the error");

        err = NC_Kinematics.Tip_Error_World(new Vector3(0, 10, 0), Origin, Origin, Work);
        Assert.That(NC_Kinematics.Drive_Delta(err, Vector3.right, false), Is.EqualTo(-0.01f).Within(1e-6f));
    }
}
