using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// One-shot generator for Item_MetalCylinder — a short, stepped, machined-
// aluminum-puck prop that behaves exactly like Item_Epoxy_Block within the
// ASRS system (same script pattern, same collider/rigidbody setup, same
// centered pivot convention) but with different visual geometry/material.
// Does NOT touch Item_Epoxy_Block itself — only reads its real dimensions
// as the sizing reference.
public static class Item_MetalCylinder_Builder
{
    private const string MeshPathMain = "Assets/Meshes/Generated/Item_MetalCylinder_Main.asset";
    private const string MeshPathTop = "Assets/Meshes/Generated/Item_MetalCylinder_Top.asset";
    private const string MaterialPath = "Assets/Materials/Mat_MachinedAluminum.mat";
    private const string PrefabPath = "Assets/Game_Objects/Item/Item_MetalCylinder.prefab";

    // Item_Epoxy_Block's own BoxCollider size (Assets/Game_Objects/Item/
    // Item_Epoxy_Block.prefab) — the required scale reference. Width/Depth
    // = X, Height = Y.
    private const float EpoxyWidth = 0.0015f;
    private const float EpoxyHeight = 0.0005f;

    // Percent-of-epoxy targets from the brief (midpoints of the given
    // ranges): diameter 70-80% of epoxy width, total height 60-75% of
    // epoxy height. These two, not the brief's suggested absolute
    // dimensions (0.080/0.035, sized for a normal human-scale prop), are
    // what actually apply here — Item_Epoxy_Block is explicitly the scale
    // reference, and everything else in this project's ASRS items lives at
    // this same tiny CAD scale.
    private const float Diameter = EpoxyWidth * 0.75f;      // ~0.001125
    private const float TotalHeight = EpoxyHeight * 0.675f; // ~0.0003375

    // Internal proportions carried over from the brief's suggested
    // dimensions (0.035 main / 0.008 top -> top is ~18.6% of total height;
    // top diameter 80% of main, the midpoint of 75-85%). Applied as SHAPE
    // ratios within the epoxy-derived overall size above, rather than as
    // absolute numbers, since the brief's absolute values and the
    // epoxy-relative percentages don't resolve to the same numbers at this
    // scale (e.g. the suggested 2.2:1 main diameter:height ratio isn't
    // simultaneously achievable with both "diameter = 70-80% of epoxy
    // width" and "total height = 60-75% of epoxy height", since the epoxy
    // block itself isn't proportioned 2.2:1). Overall size tracks the
    // epoxy block; shape tracks the brief.
    private const float TopHeightFraction = 0.008f / (0.035f + 0.008f); // ~0.186
    private const float TopDiameterFraction = 0.80f;                    // midpoint of 75-85%
    private const int Segments = 32;

    private static void BuildAndSaveMeshes(out Mesh mainMesh, out Mesh topMesh, out float mainHeight, out float topHeight, out float topDiameter)
    {
        topHeight = TotalHeight * TopHeightFraction;
        mainHeight = TotalHeight - topHeight;
        topDiameter = Diameter * TopDiameterFraction;

        float mainBevel = mainHeight * 0.2f;
        float topBevel = topHeight * 0.2f;

        mainMesh = SaveMeshAsset(BuildBeveledCylinder("Item_MetalCylinder_Main", Diameter * 0.5f, mainHeight, mainBevel, Segments), MeshPathMain);
        topMesh = SaveMeshAsset(BuildBeveledCylinder("Item_MetalCylinder_Top", topDiameter * 0.5f, topHeight, topBevel, Segments), MeshPathTop);
    }

