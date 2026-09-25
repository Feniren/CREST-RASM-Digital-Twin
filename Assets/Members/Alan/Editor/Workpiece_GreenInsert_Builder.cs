using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// One-shot generator for Workpiece_GreenInsert — a thin chamfered
// rectangular slab prop. Built procedurally (no 3D modeling tool or import
// pipeline involved, unlike the CAD-imported meshes elsewhere in this
// project) since nothing like it exists to import. Re-running overwrites
// the mesh/material in place and rebuilds the prefab.
public static class Workpiece_GreenInsert_Builder
{
    private const string MeshPath = "Assets/Meshes/Generated/Workpiece_GreenInsert.asset";
    private const string MaterialPath = "Assets/Materials/Workpiece_GreenInsert.mat";
    private const string PrefabPath = "Assets/Game_Objects/Item/Workpiece_GreenInsert.prefab";

    // Dimensions: back to the original geometry brief (thin slab, small
    // chamfer) at a size that's actually visible/findable in a normal
    // scene — matching Item_Epoxy_Block's real (1.5mm) size made it
    // functionally invisible without zooming in tight, which is why it
    // kept turning up "not there." A few centimeters across, easy to spot
    // and to resize later if it needs to match something specific.
    private const float Width = 0.05f;      // X
    private const float Thickness = 0.006f; // Y ("thin")
    private const float Depth = 0.035f;     // Z
    private const float Chamfer = 0.0015f;  // edge bevel size ("very small")

    [MenuItem("ASRS/Build Workpiece_GreenInsert Prop")]
    public static void Build()
    {
        Mesh mesh = BuildChamferedBoxMesh(Width, Thickness, Depth, Chamfer);
        SaveMeshAsset(mesh);

        Material material = BuildMaterial();

        GameObject go = new GameObject("Workpiece_GreenInsert");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;

        BoxCollider collider = go.AddComponent<BoxCollider>();
        collider.size = new Vector3(Width, Thickness, Depth);

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.mass = 0.2f;
        rb.angularDamping = 0.05f;

        go.AddComponent<Item_Workpiece_GreenInsert>();

        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);

        // Drop an actual instance into the currently open scene, right in
        // front of the Scene view camera, and frame the view on it — so
        // there's no hunting through the Project window or dragging it in
        // by hand to see whether it's actually there this time.
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
            instance.transform.position = sceneView.camera.transform.position + sceneView.camera.transform.forward * 0.5f;
        else
            instance.transform.position = new Vector3(0f, 1f, 0f);

        Selection.activeGameObject = instance;
        if (sceneView != null)
            sceneView.FrameSelected();

