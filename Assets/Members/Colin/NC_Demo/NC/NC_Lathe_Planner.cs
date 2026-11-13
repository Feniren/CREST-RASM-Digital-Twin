using UnityEngine;

/// <summary>
/// NC_Program → NC_Plan for a 2-axis turning centre (Fanuc group-A style, the ProTurn 9000).
/// Plan positions are (x = RADIUS mm, y = 0, z mm): program X is a diameter, U/W are incremental
/// X (diameter) / Z, there is no G90/G91 distance mode. Supported: G00–G03 in the XZ plane (arcs
/// by I/K incremental from the start, I a radius, or by R; broken into chords so NC_Move never
/// samples an XZ arc), G04 dwell (X/U seconds, P milliseconds), G18, G20/G21, G28, G40, G50 S
/// (max rpm), G96/G97 (constant surface speed m/min / direct rpm), G98/G99 (feed per minute /
/// per rev, per rev is the default), T turret words (T0101 = station 1, offset 1; the offset is
/// ignored because tool geometry comes from the tool definition), M00/M01, M02/M30, M03/M04/M05.
/// Arc direction: viewed with Z to the right and X up, G02 is clockwise (the Fanuc convention:
/// G03 for a convex front round, G02 for a shoulder fillet, both cutting toward the chuck). Nose-radius
/// compensation is not applied: the programmed point is the imaginary tool tip (NC_Lathe_Link), so
/// diameters, faces and shoulders are exact and tapers/arcs carry the usual nose-radius error. Everything unsupported
/// (canned cycles G70–G76, G90/G92/G94, …) raises a Warning and produces no motion.
/// </summary>
public static class NC_Lathe_Planner
{
    enum Motion { Rapid, Linear, Cw, Ccw }

    sealed class State
    {
        public Vector3 Pos;                             // x = radius
        public Motion Motion = Motion.Rapid;
        public float Unit = 1f;
        public float Feed;                              // as programmed, already in mm (per min or per rev); 0 = never set
        public bool Per_Rev = true;                     // G99
        public bool Css;                                // G96
        public float Speed;                             // G97 rpm or G96 m/min
        public float Rpm_Clamp;                         // G50 S; 0 = only the machine limit
        public bool Spindle;
        public int Tool;
        public bool Warned_Feed, Warned_Spindle, Warned_Rpm, Warned_No_Rpm;
        public bool Ended;
    }

    struct Block_Words
    {
        public bool Dwell, Home, Clamp, Spindle_On, Spindle_Off, Stop, End, Tool_Change;
    }

    /// <summary>Largest chord sagitta when an arc is broken into lines.</summary>
    public const float Chord_Tolerance_Mm = 0.01f;
    const int Max_Chords = 720;

    public static NC_Plan Plan(NC_Program program) => Plan(program, new NC_Machine_Settings());

