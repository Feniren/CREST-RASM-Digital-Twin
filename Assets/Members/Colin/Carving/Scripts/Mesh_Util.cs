using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Helpers for the flat-shaded procedural meshes both stocks build.</summary>
public static class Mesh_Util
{
    /// <summary>
    /// Unit normal of triangle (a, b, c) — the one Mesh.RecalculateNormals derives for unshared
    /// vertices. Vector3.normalized returns zero for anything shorter than 1e-5, which the cross
    /// product of a millimetre-scale triangle is, so the length is divided out explicitly.
    /// Degenerate triangles give Vector3.zero.
    /// </summary>
    public static Vector3 Flat_Normal(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 n = Vector3.Cross(b - a, c - a);
        float len = Mathf.Sqrt(n.sqrMagnitude);
        return len > 0f ? n / len : Vector3.zero;
    }

    /// <summary>
    /// Puts a fresh dynamic, unsaved mesh on the filter and returns it. A previous unsaved mesh
    /// left there by a script reload during Play is destroyed first, so nothing leaks.
    /// </summary>
    public static Mesh Replace_Dynamic_Mesh(MeshFilter filter, string name)
    {
        var old = filter.sharedMesh;
        if (old != null && old.hideFlags == HideFlags.DontSave) Object.Destroy(old);
        var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32, hideFlags = HideFlags.DontSave };
        mesh.MarkDynamic();
        filter.sharedMesh = mesh;
        return mesh;
    }
}
