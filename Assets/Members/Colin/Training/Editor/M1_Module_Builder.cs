using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ProMill8000;
using static Training_Builder_Core;

// Module 1 — "CNC Milling: What & Why" (docs/VR_Modules/01_Module1_Plan.md).
// Content + mill-specific scene wiring only; all generic scaffolding comes from
// Training_Builder_Core. Re-run "Training/3 Build Module1 Scene" or "Training/5
// Reposition Markers" after the ProMill8000 model is replaced — markers are
// re-derived from node bounds.
public class M1_Module_Builder : ITraining_Module_Builder{
    private const string Root = "Assets/Members/Colin";
    private const string ScenesDir = Root + "/Scenes";
    private const string TrainingDir = Root + "/Training";
    public const string PrefabsDir = TrainingDir + "/Prefabs";
    private const string LessonsDir = TrainingDir + "/Lessons";
    private const string LessonPath = LessonsDir + "/M1_Lesson.asset";
    public const string Module1ScenePath = ScenesDir + "/Module1_Overview.unity";

    private const string MillPrefabPath = "Assets/Prefabs/reconstructedPM8000.prefab";
    private const string VicePrefabPath = "Assets/Game_Objects/Extra/DualAxisVice.prefab";
    private const string BlueGlassPath = "Assets/Materials/Blue_Glass.mat";

    // Same axis-aligned orientation as the DigitalTwin instance (FBX is Z-up):
    // machine X (table) = world Z, machine Y (saddle) = world X, machine Z (spindle) = world Y.
    private static readonly Quaternion MillRotation = new Quaternion(-0.5f, -0.5f, 0.5f, 0.5f);
    private const float MillScale = 100f;

    // Stand the DualAxisVice upright: its wrapper prefab keeps the raw FBX
    // import orientation (long axis pointing up), never validated in any
    // scene. Hand-tuned against the model.
    private static readonly Quaternion ViceRotation = Quaternion.Euler(0f, 90f, 90f);

    private struct MarkerDef{
        public string Id;
        public string Display;
        public string NodePath;
    }

    // Component list follows the official Intelitek ProMill 8000 Quick Start
    // guide (34-0000-8000 Rev-C) "Machine components" plus teaching-critical
    // parts (guard door). Markers without a NodePath (vice, E-stop) use
    // hand-tuned bounds in BuildMarkers.
    private static readonly MarkerDef[] Markers = {
        new MarkerDef{ Id = "spindle_motor", Display = "Spindle Motor", NodePath = "SpindleBase/SpindleMotor/SM_Rotating" },
        new MarkerDef{ Id = "spindle_head", Display = "Spindle Head", NodePath = "SpindleBase/SpindleMotor/SM_Static" },
        new MarkerDef{ Id = "vice", Display = "Vise" },
        new MarkerDef{ Id = "guard_door", Display = "Guard Door", NodePath = "ProMill8000Body/PB_Column_Structure/PB_Column_Static/doors" },
        new MarkerDef{ Id = "emergency_stop", Display = "Emergency Stop Button", NodePath = "ProMill8000Body/PB_Knee_Table/PB_Knee_Static/\\X2\\59276025505C\\X0\\" },
        new MarkerDef{ Id = "power_on", Display = "Power On", NodePath = "ProMill8000Body/PB_Knee_Table/PB_Knee_Static/kaig" },
        new MarkerDef{ Id = "door_unlock", Display = "Door Unlock", NodePath = "ProMill8000Body/PB_Knee_Table/PB_Knee_Static/\\X2\\630994AE\\X0\\" },
        new MarkerDef{ Id = "electronics_cabinet", Display = "Electronics Cabinet", NodePath = "ProMill8000Body/PB_Knee_Table" },
    };

    public int Order => 0;

    public void Build(){
        BuildLessonAsset();
        BuildDemoBlockPrefab();
        BuildModule1Scene();
    }

    // ------------------------------------------------------------------
    // Lesson content
    // ------------------------------------------------------------------

