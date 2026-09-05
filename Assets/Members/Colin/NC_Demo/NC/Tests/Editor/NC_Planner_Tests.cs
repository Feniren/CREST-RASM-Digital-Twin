using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class NC_Planner_Tests
{
    static NC_Plan Plan(string text) => NC_Planner.Plan(NC_Parser.Parse("t", text));

    static List<NC_Move> Motion(NC_Plan plan) => plan.Moves.Where(m => m.Is_Motion).ToList();

    static void Near(Vector3 actual, Vector3 expected, float tol = 1e-3f)
    {
        Assert.That(Vector3.Distance(actual, expected), Is.LessThan(tol), "expected " + expected + " got " + actual);
    }

    [Test]
    public void Modal_Motion_Continues_With_Axis_Only_Blocks()
    {
        var plan = Plan("M03\nG01 X10 F100\nY10");
        var m = Motion(plan);
        Assert.That(m.Count, Is.EqualTo(2));
        Assert.That(m[1].Kind, Is.EqualTo(NC_Move_Kind.Linear));
        Near(m[1].From, new Vector3(10, 0, 0));
        Near(m[1].To, new Vector3(10, 10, 0));
        Assert.That(m[1].Feed_Mm_Min, Is.EqualTo(100f));
        Assert.That(plan.Notices, Is.Empty);
    }

    [Test]
    public void Incremental_Mode_Accumulates()
    {
        var m = Motion(Plan("G91 G00 X5\nX5\nG90 X20"));
        Near(m[1].To, new Vector3(10, 0, 0));
        Near(m[2].To, new Vector3(20, 0, 0));
    }

    [Test]
    public void Inch_Units_Scale_Coordinates_And_Feed()
    {
        var m = Motion(Plan("G70 M03\nG01 X1 F10"));
        Near(m[0].To, new Vector3(25.4f, 0, 0));
        Assert.That(m[0].Feed_Mm_Min, Is.EqualTo(254f).Within(1e-3f));
    }

    [Test]
    public void Dwell_Seconds_Do_Not_Change_The_Feed()
    {
        var plan = Plan("M03\nG01 X10 F100\nG04 F2\nX20");
        Assert.That(plan.Moves.Select(x => x.Kind), Is.EqualTo(new[] { NC_Move_Kind.Linear, NC_Move_Kind.Dwell, NC_Move_Kind.Linear }));
        Assert.That(plan.Moves[1].Seconds, Is.EqualTo(2f));
        Assert.That(plan.Moves[2].Feed_Mm_Min, Is.EqualTo(100f));
    }

    [Test]
    public void Rapid_Has_Length_And_No_Feed()
    {
        var m = Motion(Plan("G00 X3 Y4"));
        Assert.That(m[0].Kind, Is.EqualTo(NC_Move_Kind.Rapid));
        Assert.That(m[0].Length, Is.EqualTo(5f).Within(1e-4f));
        Assert.That(m[0].Feed_Mm_Min, Is.EqualTo(0f));
        Assert.That(m[0].Rate_Mm_Min(2000f), Is.EqualTo(2000f));
    }

    [Test]
    public void Arc_With_IJ_Centre()
    {
        var m = Motion(Plan("M03 F100\nG02 X10 Y10 I10 J0"))[0];
        Assert.That(m.Kind, Is.EqualTo(NC_Move_Kind.Arc));
        Assert.That(m.Clockwise, Is.True);
        Near(m.Centre, new Vector3(10, 0, 0));
        Assert.That(m.Radius, Is.EqualTo(10f).Within(1e-4f));
        Assert.That(m.Sweep, Is.EqualTo(Mathf.PI / 2f).Within(1e-4f));
        Assert.That(m.Length, Is.EqualTo(5f * Mathf.PI).Within(1e-3f));
        Near(m.Sample(m.Length * 0.5f), new Vector3(2.929f, 7.071f, 0));
        Near(m.Sample(m.Length), new Vector3(10, 10, 0));
    }

    [Test]
    public void Coincident_End_Points_Make_A_Full_Circle()
    {
        var m = Motion(Plan("M03 F100\nG00 X5\nG03 I-5"))[1];
        Assert.That(m.Kind, Is.EqualTo(NC_Move_Kind.Arc));
        Near(m.Centre, Vector3.zero);
        Assert.That(m.Sweep, Is.EqualTo(2f * Mathf.PI).Within(1e-4f));
        Assert.That(m.Length, Is.EqualTo(10f * Mathf.PI).Within(1e-3f));
        Near(m.Sample(m.Length * 0.5f), new Vector3(-5, 0, 0));
    }

    [Test]
    public void Arc_Radius_Mismatch_Is_An_Alarm()
    {
        var plan = Plan("M03 F100\nG02 X10 Y10 I10 J1");
        Assert.That(plan.Has_Alarm, Is.True);
        Assert.That(plan.Notices.Any(n => n.Message.Contains("radius")));
    }

    [Test]
    public void R_Arc_Picks_The_Minor_Side_Unless_Negative()
    {
        var minor = Motion(Plan("M03 F100\nG03 X10 Y0 R10"))[0];
        Near(minor.Centre, new Vector3(5, 8.660f, 0));
        Assert.That(minor.Sweep, Is.EqualTo(Mathf.PI / 3f).Within(1e-3f));
        Near(minor.Sample(minor.Length * 0.5f), new Vector3(5, -1.340f, 0));

        var major = Motion(Plan("M03 F100\nG03 X10 Y0 R-10"))[0];
        Near(major.Centre, new Vector3(5, -8.660f, 0));
        Assert.That(major.Sweep, Is.EqualTo(5f * Mathf.PI / 3f).Within(1e-3f));
    }

    [Test]
    public void Helical_Arc_Interpolates_Z()
    {
        var m = Motion(Plan("M03 F100\nG02 X10 Y10 Z-4 I10 J0"))[0];
        Assert.That(m.Sample(m.Length * 0.5f).z, Is.EqualTo(-2f).Within(1e-3f));
        Assert.That(m.Length, Is.GreaterThan(5f * Mathf.PI));
    }

    [Test]
    public void Drill_Cycle_G99_Returns_To_R_And_G00_Cancels()
    {
        var plan = Plan("M03 F100\nG00 Z10\nG99 G81 X5 Y5 Z-5 R2\nX15\nG80\nG00 X0");
        var m = Motion(plan);
        var linear = m.Where(x => x.Kind == NC_Move_Kind.Linear).ToList();
        Assert.That(linear.Count, Is.EqualTo(2));
        Near(linear[0].From, new Vector3(5, 5, 2));
        Near(linear[0].To, new Vector3(5, 5, -5));
        Near(linear[1].To, new Vector3(15, 5, -5));
        Near(m[m.Count - 2].To, new Vector3(15, 5, 2), 1e-4f);
        Near(m[m.Count - 1].To, new Vector3(0, 5, 2), 1e-4f);
        Assert.That(m[m.Count - 1].Kind, Is.EqualTo(NC_Move_Kind.Rapid));
    }

    [Test]
    public void Drill_Cycle_G98_Returns_To_The_Initial_Level()
    {
        var m = Motion(Plan("M03 F100\nG00 Z10\nG98 G81 X5 Y5 Z-5 R2"));
        Near(m[m.Count - 1].To, new Vector3(5, 5, 10));
    }

    [Test]
    public void Peck_Cycle_Steps_By_Q_And_Retracts_To_R()
    {
        var m = Motion(Plan("M03 F100\nG00 Z10\nG99 G83 X5 Y5 Z-5 R2 Q2"));
        var depths = m.Where(x => x.Kind == NC_Move_Kind.Linear).Select(x => x.To.z).ToList();
        Assert.That(depths, Is.EqualTo(new[] { 0f, -2f, -4f, -5f }).Within(1e-4f));
        Assert.That(m.Min(x => x.To.z), Is.EqualTo(-5f).Within(1e-4f));
        var after_first = m[m.IndexOf(m.First(x => x.Kind == NC_Move_Kind.Linear)) + 1];
        Assert.That(after_first.Kind, Is.EqualTo(NC_Move_Kind.Rapid));
        Assert.That(after_first.To.z, Is.EqualTo(2f).Within(1e-4f));
        Assert.That(m[m.Count - 1].To.z, Is.EqualTo(2f).Within(1e-4f));
    }

    [Test]
    public void Peck_Without_Q_Warns_And_Drills_In_One_Pass()
    {
        var plan = Plan("M03 F100\nG00 Z10\nG83 X5 Y5 Z-5 R2");
        Assert.That(Motion(plan).Count(x => x.Kind == NC_Move_Kind.Linear), Is.EqualTo(1));
        Assert.That(plan.Notices.Any(n => n.Message.Contains("Q")));
    }

    [Test]
    public void Cycle_Parameter_Block_Without_XY_Does_Not_Drill()
    {
        var plan = Plan("M03 F100\nG00 Z10\nG81 Z-5 R2\nX5");
        Assert.That(Motion(plan).Count(x => x.Kind == NC_Move_Kind.Linear), Is.EqualTo(1));
    }

    [Test]
    public void Tool_Change_Carries_The_Tool()
    {
        var plan = Plan("T2 M06\nM03\nG01 X10 F100");
        Assert.That(plan.Moves[0].Kind, Is.EqualTo(NC_Move_Kind.Tool_Change));
        Assert.That(plan.Moves[0].Tool, Is.EqualTo(2));
        Assert.That(plan.Moves[1].Tool, Is.EqualTo(2));
        Assert.That(plan.Tools_Used, Is.EqualTo(new[] { 2 }));
    }

    [Test]
    public void Spindle_State_Rides_On_Moves()
    {
        var plan = Plan("M03 S1200\nG01 X10 F100\nM05\nG01 X20");
        var m = Motion(plan);
        Assert.That(m[0].Spindle_On, Is.True);
        Assert.That(m[0].Spindle_Rpm, Is.EqualTo(1200f));
        Assert.That(m[1].Spindle_On, Is.False);
        Assert.That(plan.Notices.Any(n => n.Message.Contains("spindle off")));
    }

    [Test]
    public void Feed_Without_F_Uses_The_Default_With_A_Warning()
    {
        var plan = Plan("M03\nG01 X10");
        Assert.That(Motion(plan)[0].Feed_Mm_Min, Is.EqualTo(NC_Planner.Default_Feed_Mm_Min));
        Assert.That(plan.Notices.Any(n => n.Message.Contains("F word")));
    }

    [Test]
    public void End_Stops_Planning()
    {
        var plan = Plan("M03 F100\nG01 X10\nM30\nG01 X20");
        Assert.That(plan.Moves[plan.Moves.Count - 1].Kind, Is.EqualTo(NC_Move_Kind.End));
        Assert.That(Motion(plan).Count, Is.EqualTo(1));
    }

    [Test]
    public void Stop_And_Home_Are_Events()
    {
        var plan = Plan("M00\nG28");
        Assert.That(plan.Moves.Select(x => x.Kind), Is.EqualTo(new[] { NC_Move_Kind.Stop, NC_Move_Kind.Home }));
    }

    [Test]
    public void Unknown_Codes_Warn_Without_Motion()
    {
        var plan = Plan("G99.5\nM77\nG41 D1");
        Assert.That(plan.Moves, Is.Empty);
        Assert.That(plan.Notices.Count, Is.EqualTo(3));
        Assert.That(plan.Has_Alarm, Is.False);
    }

    [Test]
    public void Estimate_Sums_Cut_And_Rapid_Time()
    {
        var plan = Plan("M03\nG00 X100\nG01 X200 F600");
        Assert.That(plan.Rapid_Mm, Is.EqualTo(100f).Within(1e-3f));
        Assert.That(plan.Cut_Mm, Is.EqualTo(100f).Within(1e-3f));
        Assert.That(plan.Estimated_Seconds, Is.EqualTo(13f).Within(1e-2f));
    }
}
