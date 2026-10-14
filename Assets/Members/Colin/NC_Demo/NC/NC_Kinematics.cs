using UnityEngine;

/// <summary>
/// Work coordinates ↔ world. The work origin's local axes are the program axes: local +X =
/// program X, local +Z = program Y, local +Y = program Z, so a program point (x, y, z) mm is
/// local (x, z, y) metres. The tip is positioned in closed loop: the drive that carries the
/// tool moves by the error, the drives that carry the stock move by minus the error.
/// </summary>
public static class NC_Kinematics
{
    public const float Mm_To_M = 0.001f;
    public const float M_To_Mm = 1000f;

    /// <summary>Program (x, y, z) ↔ work-origin local (x, z, y). The swizzle is its own inverse.</summary>
    public static Vector3 Swap_YZ(Vector3 v) => new Vector3(v.x, v.z, v.y);

    public static Vector3 Work_To_World(Vector3 program_mm, Vector3 origin, Quaternion rotation)
    {
        return origin + rotation * (Swap_YZ(program_mm) * Mm_To_M);
    }

    public static Vector3 World_To_Work(Vector3 world, Vector3 origin, Quaternion rotation)
    {
        return Swap_YZ(Quaternion.Inverse(rotation) * (world - origin) * M_To_Mm);
    }

    /// <summary>Where the tip should be minus where it is, in world metres.</summary>
    public static Vector3 Tip_Error_World(Vector3 program_mm, Vector3 tip_world, Vector3 origin, Quaternion rotation)
    {
        return Work_To_World(program_mm, origin, rotation) - tip_world;
    }

    /// <summary>MoveBy delta for a drive whose axis is the unit vector drive_direction_world and that carries either the tool or the stock. Flipping the direction flips the delta: the same physical motion.</summary>
    public static float Drive_Delta(Vector3 err_world, Vector3 drive_direction_world, bool carries_tool)
    {
        float e = Vector3.Dot(err_world, drive_direction_world);
        return carries_tool ? e : -e;
    }
}