    private static void BuildLessonAsset(){
        Lesson_Definition def = LoadOrCreateLesson(LessonPath);
        def.Module_Id = "M1";
        def.Scene_Name = "Module1_Overview";
        def.Display_Name = "M1 — CNC Milling: What & Why";
        def.Quiz_Pass_Threshold = 4;
        def.Steps = BuildM1Steps();
        EditorUtility.SetDirty(def);
    }

    private static List<Lesson_Step> BuildM1Steps(){
        List<Lesson_Step> steps = new List<Lesson_Step>{
            Info("intro_role", "Welcome to the ProMill 8000 — a 3-axis CNC milling machine, and the machining station of the Intelitek SmartCIM 4.0 cell. Conveyor pallets deliver raw stock and the robot arm loads it into the mill for cutting.\n\nPress Continue."),
            Info("intro_operations", "A mill cuts by feeding a rotating tool into the workpiece. The five core operations are:\n\nFACE — flatten the top surface\nPOCKET — hollow out a recess\nCONTOUR — cut an outside profile\nDRILL — plunge holes\nSLOT — cut channels\n\nPress Continue."),
            Info("intro_axes", "The mill moves in three axes. Use the right-hand rule: thumb = +X (table left–right), index = +Y (table fore–aft), middle = +Z (spindle up–down).\n\nPress Continue."),
            Select("spindle_motor", "Spindle Motor", "the motor on top of the head — it spins the spindle and cutting tool"),
            Select("spindle_head", "Spindle Head", "it holds the rotating spindle and cutting tool, and moves up and down in Z"),
            Select("vice", "Vise", "it clamps the workpiece to the table during cutting"),
            Select("guard_door", "Guard Door", "the perspex shield that must be closed while the spindle is cutting — opened with the Door Open button in CNCBase"),
            Select("emergency_stop", "Emergency Stop Button", "the red button on the front — press it to immediately stop the machine in an emergency"),
            Select("power_on", "Power Switch", "energizes the machine before any operation — always the first control you use at start-up", false),
            Select("door_unlock", "Door Unlock", "releases the guard-door interlock so the doors can slide open for loading and unloading", false),
            Select("electronics_cabinet", "Electronics Cabinet", "the lower cabinet housing the machine's drive and control electronics — opened with the electrical panel keys"),
            Info("intro_accessories", "Not shown in this model: the machine also has a right-side connection panel (power, Ethernet, coolant, jog pendant ports) and optional accessories — a handheld jog pendant and a monitor stand.\n\nPress Continue."),
            new Lesson_Step{
                Step_Id = "axis_demo",
                Kind = Lesson_Step_Kind.Axis_Demo,
                Prompt_Text = "Axis demo — watch the machine move:\nX: table travels 280 mm left–right\nY: table travels 152 mm fore–aft\nZ: spindle travels 270 mm up–down"
            },
            new Lesson_Step{
                Step_Id = "milling_demo",
                Kind = Lesson_Step_Kind.Milling_Demo,
                Prompt_Text = "Milling demo — the guard doors open, the spindle plunges into the perspex block, and the table cuts a square pocket: plunge → square pass → retract."
            },
            Info("guided_done", "Guided tour complete!\n\nNext: the practice quiz. Labels and highlights are now off — identify each component from its name.\n\nYou need at least 4 of 6 to pass. Press Continue.")
        };

        return steps;
    }

    private static void BuildDemoBlockPrefab(){
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = "Demo_Block";
        block.transform.localScale = new Vector3(0.1f, 0.05f, 0.1f);
        Material blueGlass = AssetDatabase.LoadAssetAtPath<Material>(BlueGlassPath);

        if (blueGlass != null)
            block.GetComponent<MeshRenderer>().sharedMaterial = blueGlass;

        PrefabUtility.SaveAsPrefabAsset(block, PrefabsDir + "/Demo_Block.prefab");
        Object.DestroyImmediate(block);
    }

    // ------------------------------------------------------------------
    // Module 1 scene
    // ------------------------------------------------------------------

