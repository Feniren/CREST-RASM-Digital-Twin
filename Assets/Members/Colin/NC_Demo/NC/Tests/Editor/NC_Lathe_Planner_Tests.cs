using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class NC_Lathe_Planner_Tests
{
    static NC_Plan Plan(string text) => NC_Lathe_Planner.Plan(NC_Parser.Parse("t", text));

    static List<NC_Move> Motion(NC_Plan plan) => plan.Moves.Where(m => m.Is_Motion).ToList();

    static List<NC_Notice> Warnings(NC_Plan plan) => plan.Notices.Where(n => n.Severity != NC_Notice.Level.Info).ToList();

    static void Near(Vector3 actual, Vector3 expected, float tol = 1e-3f)
    {
        Assert.That(Vector3.Distance(actual, expected), Is.LessThan(tol), "expected " + expected + " got " + actual);
    }

    [Test]
    public void X_Is_A_Diameter_And_Plans_As_Radius()
    {
        var m = Motion(Plan("G00 X50 Z2"));
        Near(m[0].To, new Vector3(25, 0, 2));
    }

    [Test]
    public void U_And_W_Are_Incremental_U_On_Diameter()
    {
        var m = Motion(Plan("G00 X40 Z0\nU-10 W-5\nU4"));
        Near(m[1].To, new Vector3(15, 0, -5));
        Near(m[2].To, new Vector3(17, 0, -5));
    }

    [Test]
    public void Inch_Units_Scale()
    {
        var m = Motion(Plan("G20 G00 X2 Z-1"));
        Near(m[0].To, new Vector3(25.4f, 0, -25.4f));
    }

    [Test]
    public void T_Word_Indexes_The_Turret_Without_M06()
    {
        var plan = Plan("T0303\nT2");
        var changes = plan.Moves.Where(m => m.Kind == NC_Move_Kind.Tool_Change).ToList();
        Assert.That(changes.Select(c => c.Tool), Is.EqualTo(new[] { 3, 2 }));
        Assert.That(plan.Tools_Used, Is.EqualTo(new[] { 2, 3 }));
        Assert.That(plan.Notices, Is.Empty);
    }

    [Test]
    public void Mismatched_Offset_Is_Info_Only()
    {
        var plan = Plan("T0102");
        Assert.That(plan.Notices.Single().Severity, Is.EqualTo(NC_Notice.Level.Info));
        Assert.That(plan.Moves.Single().Tool, Is.EqualTo(1));
    }

    [Test]
    public void Feed_Per_Rev_Is_The_Default_And_Uses_Rpm()
    {
        var m = Motion(Plan("G97 S1000 M03\nG00 X20 Z1\nG01 Z-10 F0.2"));
        Assert.That(m[1].Feed_Mm_Min, Is.EqualTo(200f).Within(1e-3f));
    }

    [Test]
    public void G98_Is_Feed_Per_Minute()
    {
        var m = Motion(Plan("G98 G97 S1000 M03\nG00 X20 Z1\nG01 Z-10 F150"));
        Assert.That(m[1].Feed_Mm_Min, Is.EqualTo(150f).Within(1e-3f));
    }

    [Test]
    public void Feed_Per_Rev_Without_Spindle_Warns_And_Uses_Default()
    {
        var plan = Plan("G00 X20 Z1\nG01 Z-10 F0.2");
        Assert.That(Motion(plan)[1].Feed_Mm_Min, Is.EqualTo(plan.Settings.Default_Feed_Mm_Min));
        Assert.That(Warnings(plan).Count, Is.EqualTo(2), string.Join("; ", plan.Notices));   // no rpm + spindle off
    }

    [Test]
    public void Css_Rpm_Follows_Diameter_And_Is_Clamped_By_G50()
    {
        var plan = Plan("G50 S2000\nG96 S100 M03\nG00 X50 Z1\nG01 Z-5 F0.1\nG00 X10\nG01 Z-10");
        var m = Motion(plan);
        float at_50 = 1000f * 100f / (Mathf.PI * 50f);   // ≈ 636.6 rpm
        Assert.That(m[1].Spindle_Rpm, Is.EqualTo(at_50).Within(0.1f));
        Assert.That(m[1].Feed_Mm_Min, Is.EqualTo(at_50 * 0.1f).Within(0.01f));
        Assert.That(m[3].Spindle_Rpm, Is.EqualTo(2000f).Within(0.1f), "Ø10 would be 3183 rpm; G50 clamps it");
        Assert.That(plan.Notices, Is.Empty);
    }

    [Test]
    public void Css_Is_Clamped_By_The_Machine_Without_G50()
    {
        var m = Motion(Plan("G96 S200 M03\nG00 X4 Z1"));
        Assert.That(m[0].Spindle_Rpm, Is.EqualTo(new NC_Machine_Settings().Max_Rpm));
    }

    [Test]
    public void G02_And_G03_Bulge_To_Opposite_Sides()
    {
        // From (z0, r10) toward the chuck to (z-10, r10), R6, viewed Z right, X up: G02 (clockwise) dips toward
        // the axis, G03 bulges out — the Fanuc convention (G03 convex front round, G02 shoulder fillet).
        var cw = Motion(Plan("M03 G97 S500\nG00 X20 Z0\nG02 X20 Z-10 R6 F0.1"));
        var ccw = Motion(Plan("M03 G97 S500\nG00 X20 Z0\nG03 X20 Z-10 R6 F0.1"));
        Assert.That(cw.Skip(1).Min(m => m.To.x), Is.LessThan(9.5f));
        Assert.That(ccw.Skip(1).Max(m => m.To.x), Is.GreaterThan(10.5f));
        Near(cw.Last().To, new Vector3(10, 0, -10));
        Near(ccw.Last().To, new Vector3(10, 0, -10));
    }

    [Test]
    public void Arc_Chords_Stay_On_The_Circle_Within_Tolerance()
    {
        // Convex front round: (z0, r7) → (z-3, r10), centre (z-3, r7), R3.
        var plan = Plan("M03 G97 S500\nG00 X14 Z0\nG03 X20 Z-3 R3 F0.1");
        var chords = Motion(plan).Skip(1).ToList();
        Assert.That(chords.Count, Is.GreaterThan(4));
        Assert.That(chords.All(c => c.Kind == NC_Move_Kind.Linear && c.Line == chords[0].Line));
        foreach (var c in chords)
        {
            float d = new Vector2(c.To.z + 3f, c.To.x - 7f).magnitude;
            Assert.That(d, Is.EqualTo(3f).Within(1e-3f));
            Vector3 mid = (c.From + c.To) * 0.5f;
            float sag = 3f - new Vector2(mid.z + 3f, mid.x - 7f).magnitude;
            Assert.That(sag, Is.LessThanOrEqualTo(NC_Lathe_Planner.Chord_Tolerance_Mm + 1e-4f));
        }
        Assert.That(plan.Notices, Is.Empty);
    }

    [Test]
    public void IK_Arc_Matches_R_Arc()
    {
        var r = Motion(Plan("M03 G97 S500\nG00 X14 Z0\nG03 X20 Z-3 R3 F0.1"));
        var ik = Motion(Plan("M03 G97 S500\nG00 X14 Z0\nG03 X20 Z-3 I0 K-3 F0.1"));
        Assert.That(ik.Count, Is.EqualTo(r.Count));
        for (int i = 0; i < r.Count; i++) Near(ik[i].To, r[i].To);
    }

    [Test]
    public void IK_Radius_Mismatch_Alarms()
    {
        var plan = Plan("M03 G97 S500\nG00 X14 Z0\nG03 X20 Z-3 I0 K-5 F0.1");
        Assert.That(plan.Has_Alarm, Is.True);
    }

    [Test]
    public void Mill_Planes_And_Canned_Cycles_Warn_Without_Motion()
    {
        var plan = Plan("G17\nG71 U2 R1\nG90 X20 Z-10 F0.2");
        Assert.That(Warnings(plan).Count, Is.EqualTo(3), string.Join("; ", plan.Notices));
    }

    [Test]
    public void Dwell_X_Is_Seconds_P_Is_Milliseconds_And_Does_Not_Move()
    {
        var plan = Plan("G00 X20 Z1\nG04 X0.5\nG04 P1500");
        var dwells = plan.Moves.Where(m => m.Kind == NC_Move_Kind.Dwell).ToList();
        Assert.That(dwells.Select(d => d.Seconds), Is.EqualTo(new[] { 0.5f, 1.5f }).Within(1e-5f));
        Assert.That(Motion(plan).Count, Is.EqualTo(1));
    }

    [Test]
    public void G28_Homes_And_M30_Ends()
    {
        var plan = Plan("G00 X20 Z1\nG28 U0 W0\nM30\nG00 X40");
        Assert.That(plan.Moves.Select(m => m.Kind), Is.EqualTo(new[] { NC_Move_Kind.Rapid, NC_Move_Kind.Home, NC_Move_Kind.End }));
    }

    // The shipped lathe programs cut the Ø32 x 70 NC_Lathe_Stock from its free face; Z stays clear of the chuck.
    static string Shipped_Dir => Path.Combine(Application.dataPath, "StreamingAssets/NC_Lathe");

    [Test]
    public void Shipped_Lathe_Programs_Plan_Cleanly()
    {
        var jobs = NC_Program_Library.Load_All(Shipped_Dir, new NC_Machine_Settings(), NC_Lathe_Planner.Plan);
        Assert.That(jobs.Count, Is.EqualTo(4));
        var categories = new HashSet<string>();
        foreach (var job in jobs)
        {
            var plan = job.Plan;
            Assert.That(job.Program.Notices, Is.Empty, job.Name);
            Assert.That(plan.Notices, Is.Empty, job.Name + " " + string.Join("; ", plan.Notices));
            Assert.That(plan.Cut_Mm, Is.GreaterThan(0f), job.Name);
            Assert.That(plan.Moves[plan.Moves.Count - 1].Kind, Is.EqualTo(NC_Move_Kind.End), job.Name);
            var motion = plan.Moves.Where(m => m.Is_Motion).ToList();
            Assert.That(motion.Min(m => m.To.z), Is.GreaterThanOrEqualTo(-45f), job.Name + ": clear of the chuck");
            Assert.That(motion.Max(m => m.To.x), Is.LessThanOrEqualTo(45f), job.Name + ": within the cross-slide travel");
            categories.Add(job.Program.Category);
        }
        Assert.That(categories.Count, Is.EqualTo(jobs.Count));
    }
}
