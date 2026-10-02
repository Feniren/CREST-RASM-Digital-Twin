using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Generator for Item_StoutGoldTube — a short, stout, solid machined-brass rod lying
// horizontally (long axis along local X) that behaves exactly like
// Item_Epoxy_Block within the ASRS system (same script pattern, root
// BoxCollider + Rigidbody, centered pivot) with different visual
// geometry/material. Does NOT touch Item_Epoxy_Block.
//
// One command for both cases: the first run creates the prefab and drops
// an instance in front of the Scene view camera; later runs only regenerate
// the mesh and material assets in place, so hand edits made to the prefab
// itself are kept.
public static class Item_StoutGoldTube_Builder
{
    private const string MeshPath = "Assets/Meshes/Generated/Item_StoutGoldTube_Rod.asset";
    private const string MaterialPath = "Assets/Materials/Mat_MachinedBrass.mat";
    private const string PrefabPath = "Assets/Game_Objects/Item/Item_StoutGoldTube.prefab";

    // Item_Epoxy_Block's real in-scene width. Its BoxCollider reads 0.0015,
    // but the root carries a 100x scale baked in by the Blender FBX import,
    // so the true width is 150 mm.
    private const float EpoxyWidth = 0.15f;

    // Shorter, fatter sibling of Item_SlimGoldTube (120 x 16 mm): length
    // 60% of epoxy width (90 mm), diameter 16% (24 mm), a 3.75:1
    // length:diameter ratio.
    private const float Length = EpoxyWidth * 0.60f;
    private const float Diameter = EpoxyWidth * 0.16f;

    // Small 45-degree chamfer on both flat ends, scaled up with the diameter.
    private const float Chamfer = 0.0012f;
    private const int Segments = 32;

