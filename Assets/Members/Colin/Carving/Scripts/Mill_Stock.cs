using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Scene wrapper for Mill_Stock_Data: one child mesh per slab, rebuilt in LateUpdate when dirty.
/// Local origin is the block's min corner; the transform is expected to be unit scale with the
/// tool axis parallel to local +Y. The field is created in Awake and never serialized.
/// </summary>
public class Mill_Stock : MonoBehaviour
{
    public Vector3 Size = new Vector3(0.150f, 0.050f, 0.100f);
    public float Cell = 0.001f;
    public float Pitch = 0.001f;
    public Material Material;

    public Mill_Stock_Data Data { get; private set; }
    public float Last_Remesh_Ms { get; private set; }
    public int Vertex_Count { get; private set; }

    Mesh[] meshes;
    readonly List<Vector3> verts = new List<Vector3>();
    readonly List<int> tris = new List<int>();
    bool warned_axis, warned_gap;
    static readonly System.Diagnostics.Stopwatch clock = new System.Diagnostics.Stopwatch();

    void Awake()
    {
        Data = new Mill_Stock_Data(Size, Cell, Pitch);
        var mat = Material != null ? Material : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        meshes = new Mesh[Data.Slab_Count];
        for (int k = 0; k < Data.Slab_Count; k++)
        {
            var go = new GameObject("Slab_" + k);
            go.transform.SetParent(transform, false);
            var mesh = new Mesh { name = name + "_Slab_" + k, indexFormat = IndexFormat.UInt32 };
            mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            meshes[k] = mesh;
        }
        Rebuild_Dirty();
    }

    public void Reset_Stock()
    {
        Data.Reset();
    }

    /// <summary>World-space tip poses; the sweep from prev to curr is carved. Only valid while the stock is not moving.</summary>
    public void Carve(Tool_Profile tool, Vector3 prev_world, Vector3 curr_world, Vector3 tool_up_world)
    {
        Carve_Local(tool, transform.InverseTransformPoint(prev_world), transform.InverseTransformPoint(curr_world), tool_up_world);
    }

    /// <summary>Stock-local tip poses; use this when the stock itself moves between steps.</summary>
    public void Carve_Local(Tool_Profile tool, Vector3 prev_local, Vector3 curr_local, Vector3 tool_up_world)
    {
        if (!warned_axis && Mathf.Abs(Vector3.Dot(tool_up_world.normalized, transform.up)) < 0.99f)
        {
            Debug.LogWarning(name + ": tool axis is not parallel to the stock's Y axis; carving by tip position only.");
            warned_axis = true;
        }
        Data.Carve(tool, prev_local, curr_local);
        if (Data.Gap_Warning && !warned_gap)
        {
            Debug.LogWarning(name + ": a tool moved more than " + (Mill_Stock_Data.Max_Sub_Steps * Cell * 500f) + " mm in one step; the sweep was capped and may have gaps.");
            warned_gap = true;
        }
    }

    void LateUpdate()
    {
        if (Data != null) Rebuild_Dirty();   // Data is not serialized: a script reload during Play empties it
    }

    void Rebuild_Dirty()
    {
        bool any = false;
        clock.Restart();
        for (int k = 0; k < Data.Slab_Count; k++)
        {
            if (!Data.Is_Dirty(k)) continue;
            any = true;
            verts.Clear();
            tris.Clear();
            var mesh = meshes[k];
            mesh.Clear(true);
            if (Data.Build_Slab_Mesh(k, verts, tris))
            {
                mesh.SetVertices(verts);
                mesh.SetTriangles(tris, 0, false);
                mesh.RecalculateNormals();
                float y0 = Data.Slab_Bottom(k), y1 = Data.Slab_Top(k);
                mesh.bounds = new Bounds(new Vector3(Data.Size.x * 0.5f, 0.5f * (y0 + y1), Data.Size.z * 0.5f),
                                         new Vector3(Data.Size.x, y1 - y0, Data.Size.z));
            }
        }
        Data.Clear_Dirty();
        if (!any) return;
        Last_Remesh_Ms = (float)clock.Elapsed.TotalMilliseconds;
        int n = 0;
        for (int k = 0; k < meshes.Length; k++) n += meshes[k].vertexCount;
        Vertex_Count = n;
    }

    void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(1f, 0.8f, 0.3f, 0.8f);
        Gizmos.DrawWireCube(Size * 0.5f, Size);
    }
}