    [MenuItem("Training/3 Build Module1 Scene")]
    public static void BuildModule1SceneMenu(){
        new M1_Module_Builder().Build();
    }

    private static void BuildModule1Scene(){
        var scene = NewModuleScene();

        // --- Mill wrapper ---
        GameObject millPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MillPrefabPath);
        if (millPrefab == null){ Debug.LogError($"M1_Module_Builder: mill prefab missing at {MillPrefabPath}"); return; }

        GameObject wrapper = new GameObject("PM8000_Training");
        GameObject mill = (GameObject)PrefabUtility.InstantiatePrefab(millPrefab);
        mill.transform.SetParent(wrapper.transform, false);
        mill.transform.localRotation = MillRotation;
        mill.transform.localScale = Vector3.one * MillScale;

        // Ground the mill and center it in front of the player spawn
        Bounds millBounds = RendererBounds(mill.transform);
        mill.transform.position += new Vector3(-millBounds.center.x, -millBounds.min.y, 1.9f - millBounds.center.z);
        millBounds = RendererBounds(mill.transform);

        // WB_Clamp removed from the cell — the vise provides workholding (validated
        // in-editor). Removed after grounding so the mill position is unchanged.
        Transform clamp = FindChild(mill.transform, "WB_Clamp");
        if (clamp != null)
            Object.DestroyImmediate(clamp.gameObject);

        // --- Axis + milling components ---
        Transform doors = FindChild(mill.transform, "doors");
        Transform door1 = FindChild(mill.transform, "Door1");
        Transform door2 = FindChild(mill.transform, "Door2");

        if (doors == null || door1 == null || door2 == null){
            Debug.LogError("M1_Module_Builder: mill door nodes not found — check model hierarchy names.");
            return;
        }

        // The prefab already carries the tested mill rig: MillingAnimation on
        // MillController driving WB_XAxis_Drive (machine X = world X),
        // WB_YAxis_Drive (machine Y = world Z, carries the X stage as a
        // dependent) and SpindleMotor (machine Z = world Y). Reuse it instead
        // of duplicating axis components on the static assemblies.
        MillingAnimation milling = mill.GetComponentInChildren<MillingAnimation>();
        if (milling == null){ Debug.LogError("M1_Module_Builder: no MillingAnimation in the mill prefab — check the prefab rig."); return; }

        SerializedObject millingSO = new SerializedObject(milling);
        AxisMovement tableAxis = millingSO.FindProperty("worktableX").objectReferenceValue as AxisMovement;
        AxisMovement saddleAxis = millingSO.FindProperty("worktableZ").objectReferenceValue as AxisMovement;
        AxisMovement spindleAxis = millingSO.FindProperty("spindleY").objectReferenceValue as AxisMovement;

        if (tableAxis == null || saddleAxis == null || spindleAxis == null){
            Debug.LogError("M1_Module_Builder: MillingAnimation axes not wired in the mill prefab — check the prefab rig.");
            return;
        }

        // Training-only overrides: slow the axes for instruction and clamp to
        // the real travels (X 280 mm, Y 152 mm, Z 270 mm at model scale).
        ConfigureAxis(tableAxis, 0.05f, -0.14f, 0.14f);
        ConfigureAxis(saddleAxis, 0.05f, -0.076f, 0.076f);
        ConfigureAxis(spindleAxis, 0.1f, -0.27f, 0f);

        AxisMovement door1Axis = AddAxis(door1.gameObject, MovementAxis.Z, 1f, 0f, 0f, false);
        AxisMovement door2Axis = AddAxis(door2.gameObject, MovementAxis.Z, 1f, 0f, 0f, false);

        Item_Mill_Doors doorsScript = doors.gameObject.AddComponent<Item_Mill_Doors>();
        SetRef(doorsScript, "leftDoor", door2Axis);
        SetRef(doorsScript, "rightDoor", door1Axis);
        SetVal(doorsScript, "slideDistance", 0.25f);

