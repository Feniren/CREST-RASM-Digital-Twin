using ProMill8000;
using UnityEngine;

/// <summary>
/// The NC side of the lathe: the cross slide (machine X, radial) and the carriage (machine Z,
/// along the spindle axis) both carry the turret, so both move by the tip error; the spindle
/// carries the stock and only spins. The Work_Origin is static (never under the spindle) on the
/// spindle axis at the stock face: local +Z = program +Z (toward the tailstock), local +X =
/// program +X (radial, toward the turret). Program X is a diameter; plans carry the radius.
/// The programmed point is the tool's imaginary tip: for a round-nose (Ball) insert, the corner
/// of the nose's bounding box facing the axis and the chuck — the tool transform (nose bottom)
/// sits R further along +Z — so faces and shoulders land where they are programmed. Grooving
/// (Flat) tools are programmed at the centre of their width.
/// Positioning is closed loop exactly as on the mill (NC_Mill_Link); a clamped drive leaves a
/// residual that becomes a soft-limit alarm.
/// </summary>
public class NC_Lathe_Link : MonoBehaviour, I_NC_Link
{
    [Header("Drives (machine-axis naming)")]
    [SerializeField] private AxisMovement Cross_X;      // carries the turret
    [SerializeField] private AxisMovement Carriage_Z;   // carries the cross slide

    [Header("Frames")]
    [Tooltip("The chuck; spins about its local Z, which must be the stock axis. The stock mount rides it.")]
    [SerializeField] private Transform Spindle;
    [Tooltip("Static, on the spindle axis at the stock face. Local +Z = program Z, local +X = program X (radial).")]
    [SerializeField] private Transform Work_Origin;
    [Tooltip("Visual parts that turn with the chuck but can't sit under the Spindle (e.g. the CAD chuck inside the lathe prefab); spun about the spindle axis")]
    [SerializeField] private Transform[] Spin_With_Spindle;

    public Transform Tool_Tip { get; private set; }
    public string Alarm_Text { get; private set; }
    public bool Ready => drives != null && Tool_Tip != null;

    const float Residual_Tolerance = 1e-5f;   // metres
    const float Home_Tolerance = 1e-6f;       // metres

    struct Drive
    {
        public AxisMovement Movement;
        public string Label;
    }

    Drive[] drives;   // cross slide first: homing backs away from the work before the carriage travels
    float nose_z_mm;  // imaginary tip → tool transform, along program Z

    void Awake()
    {
        if (Cross_X == null || Carriage_Z == null || Spindle == null || Work_Origin == null)
        {
            Debug.LogWarning("NC_Lathe_Link: Cross_X, Carriage_Z, Spindle and Work_Origin must all be assigned.");
            enabled = false;
            return;
        }
        drives = new[]
        {
            new Drive { Movement = Cross_X, Label = "cross slide (machine X)" },
            new Drive { Movement = Carriage_Z, Label = "carriage (machine Z)" },
        };
    }

    public void Claim()
    {
        foreach (var d in drives) d.Movement.Stop();
        Alarm_Text = null;
    }

    public void Reset_Home()
    {
        foreach (var d in drives) d.Movement.ResetToOrigin();
    }

    public Carving_Tool Mount(I_NC_Tool_Changer changer, int t, Component stock)
    {
        var tool = changer is NC_Lathe_Turret turret ? turret.Mount(t, stock as Lathe_Stock) : null;
        Tool_Tip = tool != null ? tool.transform : null;
        nose_z_mm = tool != null && tool.Profile.Tip_Shape == Tool_Profile.Tip.Ball ? tool.Profile.Radius * NC_Kinematics.M_To_Mm : 0f;
        return tool;
    }

    /// <summary>Program mm with x = radius.</summary>
    public Vector3 Work_Position_Mm => NC_Kinematics.World_To_Lathe_Work(Tool_Tip.position, Work_Origin.position, Work_Origin.rotation) - new Vector3(0f, 0f, nose_z_mm);

    public bool Set_Work_Position_Mm(Vector3 p)
    {
        Vector3 err = Tip_Error(p);
        foreach (var d in drives)
            d.Movement.MoveBy(NC_Kinematics.Drive_Delta(err, d.Movement.WorldDirection, true));
        Vector3 residual = Tip_Error(p);
        if (residual.magnitude <= Residual_Tolerance) return true;
        Alarm_Text = "soft limit on the " + Drive_Name(residual) + ", " + (residual.magnitude * NC_Kinematics.M_To_Mm).ToString("0.0")
                     + " mm short of X" + (p.x * 2f).ToString("0.#") + " Z" + p.z.ToString("0.#");
        return false;
    }

    /// <summary>Cross slide home first, then the carriage; a tool change retracts both so the turret indexes clear of the work.</summary>
    public bool Home_Step(float rapid_mm_min, float dt, bool tool_change = false)
    {
        float step = rapid_mm_min / 60f * NC_Kinematics.Mm_To_M * dt;
        if (!Step_Home(drives[0].Movement, step)) return false;
        return Step_Home(drives[1].Movement, step);
    }

    /// <summary>Spinning the chuck about the stock axis keeps every radius, so the (z, r) carve is unaffected.</summary>
    public void Spin_Visual(float degrees)
    {
        Spindle.Rotate(0f, 0f, degrees, Space.Self);
        if (Spin_With_Spindle == null) return;
        foreach (var t in Spin_With_Spindle)
            if (t != null) t.RotateAround(Spindle.position, Spindle.forward, degrees);
    }

    public string Describe_Position()
    {
        Vector3 w = Work_Position_Mm;
        return "X(Ø) " + (w.x * 2f).ToString("0.000").PadLeft(8) + "   Z " + w.z.ToString("0.000").PadLeft(8);
    }

    Vector3 Tip_Error(Vector3 p) => NC_Kinematics.Lathe_Tip_Error_World(p + new Vector3(0f, 0f, nose_z_mm), Tool_Tip.position, Work_Origin.position, Work_Origin.rotation);

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
