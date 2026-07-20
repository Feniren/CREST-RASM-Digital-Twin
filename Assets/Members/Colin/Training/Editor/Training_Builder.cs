using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.UI;
using ProMill8000;

// Regenerates the VR training assets and scenes (docs/VR_Modules/). Re-run
// "Training/3 Build Module1 Scene" or "Training/5 Reposition Markers" after the
// ProMill8000 model is replaced — markers are re-derived from node bounds.
public static class Training_Builder{
    private const string Root = "Assets/Members/Colin";
    private const string ScenesDir = Root + "/Scenes";
    private const string TrainingDir = Root + "/Training";
    private const string PrefabsDir = TrainingDir + "/Prefabs";
    private const string LessonsDir = TrainingDir + "/Lessons";
    private const string MaterialsDir = TrainingDir + "/Materials";

    private const string BootstrapScenePath = ScenesDir + "/Bootstrap.unity";
    private const string Module1ScenePath = ScenesDir + "/Module1_Overview.unity";
    private const string Module2ScenePath = ScenesDir + "/Module2_Startup.unity";
    private const string M2ButtonName = "M2_Button";
    private const string MonitorGuid = "e03770ab4a7ea8c4094ef3702b684be9";
    private const string ComputerGuid = "d724869b0a60cb144bd63b585af60f77"; // uploads_files_5269118_fbx.fbx (PC tower)

    private const string RigPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.3.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
    private const string InputActionsPath = "Assets/Samples/XR Interaction Toolkit/3.3.1/Starter Assets/XRI Default Input Actions.inputactions";
    private const string MillPrefabPath = "Assets/Prefabs/reconstructedPM8000.prefab";
    private const string VicePrefabPath = "Assets/Game_Objects/Extra/DualAxisVice.prefab";
    private const string BlueGlassPath = "Assets/Materials/Blue_Glass.mat";
    private const string FloorMatPath = "Assets/Materials/Light_Grey.mat";

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

    [MenuItem("Training/0 Build Everything")]
    public static void BuildEverything(){
        BuildSharedAssets();
        BuildBootstrapScene();
        BuildModule1Scene();
        BuildModule2Scene();
        AddScenesToBuildSettings();
        Debug.Log("Training_Builder: full build complete.");
    }

    // ------------------------------------------------------------------
    // 1. Shared assets
    // ------------------------------------------------------------------

    [MenuItem("Training/1 Build Shared Assets")]
    public static void BuildSharedAssets(){
        EnsureFolders();
        BuildMarkerGlowMaterial();
        BuildComponentMarkerPrefab();
        BuildDemoBlockPrefab();
        BuildPromptPanelPrefab();
        BuildLessonAsset();
        BuildM2LessonAsset();
        AssetDatabase.SaveAssets();
        Debug.Log("Training_Builder: shared assets built.");
    }

    private static void EnsureFolders(){
        if (!AssetDatabase.IsValidFolder(ScenesDir)) AssetDatabase.CreateFolder(Root, "Scenes");
        if (!AssetDatabase.IsValidFolder(TrainingDir)) AssetDatabase.CreateFolder(Root, "Training");
        if (!AssetDatabase.IsValidFolder(PrefabsDir)) AssetDatabase.CreateFolder(TrainingDir, "Prefabs");
        if (!AssetDatabase.IsValidFolder(LessonsDir)) AssetDatabase.CreateFolder(TrainingDir, "Lessons");
        if (!AssetDatabase.IsValidFolder(MaterialsDir)) AssetDatabase.CreateFolder(TrainingDir, "Materials");
    }

