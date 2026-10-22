using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class NC_Cursor_Tests
{
    static NC_Move Linear(Vector3 from, Vector3 to, float feed)
    {
        return new NC_Move { Kind = NC_Move_Kind.Linear, From = from, To = to, Length = Vector3.Distance(from, to), Feed_Mm_Min = feed, Spindle_On = true };
    }

    static NC_Plan Plan(params NC_Move[] moves)
    {
        var plan = new NC_Plan();
        plan.Moves.AddRange(moves);
        return plan;
    }

    [Test]
    public void Advance_Samples_At_Most_Step_Apart()
    {
        var c = new NC_Cursor(Plan(Linear(Vector3.zero, new Vector3(10, 0, 0), 600f)));   // 10 mm/s
        var samples = new List<Vector3>();
        c.Advance(0.25f, 1f, 0.5f, samples);
        Assert.That(c.S, Is.EqualTo(2.5f).Within(1e-4f));
        Assert.That(samples.Count, Is.EqualTo(5));
        for (int i = 1; i < samples.Count; i++)
            Assert.That(Vector3.Distance(samples[i - 1], samples[i]), Is.LessThanOrEqualTo(0.5f + 1e-4f));
        Assert.That(samples[4].x, Is.EqualTo(2.5f).Within(1e-4f));
        Assert.That(c.Done, Is.False);
    }

    [Test]
    public void Override_Completes_And_Rolls_Into_The_Next_Move()
    {
        var c = new NC_Cursor(Plan(Linear(Vector3.zero, new Vector3(10, 0, 0), 600f), Linear(new Vector3(10, 0, 0), new Vector3(10, 10, 0), 600f)));
        var samples = new List<Vector3>();
        c.Advance(0.25f, 10f, 1f, samples);   // 25 mm of travel over a 20 mm plan
        Assert.That(c.Done, Is.True);
        Assert.That(samples[samples.Count - 1], Is.EqualTo(new Vector3(10, 10, 0)));
        Assert.That(samples.Count, Is.EqualTo(20));
    }

    [Test]
    public void Stops_At_Events_Until_Acknowledged()
    {
        var dwell = new NC_Move { Kind = NC_Move_Kind.Dwell, Seconds = 1f };
        var c = new NC_Cursor(Plan(Linear(Vector3.zero, new Vector3(10, 0, 0), 600f), dwell, Linear(new Vector3(10, 0, 0), new Vector3(20, 0, 0), 600f)));
        var samples = new List<Vector3>();
        c.Advance(10f, 1f, 1f, samples);
        Assert.That(c.Index, Is.EqualTo(1));
        Assert.That(c.At_Event, Is.True);
        int n = samples.Count;
        c.Advance(10f, 1f, 1f, samples);
        Assert.That(samples.Count, Is.EqualTo(n), "nothing moves while an event is pending");
        c.Acknowledge();
        Assert.That(c.Index, Is.EqualTo(2));
        Assert.That(c.At_Move_Start, Is.True);
        c.Advance(10f, 1f, 1f, samples);
        Assert.That(c.Done, Is.True);
        Assert.That(c.Position_Mm, Is.EqualTo(new Vector3(20, 0, 0)));
    }

    [Test]
    public void Inserted_Approach_Runs_Before_The_Current_Move()
    {
        var cut = Linear(new Vector3(5, 0, 0), new Vector3(10, 0, 0), 600f);
        var c = new NC_Cursor(Plan(cut));
        c.Insert_Before_Current(NC_Move.Rapid_Between(Vector3.zero, cut.From, cut));
        var samples = new List<Vector3>();
        c.Advance(10f, 1f, 1f, samples);
        Assert.That(c.Plan.Moves.Count, Is.EqualTo(2));
        Assert.That(samples[0].x, Is.LessThanOrEqualTo(1f + 1e-4f));
        Assert.That(samples[samples.Count - 1].x, Is.EqualTo(10f).Within(1e-4f));
        Assert.That(c.Done, Is.True);
    }

    [Test]
    public void Zero_Length_Moves_Are_Skipped()
    {
        var c = new NC_Cursor(Plan(Linear(Vector3.zero, Vector3.zero, 600f), Linear(Vector3.zero, new Vector3(1, 0, 0), 600f)));
        var samples = new List<Vector3>();
        c.Advance(1f, 1f, 1f, samples);
        Assert.That(c.Done, Is.True);
        Assert.That(samples.Count, Is.EqualTo(1));
    }
}
