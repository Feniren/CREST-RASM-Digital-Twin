using UnityEngine;

/// <summary>
/// Sub-stepping rule shared by the mill and lathe stocks: a tool sweep is stamped every half
/// cell so consecutive stamps overlap, capped at Max_Sub_Steps so a teleport cannot stall a frame.
/// </summary>
public static class Carve_Sweep
{
    public const int Max_Sub_Steps = 64;

    public static float Sub_Step(float cell) => cell * 0.5f;

    /// <summary>Longest sweep (metres) that is stamped without gaps at this cell size.</summary>
    public static float Max_Distance(float cell) => Max_Sub_Steps * Sub_Step(cell);

    /// <summary>Stamps for a sweep of dist metres; capped is set when the sweep was longer than Max_Distance.</summary>
    public static int Sub_Steps(float dist, float cell, out bool capped)
    {
        int n = dist <= 1e-9f ? 1 : Mathf.CeilToInt(dist / Sub_Step(cell));
        capped = n > Max_Sub_Steps;
        return capped ? Max_Sub_Steps : n;
    }
}
