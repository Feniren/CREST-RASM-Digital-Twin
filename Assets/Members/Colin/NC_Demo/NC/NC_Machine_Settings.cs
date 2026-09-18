using System;

/// <summary>
/// The machine numbers the planner and runner need. Defaults are the ProMill 8000 figures from
/// the user guide; docs/VR_Modules/06_CNCBase_Mockup/04_CNCBase_Reference.md §5.1 records that
/// the rapid and feed values are still unverified against the lab machine, which is why they
/// are data on an NC_Machine_Profile asset rather than constants.
/// </summary>
[Serializable]
public sealed class NC_Machine_Settings
{
    public float Rapid_Mm_Min = 2000f;          // rapid traverse
    public float Default_Feed_Mm_Min = 254f;    // "up to 10 in/min" cutting feed, used when a program never sets F
    public float Arc_Tolerance_Mm = 0.01f;      // an I/J arc whose end radius differs by more raises an alarm
    public float Peck_Clearance_Mm = 1f;        // G83 retract above the last depth before the next peck
    public float Stop_Seconds = 1f;             // how long M00/M01 pause an unattended run
}