    // One command for both cases. If the prefab already exists, only the
    // mesh and material assets are regenerated in place — the prefab
    // references those assets, so it picks up the changes while keeping any
    // hand edits made to it (e.g. root scale). The prefab itself is only
    // created, and dropped into the scene, the first time.
    [MenuItem("ASRS/Build Item_MetalCylinder Prop")]
    public static void Build()
    {
        BuildAndSaveMeshes(out Mesh mainMesh, out Mesh topMesh, out float mainHeight, out float topHeight, out float topDiameter);

        Material material = BuildMaterial();

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"Item_MetalCylinder_Builder: '{PrefabPath}' already exists — regenerated its meshes and material in place; the prefab itself (including your scale) is untouched.");
            return;
        }

        // Root: matches Item_Epoxy_Block's convention — pivot at the
        // object's own center (its BoxCollider is centered at local origin
        // with no offset), not at the bottom. Half the total height sits
        // above local Y=0, half below, so it seats on a slotted table's
        // AnchorPoint exactly the way the epoxy block does.
        GameObject root = new GameObject("Item_MetalCylinder");

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        GameObject mainGo = new GameObject("MainCylinder");
        mainGo.transform.SetParent(visual.transform, false);
        mainGo.transform.localPosition = new Vector3(0f, -TotalHeight * 0.5f, 0f);
        mainGo.AddComponent<MeshFilter>().sharedMesh = mainMesh;
        mainGo.AddComponent<MeshRenderer>().sharedMaterial = material;

        GameObject topGo = new GameObject("TopStep");
        topGo.transform.SetParent(visual.transform, false);
        topGo.transform.localPosition = new Vector3(0f, -TotalHeight * 0.5f + mainHeight, 0f);
        topGo.AddComponent<MeshFilter>().sharedMesh = topMesh;
        topGo.AddComponent<MeshRenderer>().sharedMaterial = material;

        // Same collider/rigidbody/script pattern as Item_Epoxy_Block: one
        // enabled BoxCollider centered on the root, a Rigidbody with the
        // same mass/damping, and an Item_Parent-derived script with
        // Pickup = true. A simple box (sized to the widest point) is the
        // same level of physical-shape fidelity the epoxy block itself
        // uses, not an exact mesh collider.
        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = Vector3.zero;
        collider.size = new Vector3(Diameter, TotalHeight, Diameter);

        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.angularDamping = 0.05f;

        root.AddComponent<Item_MetalCylinder>();

        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        // Drop an instance in front of the Scene view camera and frame it —
        // same as the other prop builders, so there's no hunting for it.
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
        Debug.Log($"Item_MetalCylinder_Builder: built '{PrefabPath}' — diameter {Diameter * 1000:F3}mm, total height {TotalHeight * 1000:F3}mm " +
                  $"(main {mainHeight * 1000:F3}mm + top {topHeight * 1000:F3}mm, top diameter {topDiameter * 1000:F3}mm) " +
                  $"— sized from Item_Epoxy_Block's own BoxCollider ({EpoxyWidth * 1000:F2}mm x {EpoxyHeight * 1000:F2}mm), not the brief's absolute suggested dimensions. Dropped an instance in front of the Scene view camera.");
    }

    // ------------------------------------------------------------------
    // A single beveled cylinder "part": flat bottom, straight wall, a
    // chamfered ring near the top, then a smaller flat top cap — reused
    // for both MainCylinder and TopStep. Local space: y=0 is this part's
    // own bottom, y=height is the top of its bevel.
    // ------------------------------------------------------------------

    private static Mesh BuildBeveledCylinder(string name, float radius, float height, float bevel, int segments)
    {
        float b = Mathf.Min(bevel, height * 0.45f, radius * 0.45f);
        float wallTop = height - b;
        float bevelTopRadius = radius - b;

        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        // Caps only — their exact normal is always straight up or down, so
        // use expectedNormal directly. Normalizing the cross product instead
        // silently produced (0,0,0) at this item's sub-millimeter scale
        // (Vector3.Normalize zeroes anything under 1e-5 long), which is what
        // rendered the top cap black regardless of lighting.
        void AddTri(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 expectedNormal)
        {
            Vector3 cross = Vector3.Cross(p1 - p0, p2 - p0);
            if (Vector3.Dot(cross, expectedNormal) < 0f)
                (p1, p2) = (p2, p1);
            Vector3 normal = expectedNormal.normalized;

            int start = verts.Count;
            verts.Add(p0); verts.Add(p1); verts.Add(p2);
            for (int i = 0; i < 3; i++) norms.Add(normal);
            uvs.Add(new Vector2(0.5f, 0.5f)); uvs.Add(new Vector2(1, 0)); uvs.Add(new Vector2(0, 0));
            tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
        }

        void AddQuad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,
                     Vector3 n0, Vector3 n1, Vector3 n2, Vector3 n3,
                     Vector3 expectedNormal)
        {
            Vector3 faceNormal = Vector3.Cross(p1 - p0, p2 - p0);
            bool flip = Vector3.Dot(faceNormal, expectedNormal) < 0f;

            int start = verts.Count;
            if (!flip)
            {
                verts.Add(p0); verts.Add(p1); verts.Add(p2); verts.Add(p3);
                norms.Add(n0); norms.Add(n1); norms.Add(n2); norms.Add(n3);
            }
            else
            {
                verts.Add(p0); verts.Add(p3); verts.Add(p2); verts.Add(p1);
                norms.Add(n0); norms.Add(n3); norms.Add(n2); norms.Add(n1);
            }

            uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(0, 1));
            tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
            tris.Add(start); tris.Add(start + 2); tris.Add(start + 3);
        }

        Vector3 bottomCenter = Vector3.zero;
        Vector3 topCenter = Vector3.up * height;

        for (int i = 0; i < segments; i++)
        {
            float a0 = i / (float)segments * Mathf.PI * 2f;
            float a1 = (i + 1) / (float)segments * Mathf.PI * 2f;

            Vector3 dir0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
            Vector3 dir1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));

            // Flat bottom cap.
            AddTri(bottomCenter, dir0 * radius, dir1 * radius, Vector3.down);

            // Straight wall, radius constant, from y=0 to y=wallTop.
            Vector3 wallBottom0 = dir0 * radius;
            Vector3 wallBottom1 = dir1 * radius;
            Vector3 wallTop0 = dir0 * radius + Vector3.up * wallTop;
            Vector3 wallTop1 = dir1 * radius + Vector3.up * wallTop;
            AddQuad(wallBottom0, wallBottom1, wallTop1, wallTop0, dir0, dir1, dir1, dir0, (dir0 + dir1).normalized);

            // Beveled ring: wallTop (radius) -> height (bevelTopRadius).
            Vector3 bevelTop0 = dir0 * bevelTopRadius + Vector3.up * height;
            Vector3 bevelTop1 = dir1 * bevelTopRadius + Vector3.up * height;
            Vector3 bn0 = (dir0 + Vector3.up).normalized;
            Vector3 bn1 = (dir1 + Vector3.up).normalized;
            AddQuad(wallTop0, wallTop1, bevelTop1, bevelTop0, bn0, bn1, bn1, bn0, (bn0 + bn1).normalized);

            // Flat top cap (smaller radius, after the bevel).
            AddTri(topCenter, dir1 * bevelTopRadius + Vector3.up * height, dir0 * bevelTopRadius + Vector3.up * height, Vector3.up);
        }

        Mesh mesh = new Mesh { name = name };
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    // Returns the persistent asset (not the temporary in-memory mesh), so
    // whatever references it — the prefab — points at the saved asset even
    // when an existing one was overwritten in place.
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

    // Detects the active render pipeline (URP here) instead of assuming
    // Built-in "Standard", which renders black under URP.
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

        // RGB 175,180,180 (#AFB4B4) — light neutral silver, machined
        // aluminum, not chrome. Metallic 0.75 / Smoothness 0.4: the low end
        // of the requested ranges, so flat upward-facing surfaces keep more
        // diffuse light and depend less on environment reflections (which
        // render black in a scene whose lighting hasn't been generated).
        Color aluminum = new Color(175f / 255f, 180f / 255f, 180f / 255f, 1f);
        if (material.HasProperty("_Color")) material.SetColor("_Color", aluminum);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", aluminum);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.75f);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.4f);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.4f);

        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }
}
