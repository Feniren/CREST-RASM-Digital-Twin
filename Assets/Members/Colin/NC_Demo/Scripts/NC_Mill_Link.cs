using ProMill8000;
using UnityEngine;

/// <summary>
/// The demo's view of the mill: the three AxisMovement drives keyed by the WORLD axis each one
/// moves along, the holder the active tool hangs from, and the work origin riding on the stock.
/// The tip is positioned in closed loop each step (NC_Kinematics), so tool length and stock
/// placement need no captured constants; a MoveBy that AxisMovement clamped shows up as a
/// residual and becomes a soft-limit alarm.
/// </summary>
public class NC_Mill_Link : MonoBehaviour
{
    public AxisMovement Drive_World_X;     // machine Y (saddle): carries the stock
    public AxisMovement Drive_World_Y;     // machine Z (spindle): carries the tool
    public AxisMovement Drive_World_Z;     // machine X (table): carries the stock
    public Transform Tool_Holder;          // rides the spindle drive; tools hang from it
    public Transform Tool_Tip;             // the active tool's transform (tip = position)
    public Transform Work_Origin;          // child of the mounted stock

    public bool Alarm { get; private set; }
    public string Alarm_Text { get; private set; }
    public bool Ready => Tool_Tip != null && Work_Origin != null;

    const float Residual_Tolerance = 1e-5f;   // metres

    /// <summary>Takes ownership of the drives: clears any interpolation target so AxisMovement.Update stays inert.</summary>
    public void Claim()
    {
        Drive_World_X.Stop();
        Drive_World_Y.Stop();
        Drive_World_Z.Stop();
        Alarm = false;
        Alarm_Text = null;
    }

    /// <summary>Instant home (all offsets 0). Only for scene start and restarts, never while a program runs.</summary>
    public void Reset_Home()
    {
        Drive_World_Y.ResetToOrigin();
        Drive_World_X.ResetToOrigin();
        Drive_World_Z.ResetToOrigin();
    }

    public Vector3 Work_Position_Mm => NC_Kinematics.World_To_Work(Tool_Tip.position, Work_Origin.position, Work_Origin.rotation);

    public bool Set_Work_Position_Mm(Vector3 p)
    {
        Vector3 err = NC_Kinematics.Tip_Error_World(p, Tool_Tip.position, Work_Origin.position, Work_Origin.rotation);
        Drive_World_Y.MoveBy(NC_Kinematics.Drive_Delta(err, 1, true));
        Drive_World_X.MoveBy(NC_Kinematics.Drive_Delta(err, 0, false));
        Drive_World_Z.MoveBy(NC_Kinematics.Drive_Delta(err, 2, false));
        Vector3 residual = NC_Kinematics.Tip_Error_World(p, Tool_Tip.position, Work_Origin.position, Work_Origin.rotation);
        if (residual.magnitude <= Residual_Tolerance) return true;
        Alarm = true;
        Alarm_Text = "soft limit on the " + Drive_Name(residual) + ", " + (residual.magnitude * 1000f).ToString("0.0") + " mm short of X" + p.x.ToString("0.#") + " Y" + p.y.ToString("0.#") + " Z" + p.z.ToString("0.#");
        return false;
    }

    static string Drive_Name(Vector3 residual_world)
    {
        float ax = Mathf.Abs(residual_world.x), ay = Mathf.Abs(residual_world.y), az = Mathf.Abs(residual_world.z);
        if (ay >= ax && ay >= az) return "spindle (machine Z)";
        return az >= ax ? "table (machine X)" : "saddle (machine Y)";
    }

    /// <summary>Drives every offset toward 0 at the rapid rate, spindle first; true once home.</summary>
    public bool Home_Step(float dt, bool z_only = false)
    {
        float step = NC_Planner.Rapid_Mm_Min / 60000f * dt;
        if (!Step_Home(Drive_World_Y, step)) return false;
        if (z_only) return true;
        bool x = Step_Home(Drive_World_X, step);
        bool z = Step_Home(Drive_World_Z, step);
        return x && z;
    }

    static bool Step_Home(AxisMovement drive, float step)
    {
        float off = drive.OffsetFromOrigin;
        if (Mathf.Abs(off) <= 1e-6f) return true;
        drive.MoveBy(Mathf.Clamp(-off, -step, step));
        return Mathf.Abs(drive.OffsetFromOrigin) <= 1e-6f;
    }
}