    [MenuItem("ASRS/Build Item_StoutGoldTube Prop")]
    public static void Build()
    {
        Mesh mesh = SaveMeshAsset(BuildRodMesh(), MeshPath);
        Material material = BuildMaterial();

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"Item_StoutGoldTube_Builder: '{PrefabPath}' already exists — regenerated its mesh and material in place; the prefab itself is untouched.");
            return;
        }

        // Root matches Item_Epoxy_Block: pivot at the object's center (the
        // rod's axis passes through local origin), scale 1,1,1, collider/
        // rigidbody/script on the root, visuals under a child.
        GameObject root = new GameObject("Item_StoutGoldTube");

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        GameObject rod = new GameObject("StoutGoldTube");
        rod.transform.SetParent(visual.transform, false);
        rod.AddComponent<MeshFilter>().sharedMesh = mesh;
        rod.AddComponent<MeshRenderer>().sharedMaterial = material;

        // Box rather than capsule: same collider type as Item_Epoxy_Block,
        // and a flat-bottomed box keeps the rod from rolling off the pallet.
        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = Vector3.zero;
        collider.size = new Vector3(Length, Diameter, Diameter);

        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.angularDamping = 0.05f;

        root.AddComponent<Item_StoutGoldTube>();

        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        SceneView sceneView = SceneView.lastActiveSceneView;
        instance.transform.position = sceneView != null
            ? sceneView.camera.transform.position + sceneView.camera.transform.forward * 0.5f
            : new Vector3(0f, 1f, 0f);

        Selection.activeGameObject = instance;
        if (sceneView != null)
            sceneView.FrameSelected();

        EditorGUIUtility.PingObject(prefab);
        Debug.Log($"Item_StoutGoldTube_Builder: built '{PrefabPath}' ({Length * 1000:F1} mm long x {Diameter * 1000:F1} mm diameter, long axis along X) and dropped an instance in front of the Scene view camera.");
    }

    // ------------------------------------------------------------------
    // Solid rod along X, centered on the origin: flat end caps, 45-degree
    // chamfers, straight smooth-shaded wall. Each band gets its own ring of
    // vertices so the chamfer edges stay crisp. Every normal is set
    // analytically (unit length by construction) — never by normalizing a
    // cross product, which Unity zeroes for tiny faces and renders black.
    // ------------------------------------------------------------------

    private static Mesh BuildRodMesh()
    {
        float r = Diameter * 0.5f;
        float c = Mathf.Min(Chamfer, r * 0.45f);
        float x0 = -Length * 0.5f;
        float x1 = Length * 0.5f;
        float diag = Mathf.Sqrt(0.5f);

        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        Vector3 Radial(float a) => new Vector3(0f, Mathf.Cos(a), Mathf.Sin(a));

        // Winding is checked against the known outward normal; the cross
        // product is only used for its sign.
        void AddQuad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,
                     Vector3 n0, Vector3 n1, Vector3 n2, Vector3 n3,
                     float u0, float u1, float v0, float v1)
        {
            Vector3 expected = n0 + n1 + n2 + n3;
            bool flip = Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), expected) < 0f;

            int s = verts.Count;
            verts.Add(p0); verts.Add(p1); verts.Add(p2); verts.Add(p3);
            norms.Add(n0); norms.Add(n1); norms.Add(n2); norms.Add(n3);
            uvs.Add(new Vector2(u0, v0)); uvs.Add(new Vector2(u1, v0));
            uvs.Add(new Vector2(u1, v1)); uvs.Add(new Vector2(u0, v1));
            if (!flip)
            {
                tris.Add(s); tris.Add(s + 1); tris.Add(s + 2);
                tris.Add(s); tris.Add(s + 2); tris.Add(s + 3);
            }
            else
            {
                tris.Add(s); tris.Add(s + 2); tris.Add(s + 1);
                tris.Add(s); tris.Add(s + 3); tris.Add(s + 2);
            }
        }

        void AddTri(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 normal)
        {
            bool flip = Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), normal) < 0f;

            int s = verts.Count;
            verts.Add(p0); verts.Add(p1); verts.Add(p2);
            norms.Add(normal); norms.Add(normal); norms.Add(normal);
            uvs.Add(new Vector2(0.5f, 0.5f));
            uvs.Add(new Vector2(0.5f + p1.y / Diameter, 0.5f + p1.z / Diameter));
            uvs.Add(new Vector2(0.5f + p2.y / Diameter, 0.5f + p2.z / Diameter));
            if (!flip) { tris.Add(s); tris.Add(s + 1); tris.Add(s + 2); }
            else { tris.Add(s); tris.Add(s + 2); tris.Add(s + 1); }
        }

        for (int i = 0; i < Segments; i++)
        {
            float a0 = i / (float)Segments * Mathf.PI * 2f;
            float a1 = (i + 1) / (float)Segments * Mathf.PI * 2f;
            Vector3 d0 = Radial(a0);
            Vector3 d1 = Radial(a1);
            float u0 = i / (float)Segments;
            float u1 = (i + 1) / (float)Segments;

            // Flat end caps (inside the chamfer).
            AddTri(new Vector3(x0, 0f, 0f), new Vector3(x0, 0f, 0f) + d0 * (r - c), new Vector3(x0, 0f, 0f) + d1 * (r - c), Vector3.left);
            AddTri(new Vector3(x1, 0f, 0f), new Vector3(x1, 0f, 0f) + d0 * (r - c), new Vector3(x1, 0f, 0f) + d1 * (r - c), Vector3.right);

            // Left chamfer: (x0, r-c) -> (x0+c, r).
            Vector3 nl0 = Vector3.left * diag + d0 * diag;
            Vector3 nl1 = Vector3.left * diag + d1 * diag;
            AddQuad(new Vector3(x0, 0f, 0f) + d0 * (r - c), new Vector3(x0, 0f, 0f) + d1 * (r - c),
                    new Vector3(x0 + c, 0f, 0f) + d1 * r, new Vector3(x0 + c, 0f, 0f) + d0 * r,
                    nl0, nl1, nl1, nl0, u0, u1, 0f, 0.02f);

            // Straight wall, smooth-shaded with radial normals.
            AddQuad(new Vector3(x0 + c, 0f, 0f) + d0 * r, new Vector3(x0 + c, 0f, 0f) + d1 * r,
                    new Vector3(x1 - c, 0f, 0f) + d1 * r, new Vector3(x1 - c, 0f, 0f) + d0 * r,
                    d0, d1, d1, d0, u0, u1, 0.02f, 0.98f);

            // Right chamfer: (x1-c, r) -> (x1, r-c).
            Vector3 nr0 = Vector3.right * diag + d0 * diag;
            Vector3 nr1 = Vector3.right * diag + d1 * diag;
            AddQuad(new Vector3(x1 - c, 0f, 0f) + d0 * r, new Vector3(x1 - c, 0f, 0f) + d1 * r,
                    new Vector3(x1, 0f, 0f) + d1 * (r - c), new Vector3(x1, 0f, 0f) + d0 * (r - c),
                    nr0, nr1, nr1, nr0, u0, u1, 0.98f, 1f);
        }

        Mesh mesh = new Mesh { name = "Item_StoutGoldTube_Rod" };
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    // Returns the persistent asset (not the temporary in-memory mesh), so
    // the prefab points at the saved asset even when an existing one was
    // overwritten in place.
    private static Mesh SaveMeshAsset(Mesh mesh, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(mesh, existing);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            return existing;
        }

        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

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

        // #B89A4A muted industrial brass: Metallic 0.88 / Smoothness 0.45
        // (within 0.85-0.95 / 0.40-0.55) — machined, not mirror or jewelry.
        Color brass = new Color(184f / 255f, 154f / 255f, 74f / 255f, 1f);
        if (material.HasProperty("_Color")) material.SetColor("_Color", brass);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", brass);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.88f);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.45f);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.45f);

        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }
}