    private static void BuildMarkerGlowMaterial(){
        string path = MaterialsDir + "/Marker_Glow.mat";

        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            return;

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetFloat("_Surface", 1f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = 3000;
        mat.SetColor("_BaseColor", new Color(1f, 0.85f, 0.2f, 0.4f));
        AssetDatabase.CreateAsset(mat, path);
    }

    private static void BuildComponentMarkerPrefab(){
        Material glow = AssetDatabase.LoadAssetAtPath<Material>(MaterialsDir + "/Marker_Glow.mat");

        GameObject rootGO = new GameObject("Component_Marker");
        BoxCollider collider = rootGO.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable =
            rootGO.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        // XRBaseInteractable's collider auto-population skips trigger colliders,
        // so the trigger box must be serialized into the list explicitly.
        interactable.colliders.Add(collider);
        Component_Marker marker = rootGO.AddComponent<Component_Marker>();

        GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shell.name = "Glow_Shell";
        Object.DestroyImmediate(shell.GetComponent<BoxCollider>());
        shell.transform.SetParent(rootGO.transform, false);
        MeshRenderer shellRenderer = shell.GetComponent<MeshRenderer>();
        shellRenderer.sharedMaterial = glow;
        shellRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        shellRenderer.enabled = false;

        GameObject label = new GameObject("Label", typeof(RectTransform));
        label.transform.SetParent(rootGO.transform, false);
        Canvas labelCanvas = label.AddComponent<Canvas>();
        labelCanvas.renderMode = RenderMode.WorldSpace;
        label.AddComponent<Face_Camera>();
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(500f, 110f);
        labelRect.localScale = Vector3.one * 0.0015f;
        labelRect.localPosition = new Vector3(0f, 0.7f, 0f);

        TextMeshProUGUI tmp = CreateTMP(label.transform, "Text", "Component", 56f, Vector2.zero, new Vector2(500f, 110f));
        tmp.alignment = TextAlignmentOptions.Center;
        label.SetActive(false);

        SetRef(marker, "GlowShell", shellRenderer);
        SetRef(marker, "LabelRoot", label);

        PrefabUtility.SaveAsPrefabAsset(rootGO, PrefabsDir + "/Component_Marker.prefab");
        Object.DestroyImmediate(rootGO);
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

    private static void BuildPromptPanelPrefab(){
        Canvas canvas = CreateWorldCanvas("Prompt_Panel", new Vector2(900f, 420f), 0.0012f, Vector3.zero);
        canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        canvas.gameObject.AddComponent<Face_Camera>();

        // Background is a self-sizing container: fixed width, height driven by the
        // vertical layout + content-size fitter so the dark panel hugs the text.
        GameObject bg = new GameObject("Background", typeof(RectTransform));
        bg.transform.SetParent(canvas.transform, false);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = bgRect.anchorMax = bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.sizeDelta = new Vector2(900f, 420f);
        bg.AddComponent<Image>().color = new Color(0.07f, 0.09f, 0.13f, 0.9f);

        VerticalLayoutGroup layout = bg.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(40, 40, 40, 40);
        layout.spacing = 30f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childForceExpandWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = bg.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextMeshProUGUI prompt = CreateTMP(bg.transform, "Prompt_Text", "Prompt", 36f, Vector2.zero, new Vector2(820f, 280f));
        prompt.alignment = TextAlignmentOptions.Center;

        Button continueButton = CreateButton(bg.transform, "Continue_Button", "Continue", Vector2.zero, new Vector2(300f, 80f));
        LayoutElement continueLayout = continueButton.gameObject.AddComponent<LayoutElement>();
        continueLayout.preferredWidth = 300f;
        continueLayout.preferredHeight = 80f;

        PrefabUtility.SaveAsPrefabAsset(canvas.gameObject, PrefabsDir + "/Prompt_Panel.prefab");
        Object.DestroyImmediate(canvas.gameObject);
    }

    private static void BuildLessonAsset(){
        string path = LessonsDir + "/M1_Lesson.asset";
        Lesson_Definition def = AssetDatabase.LoadAssetAtPath<Lesson_Definition>(path);
        bool isNew = def == null;

        if (isNew)
            def = ScriptableObject.CreateInstance<Lesson_Definition>();

        def.Module_Id = "M1";
        def.Scene_Name = "Module1_Overview";
        def.Display_Name = "M1 — CNC Milling: What & Why";
        def.Quiz_Pass_Threshold = 4;
        def.Steps = BuildM1Steps();

        if (isNew)
            AssetDatabase.CreateAsset(def, path);
        else
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

    private static Lesson_Step Info(string id, string prompt){
        return new Lesson_Step{ Step_Id = id, Kind = Lesson_Step_Kind.Info, Prompt_Text = prompt };
    }

    private static Lesson_Step Select(string id, string display, string description, bool includeInQuiz = true){
        return new Lesson_Step{
            Step_Id = id,
            Kind = Lesson_Step_Kind.Select_Component,
            Prompt_Text = $"This is the {display.ToUpper()} — {description}.\n\nSelect the highlighted part to continue.",
            Practice_Prompt_Text = $"Select the: {display}",
            Target_Marker_Id = id,
            Include_In_Quiz = includeInQuiz
        };
    }

    // ------------------------------------------------------------------
    // Module 2 lesson (system startup)
    // ------------------------------------------------------------------

    private static void BuildM2LessonAsset(){
        string path = LessonsDir + "/M2_Lesson.asset";
        Lesson_Definition def = AssetDatabase.LoadAssetAtPath<Lesson_Definition>(path);
        bool isNew = def == null;

        if (isNew)
            def = ScriptableObject.CreateInstance<Lesson_Definition>();

        def.Module_Id = "M2";
        def.Scene_Name = "Module2_Startup";
        def.Display_Name = "M2 — System Startup & Program Execution";
        def.Steps = BuildM2Steps();
        def.Quiz_Pass_Threshold = def.Steps.FindAll(s => s.Include_In_Quiz).Count;
        def.Practice_Shuffles_Quiz = false; // order IS the test — keep the canonical sequence

        if (isNew)
            AssetDatabase.CreateAsset(def, path);
        else
            EditorUtility.SetDirty(def);
    }

    private static List<Lesson_Step> BuildM2Steps(){
        return new List<Lesson_Step>{
            Info("intro_cell", "System Startup — bring the whole cell up from cold.\n\nTwo stations: the ARM station (PC + black-box controller, running SCORBASE) and the MILL station (ProMill 8000, running CNCBase). The robot arm itself isn't shown here — its state is described as you go.\n\nThree phases: (1) power on every system, (2) launch SCORBASE and CNCBase, (3) bring each machine Active, Home it, then verify. Follow the highlighted control at each step.\n\nPress Continue."),
            // Phase 1 — power on every system
            PanelAction("arm_pc_on", "Arm-station PC power button", "Phase 1 — power on everything. Each station has a dedicated PC; boot it first."),
            PanelAction("arm_controller_on", "Robot controller power (the black box)", "The black box is the robot controller that drives the arm — SCORBASE talks to it. Power it on."),
            PanelAction("mill_pc_on", "Mill-station PC power button", "The mill station needs its own PC running too."),
            PanelAction("mill_power_on", "ProMill 8000 main power", "Switch on the ProMill 8000's main hardware power. Every system is now powered."),
            // Phase 2 — launch the control software
            PanelAction("scorbase_launch", "Launch SCORBASE", "Phase 2 — launch the control software. SCORBASE runs the arm station."),
            PanelAction("cncbase_launch", "Launch CNCBase", "CNCBase runs the mill station. Both control programs are now up."),
            // Phase 3 — bring each machine active, home, and verify
            PanelAction("scorbase_online", "Control On (go Active)", "Phase 3 — bring each machine active, home it, and verify. In SCORBASE, Control On: only Active sends commands to real hardware."),
            PanelAction("scorbase_home", "Search Home — All Axes", "Search Home drives each axis to its home switch, setting the encoder reference. Nothing is trustworthy before homing."),
            PanelAction("scorbase_standalone", "Mode: Standalone", "Standalone = the station runs under its own software. CIM-managed (cell-wide) control is taught in Module 5."),
            PanelAction("verify_arm", "Test Move (A1)", "Jog one axis to prove the arm responds — Active, homed and Standalone."),
            PanelAction("cncbase_online", "Connect: Active", "Now the mill. In CNCBase, connect Active (the real machine), not Simulation."),
            PanelAction("cncbase_home", "Machine Home", "Machine Home sets the mill's factory reference; all positioning is relative to it."),
            PanelAction("run_start_fms", "start_fms.nc", "Run start_fms.nc — it puts the mill in a wait-loop in local control, ready for cell commands."),
            PanelAction("verify_mill", "Confirm: Running", "Confirm the status reads Running: start_fms.nc — the mill is in local control."),
            Info("guided_done", "All systems active.\n\nThe arm is Active, Homed and Standalone; the mill is Homed and running start_fms.nc in local control. The arm has loaded the first workpiece onto the vise.\n\nNext: the practice run — no highlights. Perform the full cold-start in the correct order. Out-of-order actions are logged as errors.\n\nPress Continue.")
        };
    }

    private static Lesson_Step PanelAction(string id, string label, string teach){
        return new Lesson_Step{
            Step_Id = id,
            Kind = Lesson_Step_Kind.Panel_Action,
            Prompt_Text = $"{teach}\n\n<b>Do:</b> {label}  (the highlighted control).",
            Practice_Prompt_Text = "Practice: bring the cell up from cold, in the correct order.",
            Target_Marker_Id = id,
            Include_In_Quiz = true
        };
    }

    // ------------------------------------------------------------------
    // 2. Bootstrap scene
    // ------------------------------------------------------------------

    [MenuItem("Training/2 Build Bootstrap Scene")]
    public static void BuildBootstrapScene(){
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateDirectionalLight();
        CreateFloor();

        GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefabPath);
        if (rigPrefab == null){ Debug.LogError($"Training_Builder: rig prefab missing at {RigPrefabPath}"); return; }
        GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);

        // The component markers use trigger colliders; XRI casters ignore triggers by
        // default, which makes markers unselectable. Teleport ray is left at Ignore so
        // marker boxes don't block teleport aiming.
        foreach (CurveInteractionCaster caster in rig.GetComponentsInChildren<CurveInteractionCaster>(true))
            SetVal(caster, "m_RaycastTriggerInteraction", (int)QueryTriggerInteraction.Collide);
        foreach (SphereInteractionCaster caster in rig.GetComponentsInChildren<SphereInteractionCaster>(true))
            SetVal(caster, "m_PhysicsTriggerInteraction", (int)QueryTriggerInteraction.Collide);
        foreach (XRPokeInteractor poke in rig.GetComponentsInChildren<XRPokeInteractor>(true))
            SetVal(poke, "m_PhysicsTriggerInteraction", (int)QueryTriggerInteraction.Collide);

        GameObject interactionManager = new GameObject("XR Interaction Manager");
        interactionManager.AddComponent<XRInteractionManager>();

        GameObject inputManager = new GameObject("Input Action Manager");
        InputActionManager actionManager = inputManager.AddComponent<InputActionManager>();
        var actionsAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(InputActionsPath);
        SerializedObject actionSO = new SerializedObject(actionManager);
        SerializedProperty assetsProp = actionSO.FindProperty("m_ActionAssets");
        assetsProp.arraySize = 1;
        assetsProp.GetArrayElementAtIndex(0).objectReferenceValue = actionsAsset;
        actionSO.ApplyModifiedPropertiesWithoutUndo();

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<XRUIInputModule>();

        // Fade canvas under the HMD camera
        Transform mainCamera = rig.transform.Find("Camera Offset/Main Camera");
        Canvas fadeCanvas = CreateWorldCanvas("Fade_Canvas", new Vector2(2000f, 2000f), 0.001f, Vector3.zero);
        fadeCanvas.transform.SetParent(mainCamera, false);
        fadeCanvas.transform.localPosition = new Vector3(0f, 0f, 0.35f);
        fadeCanvas.sortingOrder = 999;
        Image fadeImage = AddBackground(fadeCanvas.transform, Color.black);
        fadeImage.raycastTarget = false;
        fadeImage.enabled = false;
        Screen_Fader fader = fadeCanvas.gameObject.AddComponent<Screen_Fader>();
        SetRef(fader, "FadeImage", fadeImage);

        // Wrist HUD on the left controller
        Transform leftController = rig.transform.Find("Camera Offset/Left Controller");
        Canvas wristCanvas = CreateWorldCanvas("Wrist_HUD", new Vector2(320f, 420f), 0.0004f, Vector3.zero);
        wristCanvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        wristCanvas.transform.SetParent(leftController, false);
        wristCanvas.transform.localPosition = new Vector3(0f, 0.05f, -0.07f);
        wristCanvas.transform.localEulerAngles = new Vector3(55f, 0f, 0f);
        wristCanvas.transform.localScale = Vector3.one * 0.0004f;

        GameObject hudPanel = new GameObject("HUD_Panel", typeof(RectTransform));
        hudPanel.transform.SetParent(wristCanvas.transform, false);
        Stretch(hudPanel.GetComponent<RectTransform>());
        Image hudBg = hudPanel.AddComponent<Image>();
        hudBg.color = new Color(0.07f, 0.09f, 0.13f, 0.75f);

        TextMeshProUGUI progressText = CreateTMP(hudPanel.transform, "Progress_Text", "--", 72f, new Vector2(0f, 120f), new Vector2(300f, 100f));
        TextMeshProUGUI timerText = CreateTMP(hudPanel.transform, "Timer_Text", "00:00", 56f, new Vector2(0f, 30f), new Vector2(300f, 80f));
        Button resetButton = CreateButton(hudPanel.transform, "Reset_Button", "RESET", new Vector2(0f, -70f), new Vector2(220f, 70f));
        Button toggleButton = CreateButton(wristCanvas.transform, "Toggle_Button", "HUD", new Vector2(0f, -175f), new Vector2(120f, 55f));

        Wrist_HUD wristHud = wristCanvas.gameObject.AddComponent<Wrist_HUD>();
        SetRef(wristHud, "ProgressText", progressText);
        SetRef(wristHud, "TimerText", timerText);
        SetRef(wristHud, "ResetButton", resetButton);
        SetRef(wristHud, "ToggleButton", toggleButton);
        SetRef(wristHud, "TogglePanel", hudPanel);

        // Menu hub
        Canvas menu = CreateWorldCanvas("Menu_Hub", new Vector2(800f, 600f), 0.0015f, new Vector3(0f, 1.4f, 1.8f));
        menu.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        AddBackground(menu.transform, new Color(0.07f, 0.09f, 0.13f, 0.85f));
        TextMeshProUGUI title = CreateTMP(menu.transform, "Title", "CNC Mill VR Training", 56f, new Vector2(0f, 230f), new Vector2(720f, 90f));
        title.fontStyle = FontStyles.Bold;
        TextMeshProUGUI status = CreateTMP(menu.transform, "Status_Text", "M1 not started", 30f, new Vector2(0f, 150f), new Vector2(720f, 60f));
        Button m1Button = CreateButton(menu.transform, "M1_Button", "M1 — CNC Milling: What & Why", new Vector2(0f, 40f), new Vector2(620f, 90f));
        Button m2Button = CreateButton(menu.transform, M2ButtonName, "M2 — System Startup & Program Execution", new Vector2(0f, -75f), new Vector2(620f, 90f));
        m2Button.interactable = false;
        Button m3Button = CreateButton(menu.transform, "M3_Button", "M3 — Safety & LOTO (coming soon)", new Vector2(0f, -190f), new Vector2(620f, 90f));
        m3Button.interactable = false;

        // Managers
        GameObject managers = new GameObject("Managers");
        Data_Loader dataLoader = managers.AddComponent<Data_Loader>();
        SetVal(dataLoader, "FileName", "training_data.json");
        Module_Loader moduleLoader = managers.AddComponent<Module_Loader>();
        SetRef(moduleLoader, "Fader", fader);
        SetRef(moduleLoader, "MenuRoot", menu.gameObject);
        Lesson_Controller controller = managers.AddComponent<Lesson_Controller>();
        SetRef(controller, "DataLoader", dataLoader);
        SetRef(controller, "ModuleLoader", moduleLoader);
        SetRef(controller, "MenuStatusText", status);
        Lesson_Definition m1Lesson = AssetDatabase.LoadAssetAtPath<Lesson_Definition>(LessonsDir + "/M1_Lesson.asset");
        Lesson_Definition m2Lesson = AssetDatabase.LoadAssetAtPath<Lesson_Definition>(LessonsDir + "/M2_Lesson.asset");

        SetRef(wristHud, "Controller", controller);
        WireModuleButton(controller, m1Button, m1Lesson, 0);

        if (m2Lesson != null)
            WireModuleButton(controller, m2Button, m2Lesson, 1);

        EditorSceneManager.SaveScene(scene, BootstrapScenePath);
        Debug.Log("Training_Builder: Bootstrap scene built.");
    }

    // ------------------------------------------------------------------
    // 3. Module 1 scene
    // ------------------------------------------------------------------

    [MenuItem("Training/3 Build Module1 Scene")]
    public static void BuildModule1Scene(){
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateDirectionalLight();
        CreateFloor();

        // --- Mill wrapper ---
        GameObject millPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MillPrefabPath);
        if (millPrefab == null){ Debug.LogError($"Training_Builder: mill prefab missing at {MillPrefabPath}"); return; }

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
            Debug.LogError("Training_Builder: mill door nodes not found — check model hierarchy names.");
            return;
        }

