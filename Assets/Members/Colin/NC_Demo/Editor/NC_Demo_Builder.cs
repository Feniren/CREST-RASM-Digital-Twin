using System.IO;
using ProMill8000;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-shot deterministic build of Assets/Members/Colin/NC_Demo/Scenes/NC_Mill_Demo.unity and the
/// NC_Stock prefab. Re-run while iterating; delete once the scene is stable (repo convention for
/// one-shot tools). The mill wrapper prefab is instanced untouched — everything the demo needs
/// from it is a scene override: drive axes/limits/dependents, the vise pose, Demo_Block off.
/// </summary>
public static class NC_Demo_Builder
{
    const string Root = "Assets/Members/Colin/NC_Demo";
    const string Scene_Path = Root + "/Scenes/NC_Mill_Demo.unity";
    const string Stock_Prefab_Path = Root + "/Prefabs/NC_Stock.prefab";
    const string Mill_Prefab_Path = "Assets/Members/Colin/Training/Prefabs/IntellitekMill_Training.prefab";
    const string Table_Prefab_Path = "Assets/Game_Objects/PCTable.prefab";
    const string Carving_Materials = "Assets/Members/Colin/Carving/Materials";

    // Machine geometry in world metres with the wrapper at identity. The operator faces +X:
    // machine X = world -Z (the table's long axis), machine Y = world +X (saddle), machine Z = world +Y.
    static readonly Vector2 Spindle_Axis_XZ = new Vector2(-0.0751f, 1.9070f);
    static readonly Vector3 Stock_Size = new Vector3(0.100f, 0.050f, 0.100f);
    static readonly Vector3 M1_Block_Centre = new Vector3(-0.0953f, 1.228f, 1.924f);   // Module 1's hand-tuned block-in-vise pose
    static readonly Vector3 M1_Vice = new Vector3(-0.0433f, 1.1757f, 1.9961f);
    const float Z_Travel = 0.27f;
    const float Deepest_Cut = 0.020f;
    const float Longest_Tool = 0.040f;
    // The CAD has no cutter; the lowest spindle-nose part is where a tool holder would seat.
    // (SM_Rotating is only the motor and pulleys since the 2026-08 re-bake, 0.2 m higher.)
    const string Nose_Node = "IKX10226";

