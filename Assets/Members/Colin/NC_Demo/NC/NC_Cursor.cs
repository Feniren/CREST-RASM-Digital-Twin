using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Walks a plan by time. Motion moves are advanced at their feed (rapids at the rapid rate)
/// times a feed override, yielding sample points no farther apart than a step so the caller
/// can position and carve at each; event moves stop the walk until acknowledged.
/// </summary>
public sealed class NC_Cursor
{
    public readonly NC_Plan Plan;
    public int Index { get; private set; }
    public float S { get; private set; }                 // mm travelled along the current move
    public Vector3 Position_Mm { get; private set; }

    public NC_Move Current => Index < Plan.Moves.Count ? Plan.Moves[Index] : null;
    public bool Done => Index >= Plan.Moves.Count;
    public bool At_Event => Current != null && !Current.Is_Motion;
    public bool At_Move_Start => S <= 0f;

    public NC_Cursor(NC_Plan plan)
    {
        Plan = plan;
        Position_Mm = plan.Moves.Count > 0 ? plan.Moves[0].From : Vector3.zero;
    }

    public void Advance(float seconds, float feed_override, float max_step_mm, List<Vector3> samples)
    {
        float budget = seconds;
        while (budget > 0f && !Done && !At_Event)
        {
            var m = Current;
            if (m.Length <= 0f) { Next(); continue; }
            float rate = m.Rate_Mm_Min(NC_Planner.Rapid_Mm_Min);
            if (rate <= 0f) rate = NC_Planner.Default_Feed_Mm_Min;
            rate = rate / 60f * feed_override;
            float remaining = m.Length - S;
            float dist = rate * budget;
            if (dist >= remaining)
            {
                dist = remaining;
                budget -= remaining / rate;
            }
            else budget = 0f;

            float target = S + dist;
            while (S < target - 1e-6f)
            {
                S = Mathf.Min(target, S + max_step_mm);
                Position_Mm = m.Sample(S);
                samples.Add(Position_Mm);
            }
            if (S >= m.Length - 1e-6f)
            {
                Position_Mm = m.To;
                Next();
            }
        }
    }

    /// <summary>The caller has handled the current event move.</summary>
    public void Acknowledge()
    {
        if (At_Event) Next();
    }

    /// <summary>Runs a synthesised move (an approach rapid) before the current one.</summary>
    public void Insert_Before_Current(NC_Move move)
    {
        Plan.Moves.Insert(Index, move);
        S = 0f;
    }

    void Next()
    {
        Index++;
        S = 0f;
    }
}
