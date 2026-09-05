using System.Collections.Generic;
using UnityEngine;

/// <summary>Everything a run needs, precomputed: moves in absolute work mm plus the notices raised while planning.</summary>
public sealed class NC_Plan
{
    public readonly List<NC_Move> Moves = new List<NC_Move>();
    public readonly List<NC_Notice> Notices = new List<NC_Notice>();
    public readonly SortedSet<int> Tools_Used = new SortedSet<int>();
    public float Cut_Mm, Rapid_Mm, Estimated_Seconds;
    public bool Has_Alarm;
}

/// <summary>
/// NC_Program → NC_Plan for the ProMill dialect: modal motion (G00–G03), distance (G90/G91),
/// units (G70/G71, G20/G21), dwell (G04, F = seconds), homing (G28), drilling cycles
/// (G80–G83 with R/Q/P and G98/G99), spindle (M03/M04/M05 + S), tool change (M06 + T),
/// program stop/end (M00/M01, M02/M30). Arcs take I/J (incremental from the start point) or
/// R. Everything unsupported raises a Warning and produces no motion; geometry that cannot be
/// honoured raises an Alarm.
/// </summary>
public static class NC_Planner
{
    public const float Rapid_Mm_Min = 2000f;          // ProMill user guide rapid traverse
    public const float Default_Feed_Mm_Min = 254f;    // the guide's "up to 10 in/min" cutting feed
    public const float Arc_Tolerance_Mm = 0.01f;
    public const float Peck_Clearance_Mm = 1f;

    enum Motion { Rapid, Linear, Cw, Ccw, None, Drill, Drill_Dwell, Peck }

    sealed class State
    {
        public Vector3 Pos;
        public Motion Motion = Motion.Rapid;
        public bool Incremental;
        public float Unit = 1f;
        public float Feed;                              // mm/min; 0 = never programmed
        public bool Spindle;
        public float Rpm;
        public int Tool;
        public float Cycle_R, Cycle_Z, Cycle_Q, Cycle_P, Cycle_Initial;
        public bool Cycle_Return_Initial = true;        // G98
        public bool Warned_Feed, Warned_Spindle, Warned_Peck, Warned_Cycle_Incremental;
        public bool Ended;
    }

    public static NC_Plan Plan(NC_Program program)
    {
        var plan = new NC_Plan();
        var s = new State();
        foreach (var block in program.Blocks)
        {
            Plan_Block(block, s, plan);
            if (s.Ended) break;
        }
        foreach (var m in plan.Moves)
        {
            switch (m.Kind)
            {
                case NC_Move_Kind.Rapid: plan.Rapid_Mm += m.Length; plan.Estimated_Seconds += m.Length / (Rapid_Mm_Min / 60f); break;
                case NC_Move_Kind.Linear:
                case NC_Move_Kind.Arc: plan.Cut_Mm += m.Length; plan.Estimated_Seconds += m.Length / (m.Feed_Mm_Min / 60f); break;
                case NC_Move_Kind.Dwell:
                case NC_Move_Kind.Stop: plan.Estimated_Seconds += m.Seconds; break;
            }
        }
        foreach (var n in plan.Notices)
            if (n.Severity == NC_Notice.Level.Alarm) plan.Has_Alarm = true;
        return plan;
    }

