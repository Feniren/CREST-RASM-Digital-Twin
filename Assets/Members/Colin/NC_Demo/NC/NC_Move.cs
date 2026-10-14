using UnityEngine;

public enum NC_Move_Kind { Rapid, Linear, Arc, Dwell, Tool_Change, Stop, Home, End }

/// <summary>
/// One planned motion or event, in absolute work millimetres (program X, Y, Z). Motion kinds
/// have a length and can be sampled; event kinds carry a duration or a tool number.
/// </summary>
public sealed class NC_Move
{
    public NC_Move_Kind Kind;
    public int Line;
    public Vector3 From, To;
    public Vector3 Centre;          // arcs: XY centre, Z ignored
    public bool Clockwise;
    public float Radius, Sweep;     // arcs: radius mm, sweep radians (positive magnitude)
    public float Length;            // mm along the path; 0 for events
    public float Feed_Mm_Min;       // feed moves
    public float Seconds;           // Dwell, Stop
    public int Tool;                // modal T; for Tool_Change the tool to mount
    public bool Spindle_On;
    public float Spindle_Rpm;

    public bool Is_Motion => Kind == NC_Move_Kind.Rapid || Kind == NC_Move_Kind.Linear || Kind == NC_Move_Kind.Arc;

    /// <summary>Position after travelling s mm from From; arcs are sampled by angle with Z interpolated (helical).</summary>
    public Vector3 Sample(float s)
    {
        if (Length <= 0f || s >= Length) return To;
        float t = Mathf.Max(0f, s / Length);
        if (Kind != NC_Move_Kind.Arc) return Vector3.LerpUnclamped(From, To, t);
        float a0 = Mathf.Atan2(From.y - Centre.y, From.x - Centre.x);
        float a = a0 + (Clockwise ? -Sweep : Sweep) * t;
        return new Vector3(Centre.x + Radius * Mathf.Cos(a), Centre.y + Radius * Mathf.Sin(a), Mathf.Lerp(From.z, To.z, t));
    }

    public float Rate_Mm_Min(float rapid_mm_min) => Kind == NC_Move_Kind.Rapid ? rapid_mm_min : Feed_Mm_Min;

    /// <summary>Straight moves only: restart from a different point, e.g. the machine's actual position after a tool change.</summary>
    public void Rebase(Vector3 from)
    {
        From = from;
        Length = Vector3.Distance(From, To);
    }

    /// <summary>A rapid between two points carrying the modal state of a template move.</summary>
    public static NC_Move Rapid_Between(Vector3 from, Vector3 to, NC_Move template)
    {
        return new NC_Move
        {
            Kind = NC_Move_Kind.Rapid, Line = template.Line, From = from, To = to, Length = Vector3.Distance(from, to),
            Tool = template.Tool, Spindle_On = template.Spindle_On, Spindle_Rpm = template.Spindle_Rpm
        };
    }
}