        EditorGUIUtility.PingObject(prefab);
        Debug.Log($"Workpiece_GreenInsert_Builder: built '{PrefabPath}' ({Width * 1000:F0}mm x {Thickness * 1000:F0}mm x {Depth * 1000:F0}mm, {Chamfer * 1000:F1}mm chamfer) and dropped an instance right in front of the Scene view camera — it should be selected and framed now. Remove/reposition it once you've confirmed it's visible; only the prefab asset itself is the real deliverable.");
    }

    // ------------------------------------------------------------------
    // Chamfered box mesh — a standard "truncated box": each of the 12
    // edges gets a flat bevel face and each of the 8 corners gets a small
    // triangular facet, with the 6 main faces shrunk to make room for
    // both. Flat-shaded (every face owns its own vertices/normal) since a
    // machined polymer part should read with hard edges, not smoothed.
    //
    // Winding is self-correcting: every Add call takes the face's roughly-
    // known outward direction and flips its own winding if the computed
    // normal points the wrong way, rather than relying on having listed
    // each quad's 4 corners in exactly the right order by hand.
    // ------------------------------------------------------------------

    private static Mesh BuildChamferedBoxMesh(float width, float height, float depth, float chamfer)
    {
        float hx = width * 0.5f;
        float hy = height * 0.5f;
        float hz = depth * 0.5f;
        float c = Mathf.Min(chamfer, hx * 0.9f, hy * 0.9f, hz * 0.9f);

        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        // Per-corner vertex "roles": A keeps X at full extent (used by the
        // +-X main faces), B keeps Y at full extent, C keeps Z at full
        // extent. Two of the three roles meet along each beveled edge; all
        // three meet at each chamfered corner.
        Vector3 A(int sx, int sy, int sz) => new Vector3(sx * hx, sy * (hy - c), sz * (hz - c));
        Vector3 B(int sx, int sy, int sz) => new Vector3(sx * (hx - c), sy * hy, sz * (hz - c));
        Vector3 C(int sx, int sy, int sz) => new Vector3(sx * (hx - c), sy * (hy - c), sz * hz);

        void AddQuad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 expectedNormal)
        {
            Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0);
            if (Vector3.Dot(normal, expectedNormal) < 0f)
            {
                (p1, p3) = (p3, p1);
                normal = -normal;
            }
            normal.Normalize();

            int start = verts.Count;
            verts.Add(p0); verts.Add(p1); verts.Add(p2); verts.Add(p3);
            for (int i = 0; i < 4; i++) norms.Add(normal);
            uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0)); uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(0, 1));
            tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
            tris.Add(start); tris.Add(start + 2); tris.Add(start + 3);
        }

        void AddTri(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 expectedNormal)
        {
            Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0);
            if (Vector3.Dot(normal, expectedNormal) < 0f)
            {
                (p1, p2) = (p2, p1);
                normal = -normal;
            }
            normal.Normalize();

            int start = verts.Count;
            verts.Add(p0); verts.Add(p1); verts.Add(p2);
            for (int i = 0; i < 3; i++) norms.Add(normal);
            uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0)); uvs.Add(new Vector2(0, 1));
            tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
        }

        int[] signs = { -1, 1 };

        // 6 main faces.
        foreach (int sx in signs)
            AddQuad(A(sx, -1, -1), A(sx, -1, 1), A(sx, 1, 1), A(sx, 1, -1), new Vector3(sx, 0, 0));
        foreach (int sy in signs)
            AddQuad(B(-1, sy, -1), B(1, sy, -1), B(1, sy, 1), B(-1, sy, 1), new Vector3(0, sy, 0));
        foreach (int sz in signs)
            AddQuad(C(-1, -1, sz), C(1, -1, sz), C(1, 1, sz), C(-1, 1, sz), new Vector3(0, 0, sz));

        // 12 edge bevels — one per (axis pair, sign, sign) combination.
        // Each bevel's outward direction is diagonal, roughly bisecting the
        // two main faces it connects.
        foreach (int sx in signs)
            foreach (int sy in signs)
                AddQuad(A(sx, sy, -1), A(sx, sy, 1), B(sx, sy, 1), B(sx, sy, -1), new Vector3(sx, sy, 0));

        foreach (int sx in signs)
            foreach (int sz in signs)
                AddQuad(A(sx, -1, sz), A(sx, 1, sz), C(sx, 1, sz), C(sx, -1, sz), new Vector3(sx, 0, sz));

        foreach (int sy in signs)
            foreach (int sz in signs)
                AddQuad(B(-1, sy, sz), B(1, sy, sz), C(1, sy, sz), C(-1, sy, sz), new Vector3(0, sy, sz));

        // 8 corner facets — outward direction is the full diagonal.
        foreach (int sx in signs)
            foreach (int sy in signs)
                foreach (int sz in signs)
                    AddTri(A(sx, sy, sz), B(sx, sy, sz), C(sx, sy, sz), new Vector3(sx, sy, sz));

        Mesh mesh = new Mesh { name = "Workpiece_GreenInsert" };
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    private static void SaveMeshAsset(Mesh mesh)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(MeshPath));
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(mesh, existing);
            AssetDatabase.SaveAssets();
        }
        else
        {
            AssetDatabase.CreateAsset(mesh, MeshPath);
        }
    }

    // Picks a shader that actually matches the ACTIVE render pipeline
    // instead of copying Epoxy.mat's — that material's shader turned out to
    // be the Built-in "Standard" shader (guid 933532a4..., which resolves
    // to nothing inside Assets, confirming it's the global built-in one),
    // while this project has a custom SRP (URP, package
    // com.unity.render-pipelines.universal) assigned in Graphics Settings.
    // A Built-in-shader material renders solid black/magenta under URP —
    // which is almost certainly why the new prop showed up blacked out,
    // and Epoxy.mat is likely equally broken wherever it's actually seen.
    private static Material BuildMaterial()
    {
        Shader shader = GraphicsSettings.currentRenderPipeline != null
            ? Shader.Find("Universal Render Pipeline/Lit")
            : Shader.Find("Standard");

        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        // Muted, desaturated industrial green — not a bright/saturated hue.
        Color green = new Color(0.24f, 0.32f, 0.24f, 1f);
        if (material.HasProperty("_Color")) material.SetColor("_Color", green);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", green);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.3f);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.3f);

        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }
}