        // The prefab already carries the tested mill rig: MillingAnimation on
        // MillController driving WB_XAxis_Drive (machine X = world X),
        // WB_YAxis_Drive (machine Y = world Z, carries the X stage as a
        // dependent) and SpindleMotor (machine Z = world Y). Reuse it instead
        // of duplicating axis components on the static assemblies.
        MillingAnimation milling = mill.GetComponentInChildren<MillingAnimation>();
        if (milling == null){ Debug.LogError("Training_Builder: no MillingAnimation in the mill prefab — check the prefab rig."); return; }

        SerializedObject millingSO = new SerializedObject(milling);
        AxisMovement tableAxis = millingSO.FindProperty("worktableX").objectReferenceValue as AxisMovement;
        AxisMovement saddleAxis = millingSO.FindProperty("worktableZ").objectReferenceValue as AxisMovement;
        AxisMovement spindleAxis = millingSO.FindProperty("spindleY").objectReferenceValue as AxisMovement;

        if (tableAxis == null || saddleAxis == null || spindleAxis == null){
            Debug.LogError("Training_Builder: MillingAnimation axes not wired in the mill prefab — check the prefab rig.");
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

        // --- Prompt panel ---
        GameObject promptPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsDir + "/Prompt_Panel.prefab");
        GameObject prompt = (GameObject)PrefabUtility.InstantiatePrefab(promptPrefab);
        prompt.transform.position = new Vector3(-1.1f, 1.45f, 1.2f);
        TextMeshProUGUI promptText = prompt.transform.Find("Background/Prompt_Text").GetComponent<TextMeshProUGUI>();
        Button continueButton = prompt.transform.Find("Background/Continue_Button").GetComponent<Button>();

