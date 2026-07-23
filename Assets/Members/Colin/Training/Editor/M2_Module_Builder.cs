using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using static Training_Builder_Core;

// Module 2 — "System Startup & Program Execution" (docs/VR_Modules/
// 03_Module2_Startup_Plan.md). Content + workstation scene wiring only; all
// generic scaffolding comes from Training_Builder_Core. Depends on Module 1's
// build output (PM8000_Training.prefab) — build order is enforced by Order.
public class M2_Module_Builder : ITraining_Module_Builder{
    private const string Root = "Assets/Members/Colin";
    private const string ScenesDir = Root + "/Scenes";
    private const string TrainingDir = Root + "/Training";
    private const string PrefabsDir = TrainingDir + "/Prefabs";
    private const string LessonsDir = TrainingDir + "/Lessons";
    private const string MaterialsDir = TrainingDir + "/Materials";
    private const string LessonPath = LessonsDir + "/M2_Lesson.asset";
    private const string Module2ScenePath = ScenesDir + "/Module2_Startup.unity";

    private const string MonitorGuid = "e03770ab4a7ea8c4094ef3702b684be9";
    private const string ComputerGuid = "d724869b0a60cb144bd63b585af60f77"; // uploads_files_5269118_fbx.fbx (PC tower)

    public int Order => 1;

    public void Build(){
        BuildM2LessonAsset();
        BuildModule2Scene();
    }

    // ------------------------------------------------------------------
    // Lesson content (system startup)
    // ------------------------------------------------------------------

    private static void BuildM2LessonAsset(){
        Lesson_Definition def = LoadOrCreateLesson(LessonPath);
        def.Module_Id = "M2";
        def.Scene_Name = "Module2_Startup";
        def.Display_Name = "M2 — System Startup & Program Execution";
        def.Steps = BuildM2Steps();
        def.Quiz_Pass_Threshold = def.Steps.FindAll(s => s.Include_In_Quiz).Count;
        def.Practice_Shuffles_Quiz = false; // order IS the test — keep the canonical sequence
        EditorUtility.SetDirty(def);
    }

    private static List<Lesson_Step> BuildM2Steps(){
        return new List<Lesson_Step>{
            Info("intro_cell", "System Startup — bring the whole cell up from cold.\n\nTwo stations: the ARM station (PC + black-box controller, running SCORBASE) and the MILL station (ProMill 8000, running CNCBase). The robot arm itself isn't shown here — its state is described as you go.\n\nThree phases: (1) power on every system, (2) launch SCORBASE and CNCBase, (3) bring each machine Active, Home it, then verify. Follow the highlighted control at each step.\n\nPress Continue."),
            // Phase 1 — power on every system
            M2Action("arm_pc_on", "Arm-station PC power button", "Phase 1 — power on everything. Each station has a dedicated PC; boot it first."),
            M2Action("arm_controller_on", "Robot controller power (the black box)", "The black box is the robot controller that drives the arm — SCORBASE talks to it. Power it on."),
            M2Action("mill_pc_on", "Mill-station PC power button", "The mill station needs its own PC running too."),
            M2Action("mill_power_on", "ProMill 8000 main power", "Switch on the ProMill 8000's main hardware power. Every system is now powered."),
            // Phase 2 — launch the control software
            M2Action("scorbase_launch", "Launch SCORBASE", "Phase 2 — launch the control software. SCORBASE runs the arm station."),
            M2Action("cncbase_launch", "Launch CNCBase", "CNCBase runs the mill station. Both control programs are now up."),
            // Phase 3 — bring each machine active, home, and verify
            M2Action("scorbase_online", "Control On (go Active)", "Phase 3 — bring each machine active, home it, and verify. In SCORBASE, Control On: only Active sends commands to real hardware."),
            M2Action("scorbase_home", "Search Home — All Axes", "Search Home drives each axis to its home switch, setting the encoder reference. Nothing is trustworthy before homing."),
            M2Action("scorbase_standalone", "Mode: Standalone", "Standalone = the station runs under its own software. CIM-managed (cell-wide) control is taught in Module 5."),
            M2Action("verify_arm", "Test Move (A1)", "Jog one axis to prove the arm responds — Active, homed and Standalone."),
            M2Action("cncbase_online", "Connect: Active", "Now the mill. In CNCBase, connect Active (the real machine), not Simulation."),
            M2Action("cncbase_home", "Machine Home", "Machine Home sets the mill's factory reference; all positioning is relative to it."),
            M2Action("run_start_fms", "start_fms.nc", "Run start_fms.nc — it puts the mill in a wait-loop in local control, ready for cell commands."),
            M2Action("verify_mill", "Confirm: Running", "Confirm the status reads Running: start_fms.nc — the mill is in local control."),
            Info("guided_done", "All systems active.\n\nThe arm is Active, Homed and Standalone; the mill is Homed and running start_fms.nc in local control. The arm has loaded the first workpiece onto the vise.\n\nNext: the practice run — no highlights. Perform the full cold-start in the correct order. Out-of-order actions are logged as errors.\n\nPress Continue.")
        };
    }

