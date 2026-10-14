using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene wrapper for Mill_Stock_Data: one mesh on this object's MeshFilter, rebuilt in LateUpdate
/// when any slab is dirty. Local origin is the block's min corner; the transform is expected to
/// be unit scale with the tool axis parallel to local +Y. The field is created in Awake and never
/// serialized, so a script reload during Play empties it — Reset_Stock brings it back.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Mill_Stock : MonoBehaviour
{
    [Header("Stock (metres; read once in Awake)")]
    [SerializeField] private Vector3 Size = new Vector3(0.150f, 0.050f, 0.100f);
    [SerializeField] private float Cell = 0.001f;
    [SerializeField] private float Pitch = 0.001f;

    public Mill_Stock_Data Data { get; private set; }
    public float Last_Remesh_Ms { get; private set; }
    public int Vertex_Count => verts.Count;

    // Each slab keeps its own geometry so only dirty slabs are re-contoured; the combined lists
    // are what the single mesh uploads.
    sealed class Slab_Buffers
    {
        public readonly List<Vector3> Verts = new List<Vector3>();
        public readonly List<Vector3> Normals = new List<Vector3>();
        public readonly List<int> Tris = new List<int>();
    }

    static readonly Color Gizmo_Colour = new Color(1f, 0.8f, 0.3f, 0.8f);
    const float Axis_Alignment = 0.99f;   // |dot| below this and the tool axis is not parallel to local Y

    Slab_Buffers[] slabs;
    Mesh mesh;
    readonly List<Vector3> verts = new List<Vector3>();
    readonly List<Vector3> normals = new List<Vector3>();
    readonly List<int> tris = new List<int>();
    bool warned_axis, warned_gap, warned_lost;

    void Awake()
    {
        Initialise();
    }

    void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
    }

    /// <summary>Back to a solid block. Also recovers from a script reload during Play, which empties the field.</summary>
    public void Reset_Stock()
    {
        if (Data == null) Initialise();
        else Data.Reset();
        warned_gap = false;
    }

    /// <summary>Stock-local tip poses; the sweep from prev to curr is carved. Local space so a stock riding a machine table carves the relative motion.</summary>
    public void Carve_Local(Tool_Profile tool, Vector3 prev_local, Vector3 curr_local, Vector3 tool_up_world)
    {
        if (Data == null) { Warn_Lost(); return; }
        if (!warned_axis && Mathf.Abs(Vector3.Dot(tool_up_world.normalized, transform.up)) < Axis_Alignment)
        {
            Debug.LogWarning(name + ": tool axis is not parallel to the stock's Y axis; carving by tip position only.");
            warned_axis = true;
        }
        Data.Carve(tool, prev_local, curr_local);
        if (Data.Gap_Warning && !warned_gap)
        {
            Debug.LogWarning(name + ": a tool moved more than " + (Carve_Sweep.Max_Distance(Cell) * 1000f) + " mm in one step; the sweep was capped and may have gaps.");
            warned_gap = true;
        }
    }

    void LateUpdate()
    {
        if (Data == null) { Warn_Lost(); return; }
        Rebuild_Dirty();
    }

    void Initialise()
    {
        Data = new Mill_Stock_Data(Size, Cell, Pitch);
        slabs = new Slab_Buffers[Data.Slab_Count];
        for (int k = 0; k < slabs.Length; k++) slabs[k] = new Slab_Buffers();
        mesh = Mesh_Util.Replace_Dynamic_Mesh(GetComponent<MeshFilter>(), name + "_Stock");
        warned_lost = false;
        Rebuild_Dirty();
    }

    void Rebuild_Dirty()
    {
        double start = Time.realtimeSinceStartupAsDouble;
        bool any = false;
        for (int k = 0; k < slabs.Length; k++)
        {
            if (!Data.Is_Dirty(k)) continue;
            any = true;
            var s = slabs[k];
            s.Verts.Clear();
            s.Normals.Clear();
            s.Tris.Clear();
            Data.Build_Slab_Mesh(k, s.Verts, s.Normals, s.Tris);
        }
        Data.Clear_Dirty();
        if (!any) return;

        verts.Clear();
        normals.Clear();
        tris.Clear();
        for (int k = 0; k < slabs.Length; k++)
        {
            var s = slabs[k];
            int offset = verts.Count;
            verts.AddRange(s.Verts);
            normals.AddRange(s.Normals);
            for (int i = 0; i < s.Tris.Count; i++) tris.Add(s.Tris[i] + offset);
        }
        mesh.Clear(true);
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetTriangles(tris, 0, false);
        mesh.bounds = new Bounds(Data.Size * 0.5f, Data.Size);
        Last_Remesh_Ms = (float)((Time.realtimeSinceStartupAsDouble - start) * 1000.0);
    }

    void Warn_Lost()
    {
        if (warned_lost) return;
        warned_lost = true;
        Debug.LogWarning(name + ": stock field lost (script reload during Play); Reset_Stock rebuilds it.");
    }

    void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Gizmo_Colour;
        Gizmos.DrawWireCube(Size * 0.5f, Size);
    }
}
