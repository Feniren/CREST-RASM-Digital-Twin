using UnityEngine;

/// <summary>
/// Work coordinates ↔ world. The work origin's local axes are the program axes: local +X =
/// program X, local +Z = program Y, local +Y = program Z, so a program point (x, y, z) mm is
/// local (x, z, y) metres. The tip is positioned in closed loop: the drive that carries the
/// tool moves by the error, the drives that carry the stock move by minus the error.
/// </summary>
public static class NC_Kinematics
{
    public static Vector3 Work_To_World(Vector3 program_mm, Vector3 origin, Quaternion rotation)
    {
        return origin + rotation * new Vector3(program_mm.x, program_mm.z, program_mm.y) * 0.001f;
    }

    public static Vector3 World_To_Work(Vector3 world, Vector3 origin, Quaternion rotation)
    {
        Vector3 l = Quaternion.Inverse(rotation) * (world - origin) * 1000f;
        return new Vector3(l.x, l.z, l.y);
    }

    /// <summary>Where the tip should be minus where it is, in world metres.</summary>
    public static Vector3 Tip_Error_World(Vector3 program_mm, Vector3 tip_world, Vector3 origin, Quaternion rotation)
    {
        return Work_To_World(program_mm, origin, rotation) - tip_world;
    }

    /// <summary>MoveBy delta for a drive that moves along world axis 0/1/2 and carries either the tool or the stock.</summary>
    public static float Drive_Delta(Vector3 err_world, int world_axis, bool carries_tool)
    {
        float e = err_world[world_axis];
        return carries_tool ? e : -e;
    }
}
