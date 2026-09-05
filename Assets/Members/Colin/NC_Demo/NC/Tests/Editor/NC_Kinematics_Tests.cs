using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        Assert.That(NC_Kinematics.Drive_Delta(err, 2, false), Is.EqualTo(0.01f).Within(1e-6f), "the table carries the stock, so it moves opposite to the error");

        err = NC_Kinematics.Tip_Error_World(new Vector3(0, 0, -5), Origin, Origin, Work);
        Near(err, new Vector3(0, -0.005f, 0));
        Assert.That(NC_Kinematics.Drive_Delta(err, 1, true), Is.EqualTo(-0.005f).Within(1e-6f), "the spindle carries the tool, so it moves with the error");

        err = NC_Kinematics.Tip_Error_World(new Vector3(0, 10, 0), Origin, Origin, Work);
        Assert.That(NC_Kinematics.Drive_Delta(err, 0, false), Is.EqualTo(-0.01f).Within(1e-6f));
    }

    [Test]
    public void Shipped_Programs_Plan_Cleanly()
    {
        string dir = Path.Combine(Application.dataPath, "StreamingAssets/NC");
        string[] files = Directory.GetFiles(dir, "*.nc").OrderBy(f => f).ToArray();
        Assert.That(files.Length, Is.GreaterThanOrEqualTo(5));
        var categories = new HashSet<string>();
        foreach (string file in files)
        {
            var program = NC_Parser.Parse(Path.GetFileName(file), File.ReadAllText(file));
            var plan = NC_Planner.Plan(program);
            Assert.That(program.Notices, Is.Empty, file);
            Assert.That(plan.Notices, Is.Empty, file + " " + string.Join("; ", plan.Notices));
            Assert.That(plan.Has_Alarm, Is.False, file);
            Assert.That(plan.Cut_Mm, Is.GreaterThan(0f), file);
            Assert.That(plan.Moves[plan.Moves.Count - 1].Kind, Is.EqualTo(NC_Move_Kind.End), file);
            Assert.That(plan.Moves.Where(m => m.Is_Motion).Min(m => m.To.z), Is.GreaterThanOrEqualTo(-25f), file + ": cuts stay in the top half of the 50 mm stock");
            Assert.That(plan.Moves.Where(m => m.Is_Motion).All(m => m.To.x >= -6f && m.To.x <= 106f && m.To.y >= -6f && m.To.y <= 106f), file + ": stays over the 100 mm stock");
            categories.Add(program.Category);
        }
        Assert.That(categories.Count, Is.EqualTo(files.Length), "every shipped program declares its own category");

        var multi = NC_Planner.Plan(NC_Parser.Parse("05", File.ReadAllText(Path.Combine(dir, "05_multi_tool.nc"))));
        Assert.That(multi.Tools_Used, Is.EqualTo(new[] { 1, 3, 4 }));
        Assert.That(multi.Moves.Count(m => m.Kind == NC_Move_Kind.Tool_Change), Is.EqualTo(3));
    }
}
