using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Executes one NC_Plan on the mill: every frame the cursor yields sample points along the
/// current move (≤ Max_Step_Mm apart), each is applied to the axes and carved. Event moves
/// (dwell, stop, tool change, home, end) are handled here. Any alarm retracts and homes, then
/// reports Done with Alarm_Text set.
/// </summary>
public class NC_Runner : MonoBehaviour
{
    public enum State { Idle, Running, Paused, Dwell, Tool_Change, Homing, Done }

    public NC_Mill_Link Link;
    public NC_Tool_Rack Rack;
    public float Feed_Override = 1f;
    public float Max_Step_Mm = 1f;
    public float Tool_Change_Seconds = 0.5f;

    public State Current_State { get; private set; } = State.Idle;
    public NC_Plan Plan { get; private set; }
    public NC_Cursor Cursor { get; private set; }
    public float Cut_Seconds { get; private set; }
    public string Alarm_Text { get; private set; }
    public bool Spindle_On => Cursor != null && Cursor.Current != null && Cursor.Current.Spindle_On
                              && Current_State != State.Done && Current_State != State.Idle;

    Mill_Stock stock;
    float wait;
    bool tool_change_pending, finishing;
    State paused_from;
    Transform spindle_visual;
    readonly List<Vector3> samples = new List<Vector3>();

    public void Load(NC_Plan plan, Mill_Stock target)
    {
        Plan = plan;
        stock = target;
        Cursor = new NC_Cursor(plan);
        Cut_Seconds = 0f;
        Alarm_Text = null;
        wait = 0f;
        tool_change_pending = false;
        finishing = false;
        Link.Claim();
        Rack.Bind_Stock(stock);
        if (Rack.Active == 0)
        {
            int first = 1;
            foreach (var m in plan.Moves)
                if (m.Is_Motion && m.Tool > 0) { first = m.Tool; break; }
            Debug.Log("NC: no tool in the spindle; mounting T" + first);
            Mount(first);
        }
        else Mount(Rack.Active);
        Current_State = State.Idle;
    }

    public void Unload()
    {
        Plan = null;
        Cursor = null;
        Alarm_Text = null;
        finishing = false;
        tool_change_pending = false;
        Current_State = State.Idle;
    }

    public void Play()
    {
        if (Current_State == State.Idle && Plan != null) Current_State = State.Running;
        else if (Current_State == State.Paused) Current_State = paused_from;
    }

    public void Pause()
    {
        if (Current_State == State.Idle || Current_State == State.Paused || Current_State == State.Done) return;
        paused_from = Current_State;
        Current_State = State.Paused;
    }

    public void Toggle_Pause()
    {
        if (Current_State == State.Paused) Play();
        else Pause();
    }

    /// <summary>Retracts and homes, then reports Done (N key, alarms).</summary>
    public void Abort(string reason)
    {
        if (Plan == null || Current_State == State.Done || finishing) return;
        if (Alarm_Text == null) Alarm_Text = reason;
        Finish();
    }

    void Update()
    {
        if (Cursor == null && Current_State != State.Idle)
        {
            // A script reload during Play keeps serializable fields (this enum) but drops the plan and cursor.
            Debug.LogWarning("NC: run state lost after a script reload; press R to restart.");
            Unload();
        }
        float dt = Time.deltaTime;
        switch (Current_State)
        {
            case State.Running:
                Step_Motion(dt);
                break;
            case State.Dwell:
            case State.Tool_Change:
                wait -= dt;
                if (wait <= 0f)
                {
                    Cursor.Acknowledge();
                    Current_State = State.Running;
                }
                break;
            case State.Homing:
                if (!Link.Home_Step(dt, tool_change_pending)) break;
                if (finishing) Current_State = State.Done;
                else if (tool_change_pending)
                {
                    tool_change_pending = false;
                    Mount(Cursor.Current.Tool);
                    wait = Tool_Change_Seconds;
                    Current_State = State.Tool_Change;
                }
                else
                {
                    Cursor.Acknowledge();
                    Current_State = State.Running;
                }
                break;
        }
        if (spindle_visual != null && Spindle_On) spindle_visual.Rotate(0f, 720f * dt, 0f, Space.Self);
    }

    void Step_Motion(float dt)
    {
        if (Cursor.Done)
        {
            Finish();
            return;
        }
        var m = Cursor.Current;
        if (!m.Is_Motion)
        {
            Begin_Event(m);
            return;
        }
        if (Cursor.At_Move_Start) Anchor(m);

        samples.Clear();
        Cursor.Advance(dt, Feed_Override, Max_Step_Mm, samples);
        var tool = Rack.Active_Tool;
        for (int i = 0; i < samples.Count; i++)
        {
            if (!Link.Set_Work_Position_Mm(samples[i]))
            {
                Raise_Alarm(Link.Alarm_Text);
                return;
            }
            if (tool != null) tool.Carve_Step();
        }
        if (m.Kind != NC_Move_Kind.Rapid) Cut_Seconds += dt;
    }

    // The machine is rarely where the plan thinks after a tool change, G28 or the first block:
    // a rapid simply restarts from the actual position, a feed move gets an approach rapid first.
    void Anchor(NC_Move m)
    {
        Vector3 actual = Link.Work_Position_Mm;
        if ((actual - m.From).sqrMagnitude < 1e-4f) return;
        if (m.Kind == NC_Move_Kind.Rapid) m.Rebase(actual);
        else Cursor.Insert_Before_Current(NC_Move.Rapid_Between(actual, m.From, m));
    }

    void Begin_Event(NC_Move m)
    {
        switch (m.Kind)
        {
            case NC_Move_Kind.Dwell:
            case NC_Move_Kind.Stop:          // M00/M01: the unattended demo pauses one second and continues
                wait = m.Seconds;
                Current_State = State.Dwell;
                break;
            case NC_Move_Kind.Tool_Change:
                if (!Rack.Has(m.Tool)) { Raise_Alarm("no tool T" + m.Tool + " in the rack"); return; }
                if (Rack.Active == m.Tool) { Cursor.Acknowledge(); return; }
                tool_change_pending = true;      // retract to Z home, swap, pause
                Current_State = State.Homing;
                break;
            case NC_Move_Kind.Home:
                Current_State = State.Homing;
                break;
            case NC_Move_Kind.End:
                Cursor.Acknowledge();
                Finish();
                break;
        }
    }

    void Finish()
    {
        finishing = true;
        tool_change_pending = false;
        Current_State = State.Homing;
    }

    void Raise_Alarm(string text)
    {
        Alarm_Text = text;
        Debug.LogWarning("NC: ALARM " + text);
        Finish();
    }

    void Mount(int t)
    {
        var tool = Rack.Mount(t, Link.Tool_Holder, stock);
        Link.Tool_Tip = tool != null ? tool.transform : null;
        spindle_visual = tool != null ? tool.transform.Find("Visual") : null;
    }
}