        // --- Results panel ---
        Canvas resultsCanvas = CreateWorldCanvas("Results_Panel", new Vector2(700f, 460f), 0.0012f, new Vector3(0f, 1.5f, 1.0f));
        resultsCanvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        resultsCanvas.gameObject.AddComponent<Face_Camera>();
        AddBackground(resultsCanvas.transform, new Color(0.07f, 0.09f, 0.13f, 0.92f));
        TextMeshProUGUI resultsText = CreateTMP(resultsCanvas.transform, "Results_Text", "Results", 38f, new Vector2(0f, 70f), new Vector2(620f, 260f));
        Button retryButton = CreateButton(resultsCanvas.transform, "Retry_Button", "Retry Practice", new Vector2(-165f, -150f), new Vector2(290f, 85f));
        Button returnButton = CreateButton(resultsCanvas.transform, "Return_Button", "Return to Menu", new Vector2(165f, -150f), new Vector2(290f, 85f));

        // --- Lesson manager ---
        GameObject manager = new GameObject("Lesson_Manager");
        Lesson_Sequencer sequencer = manager.AddComponent<Lesson_Sequencer>();
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
        Component_Marker guardMarker = null;

        foreach (Component_Marker m in markerGroup.GetComponentsInChildren<Component_Marker>(true))
            if (m.Marker_Id == "guard_door"){ guardMarker = m; break; }

        SetRef(doorToggle, "DoorMarker", guardMarker);
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
        SetRef(sequencer, "PromptPanel", prompt);
        SetRef(sequencer, "PromptText", promptText);
        SetRef(sequencer, "ContinueButton", continueButton);
        SetRef(sequencer, "ResultsPanel", resultsCanvas.gameObject);
        SetRef(sequencer, "ResultsText", resultsText);
        sequencer.RetryButton = retryButton;
        sequencer.ReturnButton = returnButton;

