using UnityEngine;

/// <summary>
/// The machine side NC_Runner drives: positions the active tool tip at program coordinates
/// in closed loop, homes, and mounts tools. NC_Mill_Link (3 axes, spindle carries the tool)
/// and NC_Lathe_Link (X cross slide + Z carriage, spindle carries the stock) implement it.
/// </summary>
public interface I_NC_Link
{
    bool Ready { get; }
    Transform Tool_Tip { get; }
    string Alarm_Text { get; }
    Vector3 Work_Position_Mm { get; }

    void Claim();
    void Reset_Home();
    bool Set_Work_Position_Mm(Vector3 program_mm);
    /// <summary>Steps toward home at the rapid rate; true once there. tool_change = only the retract a tool change needs.</summary>
    bool Home_Step(float rapid_mm_min, float dt, bool tool_change);
    Carving_Tool Mount(I_NC_Tool_Changer changer, int t, Component stock);
    /// <summary>Visual spindle rotation; must not change what is carved.</summary>
    void Spin_Visual(float degrees);
    /// <summary>The readout's position line, e.g. "X 10.000   Y 5.000   Z -3.000".</summary>
    string Describe_Position();
}

/// <summary>Where the tools live between uses: NC_Tool_Rack (mill) or NC_Lathe_Turret.</summary>
public interface I_NC_Tool_Changer
{
    int Active { get; }
    Carving_Tool Active_Tool { get; }
    bool Has(int t);
    void Bind_Stock(Component stock);
    string Describe(int t);
}