    [MenuItem("NC Demo/1 Build Scene")]
    public static void Build()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("NC Demo: exit Play mode first.");
            return;
        }
        if (SceneManager.GetActiveScene().isDirty)
        {
            Debug.LogError("NC Demo: the open scene has unsaved changes; save or discard them first.");
            return;
        }
        var mill_prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Mill_Prefab_Path);
        var table_prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Table_Prefab_Path);
        if (mill_prefab == null || table_prefab == null)
        {
            Debug.LogError("NC Demo: missing " + (mill_prefab == null ? Mill_Prefab_Path : Table_Prefab_Path));
            return;
        }

        Directory.CreateDirectory(Root + "/Prefabs");
        Directory.CreateDirectory(Root + "/Scenes");
        AssetDatabase.Refresh();

        var stock_mat = Make_Material(Carving_Materials + "/Carving_Stock.mat", new Color(0.87f, 0.80f, 0.62f), 0f, 0.35f);
        var flat_mat = Make_Material(Carving_Materials + "/Carving_Tool_Flat.mat", new Color(0.62f, 0.64f, 0.68f), 0.8f, 0.7f);
        var ball_mat = Make_Material(Carving_Materials + "/Carving_Tool_Ball.mat", new Color(0.35f, 0.55f, 0.90f), 0.8f, 0.7f);
        var drill_mat = Make_Material(Carving_Materials + "/Carving_Tool_Drill.mat", new Color(0.85f, 0.35f, 0.30f), 0.8f, 0.7f);
        var stand_mat = Make_Material(Carving_Materials + "/Carving_Stand.mat", new Color(0.30f, 0.30f, 0.32f), 0.2f, 0.3f);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.localScale = new Vector3(2f, 1f, 2f);
        floor.GetComponent<MeshRenderer>().sharedMaterial = stand_mat;

        // The mill, exactly as Module 1 places it.
        var mill = (GameObject)PrefabUtility.InstantiatePrefab(mill_prefab, scene);
        mill.name = "IntellitekMill_Training";
        mill.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        var x_drive = Drive(mill.transform, "WB_XAxis_Drive");
        var y_drive = Drive(mill.transform, "WB_YAxis_Drive");
        var z_drive = Drive(mill.transform, "SpindleMotor");
        var vice = Find(mill.transform, "Vice");
        var demo_block = Find(mill.transform, "Demo_Block");
        var nose = Find(mill.transform, Nose_Node);
        var doors = mill.GetComponentInChildren<Mill_Doors_Physics>(true);
        if (x_drive == null || y_drive == null || z_drive == null || vice == null || demo_block == null || nose == null || doors == null)
        {
            Debug.LogError("NC Demo: the mill prefab no longer has the expected nodes (WB_XAxis_Drive, WB_YAxis_Drive, SpindleMotor, Vice, Demo_Block, " + Nose_Node + ", Mill_Doors_Physics).");
            return;
        }

        // Stock centred under the spindle axis at zero offsets; the vise keeps M1's relation to the block.
        Vector3 shift = new Vector3(Spindle_Axis_XZ.x - M1_Block_Centre.x, 0f, Spindle_Axis_XZ.y - M1_Block_Centre.z);
        vice.localPosition = M1_Vice + shift;
        demo_block.gameObject.SetActive(false);
        float block_bottom = M1_Block_Centre.y - Stock_Size.y * 0.5f;
        Bounds nose_bounds = Bounds_Of(nose);
        float nose_y = nose_bounds.min.y;

        var stock_mount = new GameObject("Stock_Mount").transform;
        stock_mount.position = new Vector3(Spindle_Axis_XZ.x - Stock_Size.x * 0.5f, block_bottom, Spindle_Axis_XZ.y - Stock_Size.z * 0.5f);
        var tool_holder = new GameObject("Tool_Holder").transform;
        tool_holder.position = new Vector3(Spindle_Axis_XZ.x, nose_y, Spindle_Axis_XZ.y);

        Configure_Drive(x_drive, MovementAxis.Z, -0.14f, 0.14f, stock_mount, vice);                       // machine X: the table runs along world Z
        Configure_Drive(y_drive, MovementAxis.X, -0.076f, 0.076f, x_drive.transform, stock_mount, vice);   // machine Y: the saddle carries the table
        Configure_Drive(z_drive, MovementAxis.Y, -Z_Travel, 0f, tool_holder);                             // machine Z: the spindle carries the tool

        // Tool rack: four cylinders standing tip-down in slots on a stand by the operator's right.
        var rack_go = new GameObject("Tool_Rack");
        rack_go.transform.position = new Vector3(-0.75f, 0f, 1.35f);
        Make_Block("Post", rack_go.transform, new Vector3(-0.75f, 0.44f, 1.35f), new Vector3(0.04f, 0.88f, 0.04f), stand_mat);
        Make_Block("Top", rack_go.transform, new Vector3(-0.75f, 0.895f, 1.35f), new Vector3(0.34f, 0.03f, 0.12f), stand_mat);
        var slots = new Transform[4];
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = new GameObject("Slot_" + (i + 1)).transform;
            slots[i].SetParent(rack_go.transform, false);
            slots[i].position = new Vector3(-0.75f + (i - 1.5f) * 0.08f, 0.91f, 1.35f);
        }
        var tools = new[]
        {
            Make_Tool("Tool_1_Flat_10", slots[0], 0.010f, 0.040f, Tool_Profile.Tip.Flat, flat_mat),
            Make_Tool("Tool_2_Flat_6", slots[1], 0.006f, 0.035f, Tool_Profile.Tip.Flat, flat_mat),
            Make_Tool("Tool_3_Ball_6", slots[2], 0.006f, 0.035f, Tool_Profile.Tip.Ball, ball_mat),
            Make_Tool("Tool_4_Drill_3", slots[3], 0.003f, 0.040f, Tool_Profile.Tip.Drill, drill_mat),
        };
        var rack = rack_go.AddComponent<NC_Tool_Rack>();
        rack.Tools = tools;
        rack.Slots = slots;

        // Output table to the operator's front-left; the grid origin sits on the measured top.
        var table_go = (GameObject)PrefabUtility.InstantiatePrefab(table_prefab, scene);
        table_go.name = "Output_Table";
        table_go.transform.SetPositionAndRotation(new Vector3(-1.0f, 0.714f, 2.45f), Quaternion.Euler(-90f, 0f, 0f));
        Bounds tb = Bounds_Of(table_go.transform);
        var table_top = new GameObject("Output_Table_Top").transform;
        table_top.position = new Vector3(tb.center.x - 0.325f, tb.max.y + 0.001f, tb.center.z - 0.155f);
        var table = table_top.gameObject.AddComponent<NC_Output_Table>();
        table.Table_Top = table_top;

        var views_root = new GameObject("Views").transform;
        Vector3 stock_centre = new Vector3(Spindle_Axis_XZ.x, block_bottom + Stock_Size.y * 0.6f, Spindle_Axis_XZ.y);
        var views = new[]
        {
            Make_View("View_1_Operator", views_root, new Vector3(-1.7f, 1.7f, 1.9f), stock_centre),
            Make_View("View_2_Vise", views_root, new Vector3(-0.30f, 1.55f, 1.72f), stock_centre),
            Make_View("View_3_Table", views_root, new Vector3(tb.center.x - 0.85f, tb.max.y + 0.75f, tb.center.z), new Vector3(tb.center.x, tb.max.y, tb.center.z)),
        };
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.SetPositionAndRotation(views[0].position, views[0].rotation);
            cam.fieldOfView = 45f;
            cam.nearClipPlane = 0.01f;
        }

        var stock_prefab = Build_Stock_Prefab(stock_mat);

        var demo = new GameObject("NC_Demo");
        var link = demo.AddComponent<NC_Mill_Link>();
        link.Drive_World_X = y_drive;
        link.Drive_World_Y = z_drive;
        link.Drive_World_Z = x_drive;
        link.Tool_Holder = tool_holder;
        var runner = demo.AddComponent<NC_Runner>();
        runner.Link = link;
        runner.Rack = rack;
        var controller = demo.AddComponent<NC_Demo_Controller>();
        controller.Runner = runner;
        controller.Link = link;
        controller.Rack = rack;
        controller.Table = table;
        controller.Doors = doors;
        controller.Stock_Prefab = stock_prefab;
        controller.Stock_Mount = stock_mount;
        controller.Views = views;

        // Reachability: the longest tool's tip must clear the stock top at Z home and still reach the deepest cut.
        float gap = (nose_y - Longest_Tool) - (block_bottom + Stock_Size.y);
        if (stock_mount.lossyScale != Vector3.one || tool_holder.lossyScale != Vector3.one)
            Debug.LogError("NC Demo: Stock_Mount/Tool_Holder must be unit scale.");
        if (gap <= 0f || gap + Deepest_Cut > Z_Travel)
            Debug.LogError("NC Demo: tip-to-stock gap " + (gap * 1000f).ToString("0") + " mm cannot reach a " + Deepest_Cut * 1000f + " mm cut within the " + Z_Travel * 1000f + " mm Z travel.");

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene, Scene_Path);
        Debug.Log("NC Demo: built " + Scene_Path + " and " + Stock_Prefab_Path);
        Debug.Log("NC Demo: spindle nose (" + Nose_Node + ") bottom y " + nose_y.ToString("0.0000") + " centred at x " + nose_bounds.center.x.ToString("0.0000") + " z " + nose_bounds.center.z.ToString("0.0000")
                  + " vs spindle axis " + Spindle_Axis_XZ.ToString("0.0000") + "; stock top " + (block_bottom + Stock_Size.y).ToString("0.0000") + ", longest-tool tip gap " + (gap * 1000f).ToString("0") + " mm, table top "
                  + tb.max.y.ToString("0.000") + " (" + tb.size.x.ToString("0.00") + " x " + tb.size.z.ToString("0.00") + " m)");
        Debug.Log("NC Demo: drives — WB_XAxis_Drive world Z ±140 mm (machine X), WB_YAxis_Drive world X ±76 mm (machine Y), SpindleMotor world Y -270..0 mm (machine Z)");
    }

    [MenuItem("NC Demo/8 Debug - Feed Override 1000%")]
    static void Override_1000()
    {
        var r = Runner();
        if (r != null) r.Feed_Override = 10f;
    }

    [MenuItem("NC Demo/8 Debug - Feed Override 100%")]
    static void Override_100()
    {
        var r = Runner();
        if (r != null) r.Feed_Override = 1f;
    }

    [MenuItem("NC Demo/8 Debug - Skip File")]
    static void Skip_File()
    {
        var c = Controller();
        if (c != null) c.Skip();
    }

    [MenuItem("NC Demo/8 Debug - Next View")]
    static void Next_View()
    {
        var c = Controller();
        if (c != null) c.Next_View();
    }

    static NC_Runner Runner()
    {
        var c = Controller();
        return c != null ? c.Runner : null;
    }

    static NC_Demo_Controller Controller()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("NC Demo: enter Play mode in NC_Mill_Demo first.");
            return null;
        }
        var c = Object.FindFirstObjectByType<NC_Demo_Controller>();
        if (c == null) Debug.LogError("NC Demo: no NC_Demo_Controller in the open scene.");
        return c;
    }

    static GameObject Build_Stock_Prefab(Material stock_mat)
    {
        var go = new GameObject("NC_Stock");
        var stock = go.AddComponent<Mill_Stock>();
        stock.Size = Stock_Size;
        stock.Cell = 0.001f;
        stock.Pitch = 0.001f;
        stock.Material = stock_mat;
        // Work origin at the front-left-top corner: front = -X, left = +Z (the operator faces +X).
        // Local +X = program X = world -Z; local +Z = program Y = world +X; local +Y = program Z.
        var origin = new GameObject("Work_Origin").transform;
        origin.SetParent(go.transform, false);
        origin.localPosition = new Vector3(0f, Stock_Size.y, Stock_Size.z);
        origin.localRotation = Quaternion.Euler(0f, 90f, 0f);
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, Stock_Prefab_Path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // Scene-only overrides on the nested prefab instance's AxisMovement (private fields, hence SerializedObject).
    static void Configure_Drive(AxisMovement drive, MovementAxis axis, float min, float max, params Transform[] dependents)
    {
        var so = new SerializedObject(drive);
        so.FindProperty("axis").enumValueIndex = (int)axis;
        so.FindProperty("enableLimits").boolValue = true;
        so.FindProperty("minOffset").floatValue = min;
        so.FindProperty("maxOffset").floatValue = max;
        var deps = so.FindProperty("dependents");
        deps.arraySize = dependents.Length;
        for (int i = 0; i < dependents.Length; i++) deps.GetArrayElementAtIndex(i).objectReferenceValue = dependents[i];
        so.ApplyModifiedProperties();
    }

    static AxisMovement Drive(Transform root, string name)
    {
        var t = Find(root, name);
        return t != null ? t.GetComponent<AxisMovement>() : null;
    }

    static Transform Find(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    static Bounds Bounds_Of(Transform root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        var b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    static Material Make_Material(string path, Color colour, float metallic, float smoothness)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", colour);
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Smoothness", smoothness);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static void Make_Block(string name, Transform parent, Vector3 position, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static Carving_Tool Make_Tool(string name, Transform slot, float diameter, float length, Tool_Profile.Tip tip, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(slot, false);
        var tool = go.AddComponent<Carving_Tool>();
        tool.Profile = new Tool_Profile { Diameter = diameter, Length = length, Tip_Shape = tip };
        var vis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        vis.name = "Visual";
        Object.DestroyImmediate(vis.GetComponent<Collider>());
        vis.transform.SetParent(go.transform, false);
        vis.GetComponent<MeshRenderer>().sharedMaterial = mat;
        tool.Sync_Visual();
        return tool;
    }

    static Transform Make_View(string name, Transform parent, Vector3 position, Vector3 look_at)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.position = position;
        t.LookAt(look_at);
        return t;
    }
}