    private static Lesson_Step M2Action(string id, string label, string teach){
        return PanelAction(id, label, teach, "Practice: bring the cell up from cold, in the correct order.");
    }

    // ------------------------------------------------------------------
    // Module 2 scene
    // ------------------------------------------------------------------

    [MenuItem("Training/3B Build Module2 Scene")]
    public static void BuildModule2SceneMenu(){
        new M2_Module_Builder().Build();
    }

    private static void BuildModule2Scene(){
        var scene = NewModuleScene();

        // Root the Action_Button_Registry scans for every clickable startup action.
        GameObject content = new GameObject("Module2");

        // --- Mill (reuse the wired M1 prefab: mill + vise + demo block) ---
        GameObject millPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsDir + "/PM8000_Training.prefab");
        if (millPrefab == null){ Debug.LogError("M2_Module_Builder: PM8000_Training.prefab missing — run 'Training/3 Build Module1 Scene' first."); return; }

        GameObject millWrapper = (GameObject)PrefabUtility.InstantiatePrefab(millPrefab);
        millWrapper.name = "PM8000_Training";
        millWrapper.transform.SetParent(content.transform, true);
        millWrapper.transform.position = new Vector3(1.681f, 0f, 3.44f);
        millWrapper.transform.rotation = Quaternion.Euler(0f, 270f, 0f);

        GameObject demoBlock = null;
        Transform blockT = FindChild(millWrapper.transform, "Demo_Block");
        if (blockT != null){ demoBlock = blockT.gameObject; demoBlock.SetActive(false); }

        // --- Workstation props: two desks + two monitors + two PC towers + black-box controller ---
        // Transforms baked from Colin's in-editor layout (do not eyeball-edit; rebuild reproduces these).
        GameObject deskPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game_Objects/PCTable.prefab");
        InstantiateProp(deskPrefab, content.transform, "Desk", new Vector3(0.717f, 0.72f, 2.674f), Quaternion.Euler(270f, 0f, 0f));
        InstantiateProp(deskPrefab, content.transform, "Desk (1)", new Vector3(-1.408f, 0.72f, 3.422f), Quaternion.Euler(270f, 0f, 0f));

