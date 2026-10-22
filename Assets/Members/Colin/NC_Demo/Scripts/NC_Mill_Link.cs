using ProMill8000;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// The NC side of the mill: the three drives named by MACHINE axis, the holder the active tool
/// hangs from, and the work origin program coordinates are measured from. Each drive declares
/// the axis it moves along in its own local frame (AxisMovement.Axis) and exposes it in world
/// space (WorldDirection), so the machine↔world mapping is stated exactly once, on the drives,
/// holds at any mill rotation, and is never repeated here. The tip is positioned in closed loop
/// each step (NC_Kinematics): the spindle moves by the error, the table and saddle carry the
/// stock and move by minus the error; a MoveBy that AxisMovement clamped shows up as a residual
/// and becomes a soft-limit alarm.
/// </summary>
public class NC_Mill_Link : MonoBehaviour
{
    [Header("Drives (machine-axis naming: X table, Y saddle, Z spindle)")]
    [FormerlySerializedAs("Drive_World_Z")] [SerializeField] private AxisMovement Table_X;     // carries the stock
    [FormerlySerializedAs("Drive_World_X")] [SerializeField] private AxisMovement Saddle_Y;    // carries the stock
    [FormerlySerializedAs("Drive_World_Y")] [SerializeField] private AxisMovement Spindle_Z;   // carries the tool

    [Header("Frames")]
    [Tooltip("Rides the spindle drive; the active tool hangs from it with its shank at the spindle nose.")]
    [SerializeField] private Transform Tool_Holder;
    [Tooltip("Program origin, riding the stock drives. Local +X = program X, local +Z = program Y, local +Y = program Z.")]
    [SerializeField] private Transform Work_Origin;

    public Transform Tool_Tip { get; private set; }   // the active tool's transform (tip = position)
    public string Alarm_Text { get; private set; }
    public bool Ready => drives != null && Tool_Tip != null;

    const float Residual_Tolerance = 1e-5f;   // metres: a smaller closed-loop residual is a hit
    const float Home_Tolerance = 1e-6f;       // metres

    struct Drive
    {
        public AxisMovement Movement;
        public string Label;
        public bool Carries_Tool;
    }

    Drive[] drives;   // spindle first: homing retracts before the table moves

    void Awake()
    {
        if (Table_X == null || Saddle_Y == null || Spindle_Z == null || Tool_Holder == null || Work_Origin == null)
        {
            Debug.LogWarning("NC_Mill_Link: Table_X, Saddle_Y, Spindle_Z, Tool_Holder and Work_Origin must all be assigned.");
            enabled = false;
            return;
        }
        drives = new[]
        {
            new Drive { Movement = Spindle_Z, Label = "spindle (machine Z)", Carries_Tool = true },
            new Drive { Movement = Table_X, Label = "table (machine X)" },
            new Drive { Movement = Saddle_Y, Label = "saddle (machine Y)" },
        };
    }

    /// <summary>Takes ownership of the drives: clears any interpolation target so AxisMovement.Update stays inert.</summary>
    public void Claim()
    {
        foreach (var d in drives) d.Movement.Stop();
        Alarm_Text = null;
    }

    /// <summary>Instant home (all offsets 0). Only for scene start and restarts, never while a program runs.</summary>
    public void Reset_Home()
    {
        foreach (var d in drives) d.Movement.ResetToOrigin();
    }

    /// <summary>Hangs tool T from the holder (a teleport) and makes it the tip; null if the rack has no such tool.</summary>
    public Carving_Tool Mount(NC_Tool_Rack rack, int t, Mill_Stock stock)
    {
        var tool = rack.Mount(t, Tool_Holder, stock);
        Tool_Tip = tool != null ? tool.transform : null;
        return tool;
    }

    public Vector3 Work_Position_Mm => NC_Kinematics.World_To_Work(Tool_Tip.position, Work_Origin.position, Work_Origin.rotation);

    public bool Set_Work_Position_Mm(Vector3 p)
    {
        Vector3 err = Tip_Error(p);
        foreach (var d in drives)
            d.Movement.MoveBy(NC_Kinematics.Drive_Delta(err, d.Movement.WorldDirection, d.Carries_Tool));
        Vector3 residual = Tip_Error(p);
        if (residual.magnitude <= Residual_Tolerance) return true;
        Alarm_Text = "soft limit on the " + Drive_Name(residual) + ", " + (residual.magnitude * NC_Kinematics.M_To_Mm).ToString("0.0")
                     + " mm short of X" + p.x.ToString("0.#") + " Y" + p.y.ToString("0.#") + " Z" + p.z.ToString("0.#");
        return false;
    }

    /// <summary>Drives every offset toward 0 at the rapid rate, spindle first; true once home.</summary>
    public bool Home_Step(float rapid_mm_min, float dt, bool z_only = false)
    {
        float step = rapid_mm_min / 60f * NC_Kinematics.Mm_To_M * dt;
        if (!Step_Home(drives[0].Movement, step)) return false;
        if (z_only) return true;
        bool home = true;
        for (int i = 1; i < drives.Length; i++) home &= Step_Home(drives[i].Movement, step);   // every drive steps each frame
        return home;
    }

    Vector3 Tip_Error(Vector3 p) => NC_Kinematics.Tip_Error_World(p, Tool_Tip.position, Work_Origin.position, Work_Origin.rotation);

    // The drive whose axis carries the largest residual component.
    string Drive_Name(Vector3 residual_world)
    {
        string label = drives[0].Label;
        float largest = -1f;
        foreach (var d in drives)
        {
            float r = Mathf.Abs(Vector3.Dot(residual_world, d.Movement.WorldDirection));
            if (r > largest) { largest = r; label = d.Label; }
        }
        return label;
    }

    static bool Step_Home(AxisMovement drive, float step)
    {
        float off = drive.OffsetFromOrigin;
        if (Mathf.Abs(off) <= Home_Tolerance) return true;
        drive.MoveBy(Mathf.Clamp(-off, -step, step));
        return Mathf.Abs(drive.OffsetFromOrigin) <= Home_Tolerance;
    }
}
