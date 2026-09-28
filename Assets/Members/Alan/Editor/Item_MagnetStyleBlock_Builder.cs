using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Generator for Item_MagnetStyleBlock — a low-profile black rounded-corner
// block that behaves exactly like Item_Epoxy_Block within the ASRS system
// (same script pattern, root BoxCollider + Rigidbody, centered pivot) with
// different visual geometry/material. Does NOT touch Item_Epoxy_Block.
//
// One command for both cases: the first run creates the prefab and drops
// an instance in front of the Scene view camera; later runs only regenerate
// the mesh and material assets in place, so hand edits made to the prefab
// itself are kept.
public static class Item_MagnetStyleBlock_Builder
{
    private const string MeshPath = "Assets/Meshes/Generated/Item_MagnetStyleBlock_Body.asset";
    private const string MaterialPath = "Assets/Materials/Mat_BlackRubberizedBlock.mat";
    private const string PrefabPath = "Assets/Game_Objects/Item/Item_MagnetStyleBlock.prefab";

    // Item_Epoxy_Block's real in-scene size, in its own local frame (the
    // frame Item_Slotted_Table.SetItem seats items in). Its BoxCollider reads
    // 0.0015 x 0.0005 x 0.001, but the root carries a 100x scale baked in
    // by the Blender FBX import (EpoxyBlock.fbx model node: Lcl Scaling
    // 100), so the true size is 100x that.
    private const float EpoxyWidth = 0.15f;   // X
    private const float EpoxyHeight = 0.05f;  // Y
    private const float EpoxyDepth = 0.10f;   // Z

    // Midpoints of the brief's epoxy-relative ranges: width 55-70%,
    // depth 45-60%, height 35-50%. Resolves to ~94 x 21 x 53 mm, which
    // keeps the requested ~2.0 : 1.2 : 0.55 feel (width > depth > height).
    private const float Width = EpoxyWidth * 0.625f;
    private const float Depth = EpoxyDepth * 0.525f;
    private const float Height = EpoxyHeight * 0.425f;

    // Rounded plan-view corners and a softened (quarter-round) top edge;
    // bottom stays flat and sharp so it sits flush.
    private const float CornerRadius = Depth * 0.22f;
    private const float TopRound = Height * 0.22f;
    private const int CornerSegments = 8;
    private const int TopRoundSteps = 4;