        EditorSceneManager.SaveScene(scene, Module1ScenePath);
        Debug.Log($"Training_Builder: Module1 scene built. Mill bounds: center {millBounds.center}, size {millBounds.size}");
    }

    // ------------------------------------------------------------------
    // 3B. Module 2 scene (system startup) + Bootstrap wiring
    // ------------------------------------------------------------------

    // Shared by BuildBootstrapScene (clean build) and Wire Module2 Into Bootstrap
    // (surgical patch). Idempotent: upserts the lesson into AvailableModules and
    // clears existing onClick persistent listeners before adding exactly one.
    private static void WireModuleButton(Lesson_Controller controller, Button button, Lesson_Definition lesson, int index){
        SerializedObject so = new SerializedObject(controller);
        SerializedProperty modules = so.FindProperty("AvailableModules");
        int found = -1;

        for (int i = 0; i < modules.arraySize; i++){
            Lesson_Definition d = modules.GetArrayElementAtIndex(i).objectReferenceValue as Lesson_Definition;
            if (d != null && d.Module_Id == lesson.Module_Id){ found = i; break; }
        }

        if (found < 0){
            if (modules.arraySize < index + 1)
                modules.arraySize = index + 1;
            modules.GetArrayElementAtIndex(index).objectReferenceValue = lesson;
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        button.interactable = true;

        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);

        UnityEventTools.AddIntPersistentListener(button.onClick, controller.Start_Module, index);
    }

    [MenuItem("Training/2B Wire Module2 Into Bootstrap")]
    public static void WireModule2IntoBootstrap(){
        var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
        Lesson_Controller controller = Object.FindFirstObjectByType<Lesson_Controller>();
        if (controller == null){ Debug.LogError("Training_Builder: no Lesson_Controller in Bootstrap."); return; }

        Lesson_Definition m2Lesson = AssetDatabase.LoadAssetAtPath<Lesson_Definition>(LessonsDir + "/M2_Lesson.asset");
        if (m2Lesson == null){ Debug.LogError("Training_Builder: M2_Lesson.asset missing — run 'Training/1 Build Shared Assets' first."); return; }

        Button m2Button = null;
        foreach (Button b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (b.name == M2ButtonName){ m2Button = b; break; }

        if (m2Button == null){ Debug.LogError($"Training_Builder: '{M2ButtonName}' not found in Bootstrap."); return; }

        WireModuleButton(controller, m2Button, m2Lesson, 1);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Training_Builder: Module 2 wired into Bootstrap.");
    }

    [MenuItem("Training/3B Build Module2 Scene")]
    public static void BuildModule2Scene(){
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateDirectionalLight();
        CreateFloor();

        // Root the Action_Button_Registry scans for every clickable startup action.
        GameObject content = new GameObject("Module2");

        // --- Mill (reuse the wired M1 prefab: mill + vise + demo block) ---
        GameObject millPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsDir + "/PM8000_Training.prefab");
        if (millPrefab == null){ Debug.LogError("Training_Builder: PM8000_Training.prefab missing — run 'Training/3 Build Module1 Scene' first."); return; }

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
            BuildKaigAction(content.transform, "mill_power_on", RendererBounds(kaig));
        else
            Debug.LogWarning("Training_Builder: 'kaig' part not found on the mill — mill main power has no clickable switch.");

        // --- Prompt panel (reused) ---
        GameObject promptPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsDir + "/Prompt_Panel.prefab");
        GameObject prompt = (GameObject)PrefabUtility.InstantiatePrefab(promptPrefab);
        prompt.transform.position = new Vector3(-2.443f, 1.128f, 2.889f);
        prompt.transform.rotation = Quaternion.Euler(0f, -20f, 0f);
        TextMeshProUGUI promptText = prompt.transform.Find("Background/Prompt_Text").GetComponent<TextMeshProUGUI>();
        Button continueButton = prompt.transform.Find("Background/Continue_Button").GetComponent<Button>();

        // --- Results panel ---
        Canvas resultsCanvas = CreateWorldCanvas("Results_Panel", new Vector2(700f, 460f), 0.0012f, new Vector3(0.845f, 1.064f, 1.697f));
        resultsCanvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        resultsCanvas.gameObject.AddComponent<Face_Camera>();
        AddBackground(resultsCanvas.transform, new Color(0.07f, 0.09f, 0.13f, 0.92f));
        TextMeshProUGUI resultsText = CreateTMP(resultsCanvas.transform, "Results_Text", "Results", 36f, new Vector2(0f, 70f), new Vector2(620f, 260f));
        Button retryButton = CreateButton(resultsCanvas.transform, "Retry_Button", "Retry Practice", new Vector2(-165f, -150f), new Vector2(290f, 85f));
        Button returnButton = CreateButton(resultsCanvas.transform, "Return_Button", "Return to Menu", new Vector2(165f, -150f), new Vector2(290f, 85f));

        // --- Manager ---
        GameObject manager = new GameObject("Lesson_Manager");
        Lesson_Sequencer sequencer = manager.AddComponent<Lesson_Sequencer>();
        Action_Button_Registry actionRegistry = manager.AddComponent<Action_Button_Registry>();
        Startup_State_Controller stateController = manager.AddComponent<Startup_State_Controller>();

        SetRef(sequencer, "PromptPanel", prompt);
        SetRef(sequencer, "PromptText", promptText);
        SetRef(sequencer, "ContinueButton", continueButton);
        SetRef(sequencer, "ResultsPanel", resultsCanvas.gameObject);
        SetRef(sequencer, "ResultsText", resultsText);
        sequencer.RetryButton = retryButton;
        sequencer.ReturnButton = returnButton;

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
        Debug.Log("Training_Builder: Module2 scene built.");
    }

    private static GameObject InstantiateProp(GameObject asset, Transform parent, string name, Vector3 pos, Quaternion rot){
        if (asset == null){ Debug.LogWarning($"Training_Builder: prop asset for '{name}' missing."); return null; }
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
        go.transform.rotation = rot;
        return go;
    }

    // A centered, zero-size pass-through container under a panel canvas. Its children
    // use the same canvas-center-relative coordinates as direct canvas children, so a
    // tab's buttons can be toggled active/inactive without shifting their layout.
    private static GameObject BuildTabContent(Transform canvas, string name){
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(canvas, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        return go;
    }

    private static Startup_Action_Button BuildActionButton(Transform parent, string actionId, string label, Vector2 pos, Vector2 size){
        GameObject highlight = new GameObject("Highlight_" + actionId, typeof(RectTransform));
        highlight.transform.SetParent(parent, false);
        RectTransform hr = highlight.GetComponent<RectTransform>();
        hr.sizeDelta = size + new Vector2(18f, 18f);
        hr.anchoredPosition = pos;
        Image hi = highlight.AddComponent<Image>();
        hi.color = new Color(1f, 0.85f, 0.2f, 0.95f);
        hi.raycastTarget = false;
        highlight.SetActive(false);

        Button button = CreateButton(parent, "Btn_" + actionId, label, pos, size);
        Startup_Action_Button sab = button.gameObject.AddComponent<Startup_Action_Button>();
        SetVal(sab, "Action_Id", actionId);
        SetRef(sab, "Highlight", highlight);
        return sab;
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

    // Makes a real machine part clickable: a trigger BoxCollider + XRSimpleInteractable
    // sized to the part's bounds, an Action_Interactable that fires the action, and a
    // translucent bounding-box cube that marks it as clickable. Mirrors the
    // Component_Marker prefab but feeds Notify_Action instead of marker selection.
    private static void BuildKaigAction(Transform parent, string actionId, Bounds bounds){
        Material glow = AssetDatabase.LoadAssetAtPath<Material>(MaterialsDir + "/Marker_Glow.mat");

        GameObject root = new GameObject("Action_" + actionId);
        root.transform.SetParent(parent, true);
        root.transform.position = bounds.center;

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = bounds.size * 1.1f;

        UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable =
            root.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        interactable.colliders.Add(collider);

        Action_Interactable action = root.AddComponent<Action_Interactable>();
        SetVal(action, "Action_Id", actionId);

        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "Bounding_Box";
        Object.DestroyImmediate(box.GetComponent<BoxCollider>());
        box.transform.SetParent(root.transform, false);
        box.transform.localScale = bounds.size * 1.1f;
        MeshRenderer boxRenderer = box.GetComponent<MeshRenderer>();
        boxRenderer.sharedMaterial = glow;
        boxRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        SetRef(action, "BoundingBox", boxRenderer);
    }

    private static Material LoadOrCreateMaterial(string path, Color color){
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat != null)
            return mat;

        mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // ------------------------------------------------------------------
    // 4. Build settings
    // ------------------------------------------------------------------

    [MenuItem("Training/4 Add Scenes To Build Settings")]
    public static void AddScenesToBuildSettings(){
        var scenes = new List<EditorBuildSettingsScene>{
            new EditorBuildSettingsScene(BootstrapScenePath, true),
            new EditorBuildSettingsScene(Module1ScenePath, true),
            new EditorBuildSettingsScene(Module2ScenePath, true)
        };

        foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            if (existing.path != BootstrapScenePath && existing.path != Module1ScenePath && existing.path != Module2ScenePath)
                scenes.Add(existing);

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("Training_Builder: build settings updated (Bootstrap is index 0).");
    }

    // ------------------------------------------------------------------
    // 5. Marker placement (re-run after a model swap)
    // ------------------------------------------------------------------

    [MenuItem("Training/5 Reposition Markers In Open Scene")]
    public static void RepositionMarkersInOpenScene(){
        GameObject wrapper = GameObject.Find("PM8000_Training");
        GameObject group = GameObject.Find("Component_Markers");

        if (wrapper == null || group == null){
            Debug.LogError("Training_Builder: PM8000_Training or Component_Markers not found in the open scene.");
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
        if (doorToggle != null){
            Component_Marker guardMarker = null;
            foreach (Component_Marker m in group.GetComponentsInChildren<Component_Marker>(true))
                if (m.Marker_Id == "guard_door"){ guardMarker = m; break; }
            SetRef(doorToggle, "DoorMarker", guardMarker);
        }

        EditorSceneManager.MarkSceneDirty(group.scene);
        Debug.Log("Training_Builder: markers repositioned from current model bounds.");
    }

    private static void BuildMarkers(Transform group, Transform mill, Transform vice){
        GameObject markerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsDir + "/Component_Marker.prefab");

        foreach (MarkerDef def in Markers){
            Bounds bounds;

            if (def.Id == "vice" && vice != null){
                bounds = RendererBounds(vice);
            }
            else{
                Transform node = def.NodePath != null ? mill.Find(def.NodePath) : null;

                if (node == null){
                    Debug.LogWarning($"Training_Builder: node not found for marker '{def.Id}' ({def.NodePath}) — skipped.");
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

            GameObject markerGO = (GameObject)PrefabUtility.InstantiatePrefab(markerPrefab);
            markerGO.name = "Marker_" + def.Id;
            markerGO.transform.SetParent(group, true);
            markerGO.transform.position = bounds.center;

            Component_Marker marker = markerGO.GetComponent<Component_Marker>();
            marker.Marker_Id = def.Id;
            marker.Display_Name = def.Display;
            PrefabUtility.RecordPrefabInstancePropertyModifications(marker);

            BoxCollider collider = markerGO.GetComponent<BoxCollider>();
            collider.size = bounds.size * 1.02f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(collider);

            Transform shell = markerGO.transform.Find("Glow_Shell");
            shell.localScale = bounds.size * 1.03f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(shell);

            Transform label = markerGO.transform.Find("Label");
            label.localPosition = new Vector3(0f, bounds.size.y / 2f + 0.12f, 0f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(label);

            TextMeshProUGUI labelText = label.GetComponentInChildren<TextMeshProUGUI>(true);
            labelText.text = def.Display;
            PrefabUtility.RecordPrefabInstancePropertyModifications(labelText);
        }
    }

    [MenuItem("Training/6 Import XR Device Simulator")]
    public static void ImportDeviceSimulator(){
        foreach (var sample in UnityEditor.PackageManager.UI.Sample.FindByPackage("com.unity.xr.interaction.toolkit", "3.3.1")){
            if (sample.displayName == "XR Device Simulator"){
                sample.Import(UnityEditor.PackageManager.UI.Sample.ImportOptions.OverridePreviousImports);
                Debug.Log("Training_Builder: XR Device Simulator imported. Enable 'Use XR Device Simulator in scenes' in Project Settings > XR Plug-in Management > XR Interaction Toolkit to use it.");
                return;
            }
        }

        Debug.LogError("Training_Builder: XR Device Simulator sample not found for XRI 3.3.1.");
    }

    // ------------------------------------------------------------------
    // Play-mode debug drivers (no-headset flow testing)
    // ------------------------------------------------------------------

    [MenuItem("Training/8 Debug - Start Module 1")]
    public static void DebugStartModule(){
        if (!Application.isPlaying){ Debug.LogError("Training_Builder: enter play mode first (from Bootstrap)."); return; }
        Lesson_Controller controller = Object.FindFirstObjectByType<Lesson_Controller>();
        if (controller == null){ Debug.LogError("Training_Builder: no Lesson_Controller found."); return; }
        controller.Start_Module(0);
    }

    [MenuItem("Training/8 Debug - Start Module 2")]
    public static void DebugStartModule2(){
        if (!Application.isPlaying){ Debug.LogError("Training_Builder: enter play mode first (from Bootstrap)."); return; }
        Lesson_Controller controller = Object.FindFirstObjectByType<Lesson_Controller>();
        if (controller == null){ Debug.LogError("Training_Builder: no Lesson_Controller found."); return; }
        controller.Start_Module(1);
    }

    // Drives the currently-loaded lesson to completion synchronously (guided then
    // practice — the guided->practice transition is synchronous). Verifies the
    // whole flow reaches its end state without 30 manual steps.
    [MenuItem("Training/8 Debug - Auto Run To Completion")]
    public static void DebugAutoRunToCompletion(){
        if (!Application.isPlaying){ Debug.LogError("Training_Builder: enter play mode first."); return; }
        Lesson_Sequencer seq = Object.FindFirstObjectByType<Lesson_Sequencer>();
        if (seq == null){ Debug.LogError("Training_Builder: no Lesson_Sequencer (load a module first)."); return; }

        var advance = typeof(Lesson_Sequencer).GetMethod("Advance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        int guard = 0;
        while (seq.Current_Step != null && guard < 200){
            Lesson_Step step = seq.Current_Step;

            if (step.Kind == Lesson_Step_Kind.Panel_Action)
                seq.Notify_Action(step.Target_Marker_Id);
            else
                advance.Invoke(seq, null); // Info/other — advance directly (reliable, no button-invoke timing)

            guard++;
        }

        Debug.Log($"Training_Builder: auto-run finished after {guard} steps; completed={seq.Current_Step == null}; persistentDataPath={Application.persistentDataPath}");
    }

    [MenuItem("Training/8 Debug - Auto Step (Correct)")]
    public static void DebugAutoStep(){
        DebugStep(true);
    }

    [MenuItem("Training/8 Debug - Auto Step (Wrong Answer)")]
    public static void DebugAutoStepWrong(){
        DebugStep(false);
    }

    [MenuItem("Training/8 Debug - Begin Practice Directly")]
    public static void DebugBeginPractice(){
        if (!Application.isPlaying) return;
        Lesson_Sequencer sequencer = Object.FindFirstObjectByType<Lesson_Sequencer>();
        Lesson_Definition def = AssetDatabase.LoadAssetAtPath<Lesson_Definition>(LessonsDir + "/M1_Lesson.asset");
        if (sequencer == null){ Debug.LogError("Training_Builder: no Lesson_Sequencer (load Module 1 first)."); return; }
        sequencer.Begin(def, Lesson_Mode.Practice);
    }

    [MenuItem("Training/8 Debug - Click Retry")]
    public static void DebugClickRetry(){
        if (!Application.isPlaying) return;
        Object.FindFirstObjectByType<Lesson_Sequencer>().RetryButton.onClick.Invoke();
    }

    [MenuItem("Training/8 Debug - Click Return To Menu")]
    public static void DebugClickReturn(){
        if (!Application.isPlaying) return;
        Object.FindFirstObjectByType<Lesson_Sequencer>().ReturnButton.onClick.Invoke();
    }

    [MenuItem("Training/8 Debug - Click Guard Door")]
    public static void DebugClickGuardDoor(){
        if (!Application.isPlaying) return;
        Door_Click_Toggle toggle = Object.FindFirstObjectByType<Door_Click_Toggle>();
        Marker_Registry registry = Object.FindFirstObjectByType<Marker_Registry>();
        if (toggle == null || registry == null){ Debug.LogError("Training_Builder: door toggle or registry missing."); return; }
        toggle.On_Door_Selected(registry.Resolve("guard_door"));
        Debug.Log("Training_Builder: guard door click simulated.");
    }

    [MenuItem("Training/8 Debug - Click Power On")]
    public static void DebugClickPowerOn(){ DebugClickStateToggle("power_on"); }

    [MenuItem("Training/8 Debug - Click Emergency Stop")]
    public static void DebugClickEmergencyStop(){ DebugClickStateToggle("emergency_stop"); }

    private static void DebugClickStateToggle(string markerId){
        if (!Application.isPlaying) return;
        Marker_Registry registry = Object.FindFirstObjectByType<Marker_Registry>();
        if (registry == null){ Debug.LogError("Training_Builder: registry missing."); return; }

        Component_Marker marker = registry.Resolve(markerId);

        foreach (Marker_State_Toggle toggle in Object.FindObjectsByType<Marker_State_Toggle>(FindObjectsSortMode.None)){
            if (new SerializedObject(toggle).FindProperty("Marker").objectReferenceValue == (Object)marker){
                toggle.On_Selected(marker);
                Debug.Log($"Training_Builder: {markerId} click simulated.");
                return;
            }
        }

        Debug.LogError($"Training_Builder: no Marker_State_Toggle for '{markerId}'.");
    }

    [MenuItem("Training/8 Debug - Click Door Unlock")]
    public static void DebugClickDoorUnlock(){
        if (!Application.isPlaying) return;
        Marker_Registry registry = Object.FindFirstObjectByType<Marker_Registry>();
        if (registry == null){ Debug.LogError("Training_Builder: registry missing."); return; }

        Component_Marker marker = registry.Resolve("door_unlock");

        foreach (Door_Click_Toggle toggle in Object.FindObjectsByType<Door_Click_Toggle>(FindObjectsSortMode.None)){
            if (new SerializedObject(toggle).FindProperty("DoorMarker").objectReferenceValue == (Object)marker){
                toggle.On_Door_Selected(marker);
                Debug.Log("Training_Builder: door unlock click simulated.");
                return;
            }
        }

        Debug.LogError("Training_Builder: no Door_Click_Toggle for 'door_unlock'.");
    }

    [MenuItem("Training/8 Debug - Click Wrist Reset")]
    public static void DebugClickWristReset(){
        if (!Application.isPlaying) return;
        Wrist_HUD hud = Object.FindFirstObjectByType<Wrist_HUD>(FindObjectsInactive.Include);

        if (hud != null && hud.gameObject.activeInHierarchy){
            ((Button)new SerializedObject(hud).FindProperty("ResetButton").objectReferenceValue).onClick.Invoke();
            Debug.Log("Training_Builder: wrist reset button invoked.");
            return;
        }

        // Without a tracked controller the XRI modality manager deactivates the
        // controller GameObjects (and the wrist HUD with them) — call the action directly.
        Object.FindFirstObjectByType<Lesson_Controller>().Restart_Phase();
        Debug.Log("Training_Builder: wrist HUD inactive (no XR device) — Restart_Phase called directly.");
    }

    [MenuItem("Training/8 Debug - Toggle Time Scale 5x")]
    public static void DebugTimeScale(){
        Time.timeScale = Mathf.Approximately(Time.timeScale, 1f) ? 5f : 1f;
        Debug.Log($"Training_Builder: timeScale = {Time.timeScale}");
    }

    private static void DebugStep(bool correct){
        if (!Application.isPlaying) return;
        Lesson_Sequencer sequencer = Object.FindFirstObjectByType<Lesson_Sequencer>();

        if (sequencer == null || sequencer.Current_Step == null){
            Debug.Log("Training_Builder: no active lesson step.");
            return;
        }

        Lesson_Step step = sequencer.Current_Step;
        Debug.Log($"Training_Builder: auto-step '{step.Step_Id}' ({step.Kind}) {sequencer.Step_Index + 1}/{sequencer.Step_Count} mode {sequencer.Mode}");

        switch (step.Kind){
            case Lesson_Step_Kind.Info:
                ((Button)new SerializedObject(sequencer).FindProperty("ContinueButton").objectReferenceValue).onClick.Invoke();
                break;

            case Lesson_Step_Kind.Select_Component:{
                Marker_Registry registry = Object.FindFirstObjectByType<Marker_Registry>();
                Component_Marker target = registry.Resolve(step.Target_Marker_Id);
                Debug.Log($"Training_Builder: registry holds {registry.All.Count} markers; resolve '{step.Target_Marker_Id}' -> {(target != null ? target.name : "NULL")}");

                if (target == null)
                    return;

                if (!correct){
                    foreach (Component_Marker other in registry.All)
                        if (other != target){ target = other; break; }
                }

                sequencer.Notify_Marker_Selected(target);
                break;
            }

            case Lesson_Step_Kind.Panel_Action:{
                string targetId = correct ? step.Target_Marker_Id : "__out_of_order__";
                sequencer.Notify_Action(targetId);
                break;
            }

            default:
                Debug.Log("Training_Builder: demo step — waiting for Demo_Finished.");
                break;
        }
    }

    [MenuItem("Training/9 Dump Mill Diagnostics")]
    public static void DumpDiagnostics(){
        GameObject wrapper = GameObject.Find("PM8000_Training");

        if (wrapper == null){
            Debug.LogError("Training_Builder: no PM8000_Training in open scene.");
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

        Debug.Log("Training_Builder diagnostics:\n" + sb);
    }

    // ------------------------------------------------------------------
    // Helpers
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

    private static void CreateDirectionalLight(){
        GameObject lightGO = new GameObject("Directional Light");
        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void CreateFloor(){
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.localScale = new Vector3(2f, 1f, 2f);
        Material floorMat = AssetDatabase.LoadAssetAtPath<Material>(FloorMatPath);

        if (floorMat != null)
            floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;
    }

    private static GameObject CreateIndicator(Transform parent, string text, Color color, Vector3 position){
        Canvas canvas = CreateWorldCanvas("Indicator_" + text[0], new Vector2(500f, 100f), 0.0018f, position);
        canvas.transform.SetParent(parent, true);
        canvas.gameObject.AddComponent<Face_Camera>();
        AddBackground(canvas.transform, new Color(0.05f, 0.05f, 0.08f, 0.8f));
        TextMeshProUGUI tmp = CreateTMP(canvas.transform, "Text", text, 48f, Vector2.zero, new Vector2(480f, 90f));
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        canvas.gameObject.SetActive(false);
        return canvas.gameObject;
    }

    private static Canvas CreateWorldCanvas(string name, Vector2 sizePx, float scale, Vector3 position){
        GameObject go = new GameObject(name, typeof(RectTransform));
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = sizePx;
        rect.localScale = Vector3.one * scale;
        rect.position = position;
        return canvas;
    }

    private static Image AddBackground(Transform parent, Color color){
        GameObject bg = new GameObject("Background", typeof(RectTransform));
        bg.transform.SetParent(parent, false);
        bg.transform.SetAsFirstSibling();
        Stretch(bg.GetComponent<RectTransform>());
        Image image = bg.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI CreateTMP(Transform parent, string name, string text, float fontSize, Vector2 anchoredPos, Vector2 size){
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return tmp;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size){
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;
        Image image = go.AddComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.color = new Color(0.22f, 0.45f, 0.75f, 1f);
        Button button = go.AddComponent<Button>();
        TextMeshProUGUI tmp = CreateTMP(go.transform, "Text", label, Mathf.Min(size.y * 0.42f, 36f), Vector2.zero, size);
        tmp.raycastTarget = false;
        return button;
    }

    private static void Stretch(RectTransform rect){
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Bounds RendererBounds(Transform root){
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return new Bounds(root.position, Vector3.one * 0.1f);

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    private static Transform FindChild(Transform root, string name){
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name)
                return t;

        return null;
    }

    private static Component_Marker FindMarker(GameObject group, string id){
        foreach (Component_Marker m in group.GetComponentsInChildren<Component_Marker>(true))
            if (m.Marker_Id == id)
                return m;

        return null;
    }

    // Adds a Marker_State_Toggle that flips the cue color on the mill part at
    // nodePath when its marker is selected. Skips (with a warning) if either the
    // marker or the part renderer is missing.
    private static void WireStateToggle(GameObject manager, GameObject group, Transform mill,
            Lesson_Sequencer sequencer, string markerId, string nodePath, string label, Color onColor){
        Component_Marker marker = FindMarker(group, markerId);
        Transform node = mill.Find(nodePath);
        Renderer rend = node != null ? node.GetComponentInChildren<Renderer>() : null;

        if (marker == null || rend == null){
            Debug.LogWarning($"Training_Builder: state toggle '{markerId}' skipped (marker {(marker != null)}, renderer {(rend != null)}).");
            return;
        }

        Marker_State_Toggle toggle = manager.AddComponent<Marker_State_Toggle>();
        SetRef(toggle, "Marker", marker);
        SetRef(toggle, "TargetRenderer", rend);
        SetRef(toggle, "Sequencer", sequencer);
        SetVal(toggle, "ActionLabel", label);

        // SetVal has no Color branch — set OnColor directly.
        SerializedObject so = new SerializedObject(toggle);
        so.FindProperty("OnColor").colorValue = onColor;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetRef(Component component, string property, Object value){
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(property);

        if (prop == null){
            Debug.LogError($"Training_Builder: property '{property}' not found on {component.GetType().Name}");
            return;
        }

        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetRefArray(Component component, string property, List<Object> values){
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(property);

        if (prop == null){
            Debug.LogError($"Training_Builder: property '{property}' not found on {component.GetType().Name}");
            return;
        }

        prop.arraySize = values.Count;

        for (int i = 0; i < values.Count; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AppendRefArray(Component component, string property, List<Object> values){
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(property);

        if (prop == null){
            Debug.LogError($"Training_Builder: property '{property}' not found on {component.GetType().Name}");
            return;
        }

        foreach (Object value in values){
            prop.arraySize++;
            prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = value;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetVal(Component component, string property, object value){
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(property);

        if (prop == null){
            Debug.LogError($"Training_Builder: property '{property}' not found on {component.GetType().Name}");
            return;
        }

        if (value is float f) prop.floatValue = f;
        else if (value is int i) prop.intValue = i;
        else if (value is bool b) prop.boolValue = b;
        else if (value is string s) prop.stringValue = s;

        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