        GameObject monitorAsset = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(MonitorGuid));
        InstantiateProp(monitorAsset, content.transform, "Arm_Monitor", new Vector3(0.67f, 0.768f, 2.664f), Quaternion.Euler(0f, 270f, 0f));
        InstantiateProp(monitorAsset, content.transform, "Mill_Monitor", new Vector3(-1.498f, 0.766f, 3.214f), Quaternion.Euler(0f, 180f, 0f));

        GameObject computerAsset = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(ComputerGuid));
        GameObject armPc = InstantiateProp(computerAsset, content.transform, "Arm_Computer", new Vector3(0.815f, 0.435f, 2.845f), Quaternion.Euler(270f, 90f, 0f));
        if (armPc != null) armPc.transform.localScale = Vector3.one * 2.54f;
        GameObject millPc = InstantiateProp(computerAsset, content.transform, "Mill_Computer", new Vector3(-1.31f, 0.435f, 3.593f), Quaternion.Euler(270f, 90f, 0f));
        if (millPc != null) millPc.transform.localScale = Vector3.one * 2.54f;

        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "Robot_Controller_Box";
        box.transform.SetParent(content.transform, true);
        box.transform.localScale = new Vector3(0.3f, 0.36f, 0.3f);
        box.transform.position = new Vector3(0.801f, 0.412f, 2.511f);
        box.GetComponent<MeshRenderer>().sharedMaterial = LoadOrCreateMaterial(MaterialsDir + "/M2_Box.mat", new Color(0.05f, 0.05f, 0.06f, 1f));

        // --- SCORBASE panel (arm PC) — tabbed so it sits on the monitor ---
        Canvas armCanvas = CreateWorldCanvas("SCORBASE_Panel", new Vector2(520f, 460f), 0.0009f, new Vector3(0.654f, 1.143f, 2.687f));
        armCanvas.transform.SetParent(content.transform, true);
        armCanvas.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        armCanvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        CanvasGroup armScreen = armCanvas.gameObject.AddComponent<CanvasGroup>();
        armScreen.alpha = 0.25f;
        AddBackground(armCanvas.transform, new Color(0.05f, 0.07f, 0.12f, 0.95f));

        // Header — always visible, holds the status / homing readouts the state controller drives.
        CreateTMP(armCanvas.transform, "Title", "SCORBASE", 34f, new Vector2(0f, 202f), new Vector2(480f, 46f)).fontStyle = FontStyles.Bold;
        TextMeshProUGUI armStatus = CreateTMP(armCanvas.transform, "Arm_Status", "Status: —", 24f, new Vector2(0f, 166f), new Vector2(480f, 38f));
        TextMeshProUGUI scorbaseMode = CreateTMP(armCanvas.transform, "Scorbase_Mode", "Mode: CIM", 22f, new Vector2(0f, 138f), new Vector2(480f, 32f));

        GameObject armChecks = new GameObject("Axis_Checks", typeof(RectTransform));
        armChecks.transform.SetParent(armCanvas.transform, false);
        ((RectTransform)armChecks.transform).anchoredPosition = new Vector2(0f, 110f);
        CreateTMP(armChecks.transform, "Label", "Home:", 18f, new Vector2(-205f, 0f), new Vector2(90f, 28f));
        GameObject[] axisRows = new GameObject[5];
        for (int i = 0; i < axisRows.Length; i++){
            TextMeshProUGUI check = CreateTMP(armChecks.transform, "Axis_" + (i + 1), $"A{i + 1} <color=#66FF88>OK</color>", 18f, new Vector2(-110f + i * 66f, 0f), new Vector2(62f, 28f));
            axisRows[i] = check.gameObject;
            axisRows[i].SetActive(false);
        }

        // Tab bar + content groups (only the active tab's buttons show).
        Button[] armTabs = {
            CreateButton(armCanvas.transform, "Tab_Control", "Control", new Vector2(-120f, 78f), new Vector2(232f, 44f)),
            CreateButton(armCanvas.transform, "Tab_Verify", "Verify", new Vector2(120f, 78f), new Vector2(232f, 44f))
        };

        GameObject armControlTab = BuildTabContent(armCanvas.transform, "Control_Content");
        BuildActionButton(armControlTab.transform, "scorbase_launch", "Launch SCORBASE", new Vector2(0f, 28f), new Vector2(460f, 52f));
        BuildActionButton(armControlTab.transform, "scorbase_online", "Control On (go Active)", new Vector2(0f, -32f), new Vector2(460f, 52f));
        BuildActionButton(armControlTab.transform, "scorbase_home", "Search Home — All Axes", new Vector2(0f, -92f), new Vector2(460f, 52f));
        BuildActionButton(armControlTab.transform, "scorbase_standalone", "Mode: Standalone", new Vector2(0f, -152f), new Vector2(460f, 52f));

        GameObject armVerifyTab = BuildTabContent(armCanvas.transform, "Verify_Content");
        BuildActionButton(armVerifyTab.transform, "verify_arm", "Test Move (A1)", new Vector2(0f, 28f), new Vector2(460f, 52f));
        Button cimButton = CreateButton(armVerifyTab.transform, "CIM_Disabled", "CIM-managed (Module 5)", new Vector2(0f, -32f), new Vector2(460f, 48f));
        cimButton.interactable = false;
        armVerifyTab.SetActive(false);

        Panel_Tab_Group armTabGroup = armCanvas.gameObject.AddComponent<Panel_Tab_Group>();
        SetRefArray(armTabGroup, "Tabs", new List<Object>(armTabs));
        SetRefArray(armTabGroup, "Contents", new List<Object>{ armControlTab, armVerifyTab });

        // --- CNCBase panel (mill PC) — tabbed ---
        Canvas millCanvas = CreateWorldCanvas("CNCBase_Panel", new Vector2(520f, 460f), 0.0009f, new Vector3(-1.506f, 1.139f, 3.202f));
        millCanvas.transform.SetParent(content.transform, true);
        millCanvas.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        millCanvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        CanvasGroup millScreen = millCanvas.gameObject.AddComponent<CanvasGroup>();
        millScreen.alpha = 0.25f;
        AddBackground(millCanvas.transform, new Color(0.05f, 0.07f, 0.12f, 0.95f));

        CreateTMP(millCanvas.transform, "Title", "CNCBase", 34f, new Vector2(0f, 202f), new Vector2(480f, 46f)).fontStyle = FontStyles.Bold;
        TextMeshProUGUI millStatus = CreateTMP(millCanvas.transform, "Mill_Status", "—", 24f, new Vector2(0f, 166f), new Vector2(480f, 38f));
        TextMeshProUGUI millReadout = CreateTMP(millCanvas.transform, "Mill_Readout", "X ---   Y ---   Z ---", 22f, new Vector2(0f, 138f), new Vector2(480f, 32f));

        Button[] millTabs = {
            CreateButton(millCanvas.transform, "Tab_Connect", "Connect & Home", new Vector2(-120f, 78f), new Vector2(232f, 44f)),
            CreateButton(millCanvas.transform, "Tab_Run", "Run & Verify", new Vector2(120f, 78f), new Vector2(232f, 44f))
        };

        GameObject millConnectTab = BuildTabContent(millCanvas.transform, "Connect_Content");
        BuildActionButton(millConnectTab.transform, "cncbase_launch", "Launch CNCBase", new Vector2(0f, 28f), new Vector2(460f, 52f));
        BuildActionButton(millConnectTab.transform, "cncbase_online", "Connect: Active", new Vector2(0f, -32f), new Vector2(460f, 52f));
        BuildActionButton(millConnectTab.transform, "distractor_simulation", "Connect: Simulation", new Vector2(0f, -92f), new Vector2(460f, 52f));
        BuildActionButton(millConnectTab.transform, "cncbase_home", "Machine Home", new Vector2(0f, -152f), new Vector2(460f, 52f));

        GameObject millRunTab = BuildTabContent(millCanvas.transform, "Run_Content");
        CreateTMP(millRunTab.transform, "Programs_Label", "Programs:", 18f, new Vector2(-150f, 44f), new Vector2(180f, 28f));
        BuildActionButton(millRunTab.transform, "run_start_fms", "start_fms.nc", new Vector2(0f, 8f), new Vector2(460f, 50f));
        BuildActionButton(millRunTab.transform, "distractor_prog1", "part_042.nc", new Vector2(0f, -44f), new Vector2(460f, 50f));
        BuildActionButton(millRunTab.transform, "distractor_prog2", "calib_probe.nc", new Vector2(0f, -96f), new Vector2(460f, 50f));
        BuildActionButton(millRunTab.transform, "verify_mill", "Confirm: Running", new Vector2(0f, -152f), new Vector2(460f, 52f));
        millRunTab.SetActive(false);

        Panel_Tab_Group millTabGroup = millCanvas.gameObject.AddComponent<Panel_Tab_Group>();
        SetRefArray(millTabGroup, "Tabs", new List<Object>(millTabs));
        SetRefArray(millTabGroup, "Contents", new List<Object>{ millConnectTab, millRunTab });

        // --- Power buttons on the props ---
        BuildPowerCanvas(content.transform, "arm_pc_on", "Arm PC\nPower", new Vector3(0.617f, 0.433f, 2.903f), Quaternion.Euler(0f, 90f, 0f), false, out _);
        BuildPowerCanvas(content.transform, "mill_pc_on", "Mill PC\nPower", new Vector3(-1.308f, 0.433f, 3.504f), Quaternion.Euler(0f, 0f, 0f), false, out _);
        BuildPowerCanvas(content.transform, "arm_controller_on", "Controller\nPower", new Vector3(0.65f, 0.458f, 2.457f), Quaternion.Euler(0f, 90f, 0f), true, out Image controllerIndicator);

        // Mill main power: the real ProMill 8000 "kaig" switch is itself the
        // clickable control — a bounding box marks it. Selecting it lights the
        // switch green (Startup_State_Controller.MillPowerPart).
        Transform kaig = FindChild(millWrapper.transform, "kaig");
        Renderer millPowerPart = kaig != null ? kaig.GetComponentInChildren<Renderer>() : null;

        if (kaig != null)
            BuildPartAction(content.transform, "mill_power_on", RendererBounds(kaig));
        else
            Debug.LogWarning("M2_Module_Builder: 'kaig' part not found on the mill — mill main power has no clickable switch.");

        // --- Prompt / results / sequencer scaffold ---
        Module_Scaffold scaffold = BuildLessonScaffold(new Vector3(-2.443f, 1.128f, 2.889f), Quaternion.Euler(0f, -20f, 0f), new Vector3(0.845f, 1.064f, 1.697f));
        if (scaffold == null) return;
        GameObject manager = scaffold.Manager;
        Lesson_Sequencer sequencer = scaffold.Sequencer;

        Action_Button_Registry actionRegistry = manager.AddComponent<Action_Button_Registry>();
        Startup_State_Controller stateController = manager.AddComponent<Startup_State_Controller>();

        SetRef(actionRegistry, "ButtonsRoot", content.transform);
        SetRef(actionRegistry, "Sequencer", sequencer);

        SetRef(stateController, "Sequencer", sequencer);
        SetRef(stateController, "Registry", actionRegistry);
        SetRef(stateController, "ArmScreen", armScreen);
        SetRef(stateController, "MillScreen", millScreen);
        SetRef(stateController, "ControllerIndicator", controllerIndicator);

        if (millPowerPart != null)
            SetRef(stateController, "MillPowerPart", millPowerPart);
        SetRef(stateController, "ArmStatus", armStatus);
        SetRef(stateController, "ScorbaseMode", scorbaseMode);
        SetRef(stateController, "MillStatus", millStatus);
        SetRef(stateController, "MillReadout", millReadout);
        SetRefArray(stateController, "AxisCheckRows", new List<Object>(axisRows));

        if (demoBlock != null)
            SetRef(stateController, "DemoBlock", demoBlock);

        EditorSceneManager.SaveScene(scene, Module2ScenePath);
        RegisterModule(AssetDatabase.LoadAssetAtPath<Lesson_Definition>(LessonPath), Module2ScenePath);
        Debug.Log("M2_Module_Builder: Module2 scene built.");
    }

    private static void BuildPowerCanvas(Transform parent, string actionId, string label, Vector3 worldPos, Quaternion rot, bool withIndicator, out Image indicator){
        Canvas canvas = CreateWorldCanvas("Power_" + actionId, new Vector2(280f, 150f), 0.0016f, worldPos);
        canvas.transform.SetParent(parent, true);
        canvas.transform.rotation = rot;
        canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        AddBackground(canvas.transform, new Color(0.04f, 0.04f, 0.06f, 0.95f));

        indicator = null;

        if (withIndicator){
            GameObject ind = new GameObject("Indicator", typeof(RectTransform));
            ind.transform.SetParent(canvas.transform, false);
            RectTransform ir = ind.GetComponent<RectTransform>();
            ir.sizeDelta = new Vector2(40f, 40f);
            ir.anchoredPosition = new Vector2(0f, 48f);
            indicator = ind.AddComponent<Image>();
            indicator.color = new Color(0.25f, 0.25f, 0.28f, 1f);
            BuildActionButton(canvas.transform, actionId, label, new Vector2(0f, -30f), new Vector2(230f, 80f));
        }
        else{
            BuildActionButton(canvas.transform, actionId, label, new Vector2(0f, 0f), new Vector2(240f, 110f));
        }
    }
}