    [MenuItem("ASRS/Build Item_MagnetStyleBlock Prop")]
    public static void Build()
    {
        Mesh mesh = SaveMeshAsset(BuildRoundedBlockMesh(), MeshPath);
        Material material = BuildMaterial();

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"Item_MagnetStyleBlock_Builder: '{PrefabPath}' already exists — regenerated its mesh and material in place; the prefab itself is untouched.");
            return;
        }

        // Root matches Item_Epoxy_Block: pivot at the object's center (the
        // mesh spans -Height/2..+Height/2), scale 1,1,1, collider/rigidbody/
        // script on the root, visuals under a child.
        GameObject root = new GameObject("Item_MagnetStyleBlock");

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        GameObject body = new GameObject("Body");
        body.transform.SetParent(visual.transform, false);
        body.AddComponent<MeshFilter>().sharedMesh = mesh;
        body.AddComponent<MeshRenderer>().sharedMaterial = material;

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = Vector3.zero;
        collider.size = new Vector3(Width, Height, Depth);

        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.angularDamping = 0.05f;

        root.AddComponent<Item_MagnetStyleBlock>();

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
        Debug.Log($"Item_MagnetStyleBlock_Builder: built '{PrefabPath}' ({Width * 1000:F1} x {Height * 1000:F1} x {Depth * 1000:F1} mm, W x H x D) and dropped an instance in front of the Scene view camera.");
    }

    // ------------------------------------------------------------------
    // Rounded block: a rounded-rectangle outline extruded upward, with
    // straight vertical sides, a quarter-round softened top edge, and flat
    // top/bottom caps. Every normal is set analytically (unit length by
    // construction) — never by normalizing a cross product, which Unity
    // zeroes for tiny faces and renders black.
    // ------------------------------------------------------------------

    private static Mesh BuildRoundedBlockMesh()
    {
        float hw = Width * 0.5f;
        float hd = Depth * 0.5f;
        float r = Mathf.Min(CornerRadius, hw * 0.9f, hd * 0.9f);
        float b = Mathf.Min(TopRound, Height * 0.45f, r * 0.9f);
        float yBottom = -Height * 0.5f;
        float yTop = Height * 0.5f;

        // Outline around the rounded rectangle: corner centers + outward
        // directions. Straight sides fall between consecutive corners.
        var centers = new List<Vector2>();
        var dirs = new List<Vector2>();
        Vector2[] cornerCenters =
        {
            new Vector2(hw - r, hd - r),
            new Vector2(-hw + r, hd - r),
            new Vector2(-hw + r, -hd + r),
            new Vector2(hw - r, -hd + r)
        };
        for (int c = 0; c < 4; c++)
        {
            for (int s = 0; s <= CornerSegments; s++)
            {
                float a = (c * 90f + s * 90f / CornerSegments) * Mathf.Deg2Rad;
                centers.Add(cornerCenters[c]);
                dirs.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)));
            }
        }
        int m = centers.Count;

        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        Vector3 OutlinePoint(int i, float inset, float y) =>
            new Vector3(centers[i].x + dirs[i].x * (r - inset), y, centers[i].y + dirs[i].y * (r - inset));

        void AddTri(int i0, int i1, int i2, Vector3 expectedNormal)
        {
            Vector3 cross = Vector3.Cross(verts[i1] - verts[i0], verts[i2] - verts[i0]);
            if (Vector3.Dot(cross, expectedNormal) >= 0f) { tris.Add(i0); tris.Add(i1); tris.Add(i2); }
            else { tris.Add(i0); tris.Add(i2); tris.Add(i1); }
        }

        // Side rings, bottom to top: the straight wall, then the
        // quarter-round softening into the top face.
        var ringStarts = new List<int>();
        void AddRing(float inset, float y, float theta)
        {
            ringStarts.Add(verts.Count);
            for (int i = 0; i < m; i++)
            {
                verts.Add(OutlinePoint(i, inset, y));
                norms.Add(new Vector3(dirs[i].x * Mathf.Cos(theta), Mathf.Sin(theta), dirs[i].y * Mathf.Cos(theta)));
                uvs.Add(new Vector2(i / (float)m, (y - yBottom) / Height));
            }
        }

        AddRing(0f, yBottom, 0f);
        AddRing(0f, yTop - b, 0f);
        for (int j = 1; j <= TopRoundSteps; j++)
        {
            float theta = j / (float)TopRoundSteps * Mathf.PI * 0.5f;
            AddRing(b * (1f - Mathf.Cos(theta)), yTop - b + b * Mathf.Sin(theta), theta);
        }

        for (int ring = 0; ring < ringStarts.Count - 1; ring++)
        {
            int lo = ringStarts[ring];
            int hi = ringStarts[ring + 1];
            for (int i = 0; i < m; i++)
            {
                int n = (i + 1) % m;
                Vector3 expected = norms[lo + i] + norms[hi + i];
                AddTri(lo + i, lo + n, hi + n, expected);
                AddTri(lo + i, hi + n, hi + i, expected);
            }
        }

        // Flat caps with their own vertices, so the bottom edge stays crisp.
        void AddCap(float inset, float y, Vector3 normal)
        {
            int center = verts.Count;
            verts.Add(new Vector3(0f, y, 0f));
            norms.Add(normal);
            uvs.Add(new Vector2(0.5f, 0.5f));

            int start = verts.Count;
            for (int i = 0; i < m; i++)
            {
                Vector3 p = OutlinePoint(i, inset, y);
                verts.Add(p);
                norms.Add(normal);
                uvs.Add(new Vector2(p.x / Width + 0.5f, p.z / Depth + 0.5f));
            }

            for (int i = 0; i < m; i++)
                AddTri(center, start + i, start + (i + 1) % m, normal);
        }

        AddCap(0f, yBottom, Vector3.down);
        AddCap(b, yTop, Vector3.up);

        Mesh mesh = new Mesh { name = "Item_MagnetStyleBlock_Body" };
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    // Returns the persistent asset, so the prefab always references the
    // saved mesh even when an existing one is overwritten in place.
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

        // #232323 dark charcoal, molded-polymer / rubber-coated look:
        // Metallic 0.05, Smoothness 0.22 (within 0.0-0.15 / 0.15-0.30) —
        // satin at most, never glossy or metal-looking.
        Color charcoal = new Color(35f / 255f, 35f / 255f, 35f / 255f, 1f);
        if (material.HasProperty("_Color")) material.SetColor("_Color", charcoal);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", charcoal);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.05f);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.22f);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.22f);

        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }
}