    static void Plan_Block(NC_Block b, State s, NC_Plan plan)
    {
        bool dwell = false, home = false, tool_change = false, spindle_on = false, spindle_off = false, stop = false, end = false;

        foreach (var w in b.Words)
        {
            switch (w.Letter)
            {
                case 'G':
                    if (!Is_Integer(w.Value)) { Warn(plan, b, "unsupported G" + w.Value); break; }
                    switch ((int)w.Value)
                    {
                        case 0: s.Motion = Motion.Rapid; break;
                        case 1: s.Motion = Motion.Linear; break;
                        case 2: s.Motion = Motion.Cw; break;
                        case 3: s.Motion = Motion.Ccw; break;
                        case 4: dwell = true; break;
                        case 17: break;
                        case 18: case 19: Warn(plan, b, "only the G17 (XY) plane is supported"); break;
                        case 20: case 70: s.Unit = 25.4f; break;
                        case 21: case 71: s.Unit = 1f; break;
                        case 28: home = true; break;
                        case 40: case 43: case 49: case 54: case 55: case 56: case 57: case 58: case 59: case 94: break;
                        case 41: case 42: Warn(plan, b, "cutter compensation is not supported; the path is the tool centre"); break;
                        case 80: s.Motion = Motion.None; break;
                        case 81: Enter_Cycle(s, Motion.Drill); break;
                        case 82: Enter_Cycle(s, Motion.Drill_Dwell); break;
                        case 83: Enter_Cycle(s, Motion.Peck); break;
                        case 90: s.Incremental = false; break;
                        case 91: s.Incremental = true; break;
                        case 98: s.Cycle_Return_Initial = true; break;
                        case 99: s.Cycle_Return_Initial = false; break;
                        default: Warn(plan, b, "unsupported G" + (int)w.Value); break;
                    }
                    break;
                case 'M':
                    if (!Is_Integer(w.Value)) { Warn(plan, b, "unsupported M" + w.Value); break; }
                    switch ((int)w.Value)
                    {
                        case 0: case 1: stop = true; break;
                        case 2: case 30: end = true; break;
                        case 3: case 4: spindle_on = true; break;
                        case 5: spindle_off = true; break;
                        case 6: tool_change = true; break;
                        case 8: case 9: break;
                        default: Warn(plan, b, "unsupported M" + (int)w.Value); break;
                    }
                    break;
                case 'S': s.Rpm = w.Value; break;
                case 'T': s.Tool = Mathf.RoundToInt(w.Value); break;
            }
        }

        if (spindle_on) s.Spindle = true;
        if (tool_change)
        {
            if (s.Tool <= 0) Warn(plan, b, "M06 without a tool number");
            else
            {
                var tc = New_Move(NC_Move_Kind.Tool_Change, b, s);
                plan.Moves.Add(tc);
                plan.Tools_Used.Add(s.Tool);
            }
        }
        if (b.Try_Get('F', out float f))
        {
            if (!dwell) s.Feed = f * s.Unit;             // G04's F is seconds, never a feed
        }
        if (dwell)
        {
            var d = New_Move(NC_Move_Kind.Dwell, b, s);
            if (b.Try_Get('F', out float sec) || b.Try_Get('P', out sec)) d.Seconds = Mathf.Max(0f, sec);
            else Warn(plan, b, "G04 without a duration");
            plan.Moves.Add(d);
        }

        if (home)
        {
            if (Has_Axis(b)) Emit_Line(b, s, plan, Target(b, s), true);
            plan.Moves.Add(New_Move(NC_Move_Kind.Home, b, s));
        }
        else if (Is_Cycle(s.Motion)) Expand_Cycle(b, s, plan);
        else if (Has_Axis(b) || (Is_Arc(s.Motion) && (b.Has('I') || b.Has('J'))))
        {
            Vector3 to = Target(b, s);
            switch (s.Motion)
            {
                case Motion.Rapid: Emit_Line(b, s, plan, to, true); break;
                case Motion.Linear: Emit_Line(b, s, plan, to, false); break;
                case Motion.Cw: Emit_Arc(b, s, plan, to, true); break;
                case Motion.Ccw: Emit_Arc(b, s, plan, to, false); break;
                default:
                    Warn(plan, b, "axis words with no motion mode after G80; treated as a rapid");
                    Emit_Line(b, s, plan, to, true);
                    break;
            }
        }

        if (spindle_off) s.Spindle = false;
        if (stop)
        {
            var st = New_Move(NC_Move_Kind.Stop, b, s);
            st.Seconds = 1f;
            plan.Moves.Add(st);
        }
        if (end)
        {
            plan.Moves.Add(New_Move(NC_Move_Kind.End, b, s));
            s.Ended = true;
        }
    }

    static bool Is_Integer(float v) => Mathf.Abs(v - Mathf.Round(v)) < 1e-4f;
    static bool Is_Cycle(Motion m) => m == Motion.Drill || m == Motion.Drill_Dwell || m == Motion.Peck;
    static bool Is_Arc(Motion m) => m == Motion.Cw || m == Motion.Ccw;
    static bool Has_Axis(NC_Block b) => b.Has('X') || b.Has('Y') || b.Has('Z');

