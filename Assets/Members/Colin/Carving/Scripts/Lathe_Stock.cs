using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene wrapper for Lathe_Stock_Data: one revolved mesh on this object's MeshFilter. Axis =
/// local +Z from 0 to Length; a tool's radius is its distance from that axis, so a tool anywhere
/// around the stock cuts at its radius. Unit scale expected. The field is created in Awake and
/// never serialized, so a script reload during Play empties it — Reset_Stock brings it back.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Lathe_Stock : MonoBehaviour
{
    [Header("Stock (metres; read once in Awake)")]
    [SerializeField] private float Length = 0.120f;
    [SerializeField] private float Radius = 0.025f;
    [SerializeField] private float Cell = 0.0005f;
    [SerializeField] private int Segments = 48;

    public Lathe_Stock_Data Data { get; private set; }
    public float Last_Remesh_Ms { get; private set; }
    public int Vertex_Count => verts.Count;

    static readonly Color Gizmo_Colour = new Color(0.4f, 0.8f, 1f, 0.8f);

    Mesh mesh;
    readonly List<Vector3> verts = new List<Vector3>();
    readonly List<Vector3> normals = new List<Vector3>();
    readonly List<int> tris = new List<int>();
    bool warned_gap, warned_lost;

    void Awake()
    {
        Initialise();
    }

    void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
    }

    /// <summary>Back to a solid cylinder. Also recovers from a script reload during Play, which empties the field.</summary>
    public void Reset_Stock()
    {
        if (Data == null) Initialise();
        else Data.Reset();
        warned_gap = false;
    }

    /// <summary>World-space tip poses; the sweep from prev to curr is carved.</summary>
    public void Carve(Tool_Profile tool, Vector3 prev_world, Vector3 curr_world)
    {
        if (Data == null) { Warn_Lost(); return; }
        Data.Carve(tool, To_ZR(prev_world), To_ZR(curr_world));
        if (Data.Gap_Warning && !warned_gap)
        {
            Debug.LogWarning(name + ": a tool moved more than " + (Carve_Sweep.Max_Distance(Cell) * 1000f) + " mm in one step; the sweep was capped and may have gaps.");
            warned_gap = true;
        }
    }

    Vector2 To_ZR(Vector3 world)
    {
        Vector3 l = transform.InverseTransformPoint(world);
        return new Vector2(l.z, new Vector2(l.x, l.y).magnitude);
    }

    void LateUpdate()
    {
        if (Data == null) { Warn_Lost(); return; }
        if (Data.Dirty) Rebuild();
    }

    void Initialise()
    {
        Data = new Lathe_Stock_Data(Length, Radius, Cell);
        mesh = Mesh_Util.Replace_Dynamic_Mesh(GetComponent<MeshFilter>(), name + "_Stock");
        warned_lost = false;
        Rebuild();
    }

    void Rebuild()
    {
        double start = Time.realtimeSinceStartupAsDouble;
        verts.Clear();
        normals.Clear();
        tris.Clear();
        Data.Build_Mesh(Segments, verts, normals, tris);
        mesh.Clear(true);
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetTriangles(tris, 0, false);
        mesh.bounds = new Bounds(new Vector3(0f, 0f, Data.Length * 0.5f), new Vector3(Data.Radius * 2f, Data.Radius * 2f, Data.Length));
        Data.Clear_Dirty();
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
        Gizmos.DrawWireCube(new Vector3(0f, 0f, Length * 0.5f), new Vector3(Radius * 2f, Radius * 2f, Length));
    }
}
