using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Scene wrapper for Lathe_Stock_Data. Axis = local +Z from 0 to Length; a tool's radius is its
/// distance from that axis, so a tool anywhere around the stock cuts at its radius. Unit scale
/// expected. The field is created in Awake and never serialized.
/// </summary>
public class Lathe_Stock : MonoBehaviour
{
    public float Length = 0.120f;
    public float Radius = 0.025f;
    public float Cell = 0.0005f;
    public int Segments = 48;
    public Material Material;

    public Lathe_Stock_Data Data { get; private set; }
    public float Last_Remesh_Ms { get; private set; }
    public int Vertex_Count { get; private set; }

    Mesh mesh;
    readonly List<Vector3> verts = new List<Vector3>();
    readonly List<int> tris = new List<int>();
    bool warned_gap;
    static readonly System.Diagnostics.Stopwatch clock = new System.Diagnostics.Stopwatch();

    void Awake()
    {
        Data = new Lathe_Stock_Data(Length, Radius, Cell);
        var mat = Material != null ? Material : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mesh = new Mesh { name = name + "_Surface", indexFormat = IndexFormat.UInt32 };
        mesh.MarkDynamic();
        var mf = GetComponent<MeshFilter>();
        if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        var mr = GetComponent<MeshRenderer>();
        if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        Rebuild();
    }

    public void Reset_Stock()
    {
        Data.Reset();
    }

    public void Carve(Tool_Profile tool, Vector3 prev_world, Vector3 curr_world)
    {
        Data.Carve(tool, To_ZR(prev_world), To_ZR(curr_world));
        if (Data.Gap_Warning && !warned_gap)
        {
            Debug.LogWarning(name + ": a tool moved more than " + (Lathe_Stock_Data.Max_Sub_Steps * Cell * 500f) + " mm in one step; the sweep was capped and may have gaps.");
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
        if (Data.Dirty) Rebuild();
    }

    void Rebuild()
    {
        clock.Restart();
        verts.Clear();
        tris.Clear();
        Data.Build_Mesh(Segments, verts, tris);
        mesh.Clear(true);
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0, false);
        mesh.RecalculateNormals();
        mesh.bounds = new Bounds(new Vector3(0f, 0f, Data.Length * 0.5f), new Vector3(Data.Radius * 2f, Data.Radius * 2f, Data.Length));
        Data.Dirty = false;
        Last_Remesh_Ms = (float)clock.Elapsed.TotalMilliseconds;
        Vertex_Count = mesh.vertexCount;
    }

    void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.8f);
        Gizmos.DrawWireCube(new Vector3(0f, 0f, Length * 0.5f), new Vector3(Radius * 2f, Radius * 2f, Length));
    }
}