    static void Warn(NC_Plan plan, NC_Block b, string message)
    {
        plan.Notices.Add(new NC_Notice(NC_Notice.Level.Warning, b.Line, message));
    }

    static void Alarm(NC_Plan plan, NC_Block b, string message)
    {
        plan.Notices.Add(new NC_Notice(NC_Notice.Level.Alarm, b.Line, message));
    }

    static NC_Move New_Move(NC_Move_Kind kind, NC_Block b, State s)
    {
        return new NC_Move { Kind = kind, Line = b.Line, From = s.Pos, To = s.Pos, Tool = s.Tool, Spindle_On = s.Spindle, Spindle_Rpm = s.Rpm };
    }

    static Vector3 Target(NC_Block b, State s)
    {
        Vector3 t = s.Pos;
        if (b.Try_Get('X', out float x)) t.x = s.Incremental ? t.x + x * s.Unit : x * s.Unit;
        if (b.Try_Get('Y', out float y)) t.y = s.Incremental ? t.y + y * s.Unit : y * s.Unit;
        if (b.Try_Get('Z', out float z)) t.z = s.Incremental ? t.z + z * s.Unit : z * s.Unit;
        return t;
    }

    static void Feed_Check(NC_Block b, State s, NC_Plan plan, NC_Move m)
    {
        if (s.Feed > 0f) m.Feed_Mm_Min = s.Feed;
        else
        {
            m.Feed_Mm_Min = Default_Feed_Mm_Min;
            if (!s.Warned_Feed) Warn(plan, b, "feed move before any F word; using " + Default_Feed_Mm_Min + " mm/min");
            s.Warned_Feed = true;
        }
        if (!s.Spindle && !s.Warned_Spindle)
        {
            Warn(plan, b, "feed move with the spindle off");
            s.Warned_Spindle = true;
        }
        if (s.Tool > 0) plan.Tools_Used.Add(s.Tool);
    }

    static void Emit_Line(NC_Block b, State s, NC_Plan plan, Vector3 to, bool rapid)
    {
        var m = New_Move(rapid ? NC_Move_Kind.Rapid : NC_Move_Kind.Linear, b, s);
        m.To = to;
        m.Length = Vector3.Distance(s.Pos, to);
        if (!rapid) Feed_Check(b, s, plan, m);
        if (m.Length > 1e-6f) plan.Moves.Add(m);
        s.Pos = to;
    }

    static void Emit_Arc(NC_Block b, State s, NC_Plan plan, Vector3 to, bool clockwise)
    {
        Vector2 from = new Vector2(s.Pos.x, s.Pos.y);
        Vector2 end = new Vector2(to.x, to.y);
        Vector2 centre;
        float radius;

        bool has_i = b.Try_Get('I', out float i);
        bool has_j = b.Try_Get('J', out float j);
        if (has_i || has_j)
        {
            centre = from + new Vector2(i, j) * s.Unit;
            radius = Vector2.Distance(centre, from);
            float r_end = Vector2.Distance(centre, end);
            if (Mathf.Abs(radius - r_end) > Arc_Tolerance_Mm)
                Alarm(plan, b, "arc radius mismatch: " + radius.ToString("0.###") + " at the start, " + r_end.ToString("0.###") + " at the end");
        }
        else if (b.Try_Get('R', out float r))
        {
            r *= s.Unit;
            Vector2 chord = end - from;
            float len = chord.magnitude;
            if (len < 1e-6f)
            {
                Alarm(plan, b, "an R arc needs distinct end points; use I/J for a full circle");
                s.Pos = to;
                return;
            }
            float h2 = r * r - len * len * 0.25f;
            if (h2 < 0f)
            {
                Alarm(plan, b, "arc radius " + Mathf.Abs(r).ToString("0.###") + " is smaller than half the chord " + (len * 0.5f).ToString("0.###"));
                h2 = 0f;
            }
            Vector2 left = new Vector2(-chord.y, chord.x) / len;
            bool centre_left = !clockwise;
            if (r < 0f) centre_left = !centre_left;
            centre = (from + end) * 0.5f + left * (centre_left ? Mathf.Sqrt(h2) : -Mathf.Sqrt(h2));
            radius = Mathf.Abs(r);
        }
        else
        {
            Alarm(plan, b, "arc without I/J or R");
            s.Pos = to;
            return;
        }

        if (radius < 1e-6f)
        {
            Alarm(plan, b, "zero-radius arc");
            s.Pos = to;
            return;
        }
        float a0 = Mathf.Atan2(from.y - centre.y, from.x - centre.x);
        float a1 = Mathf.Atan2(end.y - centre.y, end.x - centre.x);
        float sweep = Mathf.Repeat(clockwise ? a0 - a1 : a1 - a0, 2f * Mathf.PI);
        if (sweep < 1e-4f) sweep = 2f * Mathf.PI;   // coincident end points: a full circle

        var m = New_Move(NC_Move_Kind.Arc, b, s);
        m.To = to;
        m.Centre = new Vector3(centre.x, centre.y, 0f);
        m.Clockwise = clockwise;
        m.Radius = radius;
        m.Sweep = sweep;
        float dz = to.z - s.Pos.z;
        m.Length = Mathf.Sqrt(radius * sweep * (radius * sweep) + dz * dz);
        Feed_Check(b, s, plan, m);
        plan.Moves.Add(m);
        s.Pos = to;
    }