    public static NC_Plan Plan(NC_Program program, NC_Machine_Settings settings)
    {
        var plan = new NC_Plan(settings);
        var s = new State();
        foreach (var block in program.Blocks)
        {
            Apply_Block(block, s, plan, Read_Words(block, s, plan));
            if (s.Ended) break;
        }
        foreach (var m in plan.Moves)
        {
            switch (m.Kind)
            {
                case NC_Move_Kind.Rapid: plan.Rapid_Mm += m.Length; plan.Estimated_Seconds += m.Length / (plan.Settings.Rapid_Mm_Min / 60f); break;
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

    /// <summary>Spindle speed at a radius (mm): G97 direct, or G96 surface speed clamped by G50 and the machine.</summary>
    static float Rpm(State s, NC_Plan plan, float radius_mm)
    {
        float limit = plan.Settings.Max_Rpm;
        if (s.Rpm_Clamp > 0f) limit = Mathf.Min(limit, s.Rpm_Clamp);
        if (!s.Css) return Mathf.Min(s.Speed, plan.Settings.Max_Rpm);
        float d = Mathf.Abs(radius_mm) * 2f;
        if (d < 1e-3f) return limit;
        return Mathf.Min(1000f * s.Speed / (Mathf.PI * d), limit);
    }

    static Block_Words Read_Words(NC_Block b, State s, NC_Plan plan)
    {
        var w = new Block_Words();
        foreach (var word in b.Words)
        {
            if (word.Letter == 'G') Read_G(word.Value, b, s, plan, ref w);
            else if (word.Letter == 'M') Read_M(word.Value, b, s, plan, ref w);
        }
        // S and T after the G words, so "G50 S2500" is a clamp whatever the word order.
        if (b.Try_Get('S', out float sv))
        {
            if (w.Clamp) s.Rpm_Clamp = sv;
            else
            {
                s.Speed = sv;
                if (!s.Css && sv > plan.Settings.Max_Rpm && !s.Warned_Rpm)
                {
                    Warn(plan, b, "S" + sv + " is above the machine's " + plan.Settings.Max_Rpm + " rpm; clamped");
                    s.Warned_Rpm = true;
                }
            }
        }
        if (b.Try_Get('T', out float tv))
        {
            int code = Mathf.RoundToInt(tv);
            int station = code >= 100 ? code / 100 : code;
            int offset = code >= 100 ? code % 100 : station;
            if (station <= 0) Info(plan, b, "T" + code + " cancels the offset; no turret move");
            else
            {
                if (offset != station && offset != 0) Info(plan, b, "offset " + offset + " is ignored; tool geometry comes from the tool definition");
                s.Tool = station;
                w.Tool_Change = true;
            }
        }
        return w;
    }

    static void Read_G(float value, NC_Block b, State s, NC_Plan plan, ref Block_Words w)
    {
        if (!Is_Integer(value))
        {
            Warn(plan, b, "unsupported G" + value);
            return;
        }
        int code = (int)value;
        switch (code)
        {
            case 0: s.Motion = Motion.Rapid; break;
            case 1: s.Motion = Motion.Linear; break;
            case 2: s.Motion = Motion.Cw; break;
            case 3: s.Motion = Motion.Ccw; break;
            case 4: w.Dwell = true; break;
            case 18: break;                                                       // XZ plane: the only one
            case 17: case 19: Warn(plan, b, "only the G18 (XZ) plane exists on a lathe"); break;
            case 20: s.Unit = 25.4f; break;
            case 21: s.Unit = 1f; break;
            case 28: w.Home = true; break;
            case 40: break;
            case 41: case 42: Warn(plan, b, "nose-radius compensation is not supported; the path is the tool tip"); break;
            case 50: w.Clamp = true; break;
            case 54: case 55: case 56: case 57: case 58: case 59: Info(plan, b, "G" + code + " work offset is ignored; the work origin is the stock face"); break;
            case 70: case 71: case 72: case 73: case 74: case 75: case 76:
                Warn(plan, b, "G" + code + " turning cycles are not supported; program the passes with G00/G01"); break;
            case 90: case 92: case 94:
                Warn(plan, b, "G" + code + " single-pass cycles are not supported; program the passes with G00/G01"); break;
            case 96: s.Css = true; break;
            case 97: s.Css = false; break;
            case 98: s.Per_Rev = false; break;
            case 99: s.Per_Rev = true; break;
            default: Warn(plan, b, "unsupported G" + code); break;
        }
    }

    static void Read_M(float value, NC_Block b, State s, NC_Plan plan, ref Block_Words w)
    {
        if (!Is_Integer(value))
        {
            Warn(plan, b, "unsupported M" + value);
            return;
        }
        int code = (int)value;
        switch (code)
        {
            case 0: case 1: w.Stop = true; break;
            case 2: case 30: w.End = true; break;
            case 3: case 4: w.Spindle_On = true; break;
            case 5: w.Spindle_Off = true; break;
            case 6: Info(plan, b, "M06 is not needed on a turret; the T word indexes it"); break;
            case 8: case 9: Info(plan, b, "M" + code + " coolant is ignored"); break;
            default: Warn(plan, b, "unsupported M" + code); break;
        }
    }

    // Same order as the mill planner: spindle on → tool change → F → dwell → motion → spindle off → stop → end.
    static void Apply_Block(NC_Block b, State s, NC_Plan plan, Block_Words w)
    {
        if (w.Spindle_On) s.Spindle = true;
        if (w.Tool_Change)
        {
            plan.Moves.Add(New_Move(NC_Move_Kind.Tool_Change, b, s, plan));
            plan.Tools_Used.Add(s.Tool);
        }
        if (b.Try_Get('F', out float f)) s.Feed = f * s.Unit;
        if (w.Dwell)
        {
            var d = New_Move(NC_Move_Kind.Dwell, b, s, plan);
            if (b.Try_Get('X', out float sec) || b.Try_Get('U', out sec)) d.Seconds = Mathf.Max(0f, sec);
            else if (b.Try_Get('P', out float ms)) d.Seconds = Mathf.Max(0f, ms * 0.001f);
            else Warn(plan, b, "G04 without a duration (X/U seconds or P milliseconds)");
            plan.Moves.Add(d);
        }
        else
        {
            if (b.Has('Y')) Warn(plan, b, "a lathe has no Y axis; Y is ignored");
            if (w.Home)
            {
                if (Has_Axis(b)) Emit_Line(b, s, plan, Target(b, s), true);
                plan.Moves.Add(New_Move(NC_Move_Kind.Home, b, s, plan));
            }
            else if (Has_Axis(b))
            {
                Vector3 to = Target(b, s);
                switch (s.Motion)
                {
                    case Motion.Rapid: Emit_Line(b, s, plan, to, true); break;
                    case Motion.Linear: Emit_Line(b, s, plan, to, false); break;
                    case Motion.Cw: Emit_Arc(b, s, plan, to, true); break;
                    case Motion.Ccw: Emit_Arc(b, s, plan, to, false); break;
                }
            }
        }

        if (w.Spindle_Off) s.Spindle = false;
        if (w.Stop)
        {
            var st = New_Move(NC_Move_Kind.Stop, b, s, plan);
            st.Seconds = plan.Settings.Stop_Seconds;
            plan.Moves.Add(st);
        }
        if (w.End)
        {
            plan.Moves.Add(New_Move(NC_Move_Kind.End, b, s, plan));
            s.Ended = true;
        }
    }

    static bool Is_Integer(float v) => Mathf.Abs(v - Mathf.Round(v)) < 1e-4f;
    static bool Has_Axis(NC_Block b) => b.Has('X') || b.Has('Z') || b.Has('U') || b.Has('W');

    static void Info(NC_Plan plan, NC_Block b, string message) => plan.Notices.Add(new NC_Notice(NC_Notice.Level.Info, b.Line, message));
    static void Warn(NC_Plan plan, NC_Block b, string message) => plan.Notices.Add(new NC_Notice(NC_Notice.Level.Warning, b.Line, message));
    static void Alarm(NC_Plan plan, NC_Block b, string message) => plan.Notices.Add(new NC_Notice(NC_Notice.Level.Alarm, b.Line, message));

    static NC_Move New_Move(NC_Move_Kind kind, NC_Block b, State s, NC_Plan plan)
    {
        return new NC_Move
        {
            Kind = kind, Line = b.Line, From = s.Pos, To = s.Pos, Tool = s.Tool,
            Spindle_On = s.Spindle, Spindle_Rpm = s.Spindle ? Rpm(s, plan, s.Pos.x) : 0f
        };
    }

    // X/Z absolute (X a diameter), U/W incremental (U a diameter change).
    static Vector3 Target(NC_Block b, State s)
    {
        Vector3 t = s.Pos;
        if (b.Try_Get('X', out float x)) t.x = x * s.Unit * 0.5f;
        if (b.Try_Get('U', out float u)) t.x += u * s.Unit * 0.5f;
        if (b.Try_Get('Z', out float z)) t.z = z * s.Unit;
        if (b.Try_Get('W', out float w)) t.z += w * s.Unit;
        return t;
    }

    static void Feed_Check(NC_Block b, State s, NC_Plan plan, NC_Move m)
    {
        float rpm = s.Spindle ? Rpm(s, plan, m.From.x) : 0f;
        if (s.Feed <= 0f)
        {
            m.Feed_Mm_Min = plan.Settings.Default_Feed_Mm_Min;
            if (!s.Warned_Feed) Warn(plan, b, "feed move before any F word; using " + plan.Settings.Default_Feed_Mm_Min + " mm/min");
            s.Warned_Feed = true;
        }
        else if (!s.Per_Rev) m.Feed_Mm_Min = s.Feed;
        else if (rpm > 0f) m.Feed_Mm_Min = s.Feed * rpm;
        else
        {
            m.Feed_Mm_Min = plan.Settings.Default_Feed_Mm_Min;
            if (!s.Warned_No_Rpm) Warn(plan, b, "feed per rev (G99) with no spindle speed; using " + plan.Settings.Default_Feed_Mm_Min + " mm/min");
            s.Warned_No_Rpm = true;
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
        var m = New_Move(rapid ? NC_Move_Kind.Rapid : NC_Move_Kind.Linear, b, s, plan);
        m.To = to;
        m.Length = Vector3.Distance(s.Pos, to);
        if (!rapid) Feed_Check(b, s, plan, m);
        if (m.Length > 1e-6f) plan.Moves.Add(m);
        s.Pos = to;
    }

    // The arc is solved in the (z, r) plane — z to the right, r up — then emitted as chords.
    static void Emit_Arc(NC_Block b, State s, NC_Plan plan, Vector3 to, bool clockwise)
    {
        Vector2 from = new Vector2(s.Pos.z, s.Pos.x);
        Vector2 end = new Vector2(to.z, to.x);
        Vector2 centre;
        float radius;

        bool has_i = b.Try_Get('I', out float i);
        bool has_k = b.Try_Get('K', out float k);
        if (has_i || has_k)
        {
            centre = from + new Vector2(k, i) * s.Unit;
            radius = Vector2.Distance(centre, from);
            float r_end = Vector2.Distance(centre, end);
            if (Mathf.Abs(radius - r_end) > plan.Settings.Arc_Tolerance_Mm)
                Alarm(plan, b, "arc radius mismatch: " + radius.ToString("0.###") + " at the start, " + r_end.ToString("0.###") + " at the end");
        }
        else if (b.Try_Get('R', out float r))
        {
            r *= s.Unit;
            Vector2 chord = end - from;
            float len = chord.magnitude;
            if (len < 1e-6f)
            {
                Alarm(plan, b, "an R arc needs distinct end points");
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
            Alarm(plan, b, "arc without I/K or R");
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
        if (sweep < 1e-4f) sweep = 2f * Mathf.PI;

        float step = Chord_Tolerance_Mm >= radius ? Mathf.PI : 2f * Mathf.Acos(1f - Chord_Tolerance_Mm / radius);
        int n = Mathf.Clamp(Mathf.CeilToInt(sweep / step), 1, Max_Chords);
        float dir = clockwise ? -1f : 1f;
        for (int c = 1; c <= n; c++)
        {
            Vector3 p;
            if (c == n) p = to;
            else
            {
                float a = a0 + dir * sweep * c / n;
                p = new Vector3(centre.y + radius * Mathf.Sin(a), 0f, centre.x + radius * Mathf.Cos(a));
            }
            Emit_Line(b, s, plan, p, false);
        }
    }
}