        // --- Vice + demo block on the table ---
        // Parented to the unscaled wrapper (not the 100x rotated mill subtree) and
        // registered as axis dependents so they ride table moves.
        Transform xStage = tableAxis.transform; // WB_XAxis_Drive — the stage workpieces mount on
        Bounds stageBounds = RendererBounds(xStage);
        Vector3 tableCenter = new Vector3(stageBounds.center.x, stageBounds.max.y, stageBounds.center.z);
        GameObject vicePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VicePrefabPath);
        GameObject vice = null;

        if (vicePrefab != null){
            vice = (GameObject)PrefabUtility.InstantiatePrefab(vicePrefab);
            vice.name = "Vice";
            vice.transform.SetParent(wrapper.transform, false);
            vice.transform.rotation = ViceRotation; // stand the raw FBX import upright
            Bounds viceBounds = RendererBounds(vice.transform);
            vice.transform.position += tableCenter + new Vector3(0f, 0f, -0.2f) - new Vector3(viceBounds.center.x, viceBounds.min.y, viceBounds.center.z);
        }

        GameObject blockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsDir + "/Demo_Block.prefab");
        GameObject block = (GameObject)PrefabUtility.InstantiatePrefab(blockPrefab);
        block.transform.SetParent(wrapper.transform, false);
        // Under the spindle so the milling-demo plunge lands on the block.
        Bounds spindleBounds = RendererBounds(spindleAxis.transform);
        block.transform.position = new Vector3(spindleBounds.center.x, tableCenter.y + 0.025f, spindleBounds.center.z);

        // Hand-tuned placement captured in-editor and locked so Training/3 rebuilds
        // keep it (the dynamic placement above is the fallback). Vise reseated upright
        // and reoriented; demo block reseated on the table.
        if (vice != null){
            vice.transform.localPosition = new Vector3(-0.0589f, 1.1702f, 1.9683f);
            vice.transform.localRotation = Quaternion.Euler(0f, 0f, 270f);
        }
        block.transform.localPosition = new Vector3(-0.0859f, 1.2365f, 1.9087f);

        var riders = new List<Object>();
        if (vice != null) riders.Add(vice.transform);
        riders.Add(block.transform);
        // Append — saddleAxis.dependents already carries the X stage.
        AppendRefArray(tableAxis, "dependents", riders);
        AppendRefArray(saddleAxis, "dependents", riders);

        // --- Save the wired mill as the reusable training prefab ---
        PrefabUtility.SaveAsPrefabAssetAndConnect(wrapper, PrefabsDir + "/PM8000_Training.prefab", InteractionMode.AutomatedAction);

        // --- Markers ---
        GameObject markerGroup = new GameObject("Component_Markers");
        BuildMarkers(markerGroup.transform, mill.transform, vice != null ? vice.transform : null);

        // --- Axis indicators ---
        GameObject indicators = new GameObject("Axis_Indicators");
        float indicatorY = millBounds.max.y * 0.8f;
        float frontZ = millBounds.min.z - 0.2f;
        GameObject xInd = CreateIndicator(indicators.transform, "X — table left / right", new Color(1f, 0.3f, 0.3f), new Vector3(-0.7f, indicatorY, frontZ));
        GameObject yInd = CreateIndicator(indicators.transform, "Y — table fore / aft", new Color(0.3f, 1f, 0.4f), new Vector3(0f, indicatorY, frontZ));
        GameObject zInd = CreateIndicator(indicators.transform, "Z — spindle up / down", new Color(0.35f, 0.55f, 1f), new Vector3(0.7f, indicatorY, frontZ));

        // --- Prompt / results / sequencer scaffold ---
        Module_Scaffold scaffold = BuildLessonScaffold(new Vector3(-1.1f, 1.45f, 1.2f), Quaternion.identity, new Vector3(0f, 1.5f, 1.0f));
        if (scaffold == null) return;
        GameObject manager = scaffold.Manager;
        Lesson_Sequencer sequencer = scaffold.Sequencer;

        Marker_Registry registry = manager.AddComponent<Marker_Registry>();
        Part_Highlighter highlighter = manager.AddComponent<Part_Highlighter>();
        Mill_Demo_Controller demoController = manager.AddComponent<Mill_Demo_Controller>();

        SetRef(registry, "MarkersRoot", markerGroup.transform);
        SetRef(registry, "Sequencer", sequencer);

        SetRef(highlighter, "Sequencer", sequencer);
        SetRef(highlighter, "Registry", registry);

        SetRef(demoController, "WorktableX", tableAxis);
        SetRef(demoController, "SaddleY", saddleAxis);
        SetRef(demoController, "SpindleZ", spindleAxis);
        SetRef(demoController, "Milling", milling);
        SetRef(demoController, "Doors", doorsScript);
        SetRef(demoController, "XIndicator", xInd);
        SetRef(demoController, "YIndicator", yInd);
        SetRef(demoController, "ZIndicator", zInd);

        Door_Click_Toggle doorToggle = manager.AddComponent<Door_Click_Toggle>();
        SetRef(doorToggle, "DoorMarker", FindMarker(markerGroup, "guard_door"));
        SetRef(doorToggle, "Doors", doorsScript);
        SetRef(doorToggle, "Sequencer", sequencer);

        // --- Door Unlock button: slide the mill doors via a second Door_Click_Toggle ---
        Door_Click_Toggle unlockToggle = manager.AddComponent<Door_Click_Toggle>();
        SetRef(unlockToggle, "DoorMarker", FindMarker(markerGroup, "door_unlock"));
        SetRef(unlockToggle, "Doors", doorsScript);
        SetRef(unlockToggle, "Sequencer", sequencer);

        // --- Power On + Emergency Stop buttons: visible state cue on the real mill part ---
        WireStateToggle(manager, markerGroup, mill.transform, sequencer, "power_on",
            "ProMill8000Body/PB_Knee_Table/PB_Knee_Static/kaig", "Power On",
            new Color(0.2f, 1f, 0.3f, 1f));
        WireStateToggle(manager, markerGroup, mill.transform, sequencer, "emergency_stop",
            "ProMill8000Body/PB_Knee_Table/PB_Knee_Static/\\X2\\59276025505C\\X0\\", "Emergency Stop",
            new Color(1f, 0.15f, 0.15f, 1f));

        SetRef(sequencer, "Registry", registry);
        SetRef(sequencer, "DemoController", demoController);

        EditorSceneManager.SaveScene(scene, Module1ScenePath);
        RegisterModule(AssetDatabase.LoadAssetAtPath<Lesson_Definition>(LessonPath), Module1ScenePath);
        Debug.Log($"M1_Module_Builder: Module1 scene built. Mill bounds: center {millBounds.center}, size {millBounds.size}");
    }

    // ------------------------------------------------------------------
    // Marker placement (re-run after a model swap)
    // ------------------------------------------------------------------

    [MenuItem("Training/5 Reposition Markers In Open Scene")]
    public static void RepositionMarkersInOpenScene(){
        GameObject wrapper = GameObject.Find("PM8000_Training");
        GameObject group = GameObject.Find("Component_Markers");

        if (wrapper == null || group == null){
            Debug.LogError("M1_Module_Builder: PM8000_Training or Component_Markers not found in the open scene.");
            return;
        }

        Transform mill = wrapper.transform.GetChild(0);
        Transform vice = FindChild(wrapper.transform, "Vice");

        foreach (Transform child in new List<Transform>(group.transform.GetComponentsInChildren<Transform>()))
            if (child != null && child.parent == group.transform)
                Object.DestroyImmediate(child.gameObject);

        BuildMarkers(group.transform, mill, vice);

        // BuildMarkers recreated the marker objects, so rewire the serialized
        // guard-door reference that Build Module1 Scene normally sets.
        Door_Click_Toggle doorToggle = Object.FindFirstObjectByType<Door_Click_Toggle>();
        if (doorToggle != null)
            SetRef(doorToggle, "DoorMarker", FindMarker(group, "guard_door"));

        EditorSceneManager.MarkSceneDirty(group.scene);
        Debug.Log("M1_Module_Builder: markers repositioned from current model bounds.");
    }

    private static void BuildMarkers(Transform group, Transform mill, Transform vice){
        foreach (MarkerDef def in Markers){
            Bounds bounds;

            if (def.Id == "vice" && vice != null){
                bounds = RendererBounds(vice);
            }
            else{
                Transform node = def.NodePath != null ? mill.Find(def.NodePath) : null;

                if (node == null){
                    Debug.LogWarning($"M1_Module_Builder: node not found for marker '{def.Id}' ({def.NodePath}) — skipped.");
                    continue;
                }

                bounds = RendererBounds(node);
            }

            // Hand-tuned hitboxes captured in-editor and pinned to absolute world
            // bounds — raw node bounds would overlap or over-cover, so Training/5
            // would otherwise undo the manual placement. Re-bake from the scene if
            // the mill is ever re-placed.
            //  - spindle motor (SM_Rotating) / head (SM_Static): nested geometry,
            //    boxes shrunk, split apart vertically, then nudged in X by hand.
            //  - electronics cabinet (PB_Knee_Table): tightened around the panel.
            if (def.Id == "spindle_motor")
                bounds = new Bounds(new Vector3(-0.0748f, 1.7883f, 1.8837f), new Vector3(0.16f, 0.16f, 0.14f));
            else if (def.Id == "spindle_head")
                bounds = new Bounds(new Vector3(-0.0662f, 1.5937f, 1.9063f), new Vector3(0.16f, 0.16f, 0.14f));
            else if (def.Id == "electronics_cabinet")
                bounds = new Bounds(new Vector3(0.082f, 0.5079f, 1.961f), new Vector3(0.7211f, 0.8047f, 1.1194f));

            BuildComponentMarker(group, def.Id, def.Display, bounds);
        }
    }

    // ------------------------------------------------------------------
    // Mill axis helpers (ProMill8000 components)
    // ------------------------------------------------------------------

    private static AxisMovement AddAxis(GameObject target, MovementAxis axis, float speed, float min, float max, bool useLimits = true){
        AxisMovement movement = target.AddComponent<AxisMovement>();
        SerializedObject so = new SerializedObject(movement);
        so.FindProperty("axis").enumValueIndex = (int)axis;
        so.FindProperty("speed").floatValue = speed;
        so.FindProperty("enableLimits").boolValue = useLimits;
        so.FindProperty("minOffset").floatValue = min;
        so.FindProperty("maxOffset").floatValue = max;
        so.ApplyModifiedPropertiesWithoutUndo();
        return movement;
    }

    private static void ConfigureAxis(AxisMovement movement, float speed, float min, float max){
        SerializedObject so = new SerializedObject(movement);
        so.FindProperty("speed").floatValue = speed;
        so.FindProperty("enableLimits").boolValue = true;
        so.FindProperty("minOffset").floatValue = min;
        so.FindProperty("maxOffset").floatValue = max;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ------------------------------------------------------------------
    // Play-mode debug drivers (mill-specific; generic ones in Training_Debug)
    // ------------------------------------------------------------------

    [MenuItem("Training/8 Debug - Begin Practice Directly")]
    public static void DebugBeginPractice(){
        if (!Application.isPlaying) return;
        Lesson_Sequencer sequencer = Object.FindFirstObjectByType<Lesson_Sequencer>();
        Lesson_Definition def = AssetDatabase.LoadAssetAtPath<Lesson_Definition>(LessonPath);
        if (sequencer == null){ Debug.LogError("M1_Module_Builder: no Lesson_Sequencer (load Module 1 first)."); return; }
        sequencer.Begin(def, Lesson_Mode.Practice);
    }

    [MenuItem("Training/8 Debug - Click Guard Door")]
    public static void DebugClickGuardDoor(){
        if (!Application.isPlaying) return;
        Door_Click_Toggle toggle = Object.FindFirstObjectByType<Door_Click_Toggle>();
        Marker_Registry registry = Object.FindFirstObjectByType<Marker_Registry>();
        if (toggle == null || registry == null){ Debug.LogError("M1_Module_Builder: door toggle or registry missing."); return; }
        toggle.On_Door_Selected(registry.Resolve("guard_door"));
        Debug.Log("M1_Module_Builder: guard door click simulated.");
    }

    [MenuItem("Training/8 Debug - Click Power On")]
    public static void DebugClickPowerOn(){ DebugClickStateToggle("power_on"); }

    [MenuItem("Training/8 Debug - Click Emergency Stop")]
    public static void DebugClickEmergencyStop(){ DebugClickStateToggle("emergency_stop"); }

    private static void DebugClickStateToggle(string markerId){
        if (!Application.isPlaying) return;
        Marker_Registry registry = Object.FindFirstObjectByType<Marker_Registry>();
        if (registry == null){ Debug.LogError("M1_Module_Builder: registry missing."); return; }

        Component_Marker marker = registry.Resolve(markerId);

        foreach (Marker_State_Toggle toggle in Object.FindObjectsByType<Marker_State_Toggle>(FindObjectsSortMode.None)){
            if (new SerializedObject(toggle).FindProperty("Marker").objectReferenceValue == (Object)marker){
                toggle.On_Selected(marker);
                Debug.Log($"M1_Module_Builder: {markerId} click simulated.");
                return;
            }
        }

        Debug.LogError($"M1_Module_Builder: no Marker_State_Toggle for '{markerId}'.");
    }

    [MenuItem("Training/8 Debug - Click Door Unlock")]
    public static void DebugClickDoorUnlock(){
        if (!Application.isPlaying) return;
        Marker_Registry registry = Object.FindFirstObjectByType<Marker_Registry>();
        if (registry == null){ Debug.LogError("M1_Module_Builder: registry missing."); return; }

        Component_Marker marker = registry.Resolve("door_unlock");

        foreach (Door_Click_Toggle toggle in Object.FindObjectsByType<Door_Click_Toggle>(FindObjectsSortMode.None)){
            if (new SerializedObject(toggle).FindProperty("DoorMarker").objectReferenceValue == (Object)marker){
                toggle.On_Door_Selected(marker);
                Debug.Log("M1_Module_Builder: door unlock click simulated.");
                return;
            }
        }

        Debug.LogError("M1_Module_Builder: no Door_Click_Toggle for 'door_unlock'.");
    }

    [MenuItem("Training/9 Dump Mill Diagnostics")]
    public static void DumpDiagnostics(){
        GameObject wrapper = GameObject.Find("PM8000_Training");

        if (wrapper == null){
            Debug.LogError("M1_Module_Builder: no PM8000_Training in open scene.");
            return;
        }

        Transform mill = wrapper.transform.GetChild(0);
        var sb = new System.Text.StringBuilder();
        string[] nodes = {
            "Worktable_Base", "WB_XAxis_Drive", "WB_YAxis_Drive", "WB_Clamp", "WB_Static", "WB_Spindle", "WB_Hardware",
            "PB_XY_Saddle", "PB_Saddle_Moving", "PB_Saddle_Static", "PB_Knee_Table",
            "SpindleBase", "SpindleMotor", "SB_Static", "doors", "PB_Column_Structure", "PB_Column_Static",
            "ToolChangeBody", "MillController"
        };

        foreach (string name in nodes){
            Transform t = FindChild(mill, name);

            if (t == null){
                sb.AppendLine($"{name}: NOT FOUND");
                continue;
            }

            Bounds b = RendererBounds(t);
            sb.AppendLine($"{name}: center ({b.center.x:F2}, {b.center.y:F2}, {b.center.z:F2}) size ({b.size.x:F2}, {b.size.y:F2}, {b.size.z:F2})");
        }

        Debug.Log("M1_Module_Builder diagnostics:\n" + sb);
    }
}