    static void Enter_Cycle(State s, Motion cycle)
    {
        if (!Is_Cycle(s.Motion)) s.Cycle_Initial = s.Pos.z;   // the level the cycle was entered at (G98 return)
        s.Motion = cycle;
    }

    // A cycle block carries its parameters (R, Z, Q, P) and drills at every block that names X or Y.
    static void Expand_Cycle(NC_Block b, State s, NC_Plan plan)
    {
        if (s.Incremental && !s.Warned_Cycle_Incremental)
        {
            Warn(plan, b, "G91 inside a drilling cycle is not supported; R and Z are read as absolute");
            s.Warned_Cycle_Incremental = true;
        }
        if (b.Try_Get('R', out float r)) s.Cycle_R = r * s.Unit;
        if (b.Try_Get('Z', out float z)) s.Cycle_Z = z * s.Unit;
        if (b.Try_Get('Q', out float q)) s.Cycle_Q = Mathf.Abs(q) * s.Unit;
        if (b.Try_Get('P', out float p)) s.Cycle_P = p;
        if (!b.Has('X') && !b.Has('Y')) return;

        Vector3 xy = Target(b, s);
        float depth = s.Cycle_Z, plane = s.Cycle_R;
        float back = s.Cycle_Return_Initial ? s.Cycle_Initial : plane;
        if (depth >= plane)
        {
            Alarm(plan, b, "drilling cycle Z " + depth + " is not below R " + plane);
            return;
        }

        Emit_Line(b, s, plan, new Vector3(xy.x, xy.y, s.Pos.z), true);
        Emit_Line(b, s, plan, new Vector3(xy.x, xy.y, plane), true);

        bool peck = s.Motion == Motion.Peck;
        if (peck && s.Cycle_Q <= 0f)
        {
            if (!s.Warned_Peck) Warn(plan, b, "G83 without a positive Q; drilling in one pass");
            s.Warned_Peck = true;
            peck = false;
        }
        if (peck)
        {
            float reached = plane;
            while (reached > depth + 1e-4f)
            {
                float next = Mathf.Max(depth, reached - s.Cycle_Q);
                if (reached < plane) Emit_Line(b, s, plan, new Vector3(xy.x, xy.y, Mathf.Min(plane, reached + Peck_Clearance_Mm)), true);
                Emit_Line(b, s, plan, new Vector3(xy.x, xy.y, next), false);
                Emit_Line(b, s, plan, new Vector3(xy.x, xy.y, plane), true);
                reached = next;
            }
        }
        else
        {
            Emit_Line(b, s, plan, new Vector3(xy.x, xy.y, depth), false);
            if (s.Motion == Motion.Drill_Dwell)
            {
                var d = New_Move(NC_Move_Kind.Dwell, b, s);
                d.Seconds = Mathf.Max(0f, s.Cycle_P);
                plan.Moves.Add(d);
            }
        }
        Emit_Line(b, s, plan, new Vector3(xy.x, xy.y, back), true);
    }
}
