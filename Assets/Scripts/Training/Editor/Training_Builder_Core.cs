using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

// What every module scene shares: the Lesson_Manager object and its wired
// Lesson_Sequencer. Returned by Training_Builder_Core.BuildLessonScaffold.
public class Module_Scaffold{
    public GameObject Manager;
    public Lesson_Sequencer Sequencer;
}

// Shared, module-agnostic half of the training scene generator. Per-module
// builders (ITraining_Module_Builder implementations under each member's
// Editor folder) call into this for scene scaffolding, UI factories, framework
// prefabs and serialized-property plumbing. Module content lives in those
// builders — nothing in this file should name a specific machine or module.
public static class Training_Builder_Core{
    public const string FrameworkDir = "Assets/Training";
    public const string FrameworkPrefabsDir = FrameworkDir + "/Prefabs";
    public const string FrameworkMaterialsDir = FrameworkDir + "/Materials";
    public const string RegistryAssetPath = FrameworkDir + "/Training_Modules.asset";
    public const string GlowMaterialPath = FrameworkMaterialsDir + "/Marker_Glow.mat";
    public const string ComponentMarkerPrefabPath = FrameworkPrefabsDir + "/Component_Marker.prefab";
    public const string PromptPanelPrefabPath = FrameworkPrefabsDir + "/Prompt_Panel.prefab";

    public const string RigPrefabPath = "Assets/Game_Objects/Amogus.prefab";
    public const string FloorMatPath = "Assets/Materials/Light_Grey.mat";

    // ------------------------------------------------------------------
    // 0. Full build: framework assets -> every module builder -> bootstrap
    // ------------------------------------------------------------------

    [MenuItem("Training/0 Build Everything")]
    public static void BuildEverything(){
        BuildFrameworkAssets();

        foreach (ITraining_Module_Builder builder in AllModuleBuilders()){
            Debug.Log($"Training_Builder_Core: building module via {builder.GetType().Name} (order {builder.Order}).");
            builder.Build();
        }

        BuildBootstrapScene();
        AddScenesToBuildSettings();
        Debug.Log("Training_Builder_Core: full build complete.");
    }

    private static List<ITraining_Module_Builder> AllModuleBuilders(){
        List<ITraining_Module_Builder> builders = new List<ITraining_Module_Builder>();

        foreach (System.Type type in TypeCache.GetTypesDerivedFrom<ITraining_Module_Builder>())
            if (!type.IsAbstract && !type.IsInterface)
                builders.Add((ITraining_Module_Builder)System.Activator.CreateInstance(type));

        builders.Sort((a, b) => a.Order.CompareTo(b.Order));
        return builders;
    }

    // ------------------------------------------------------------------
    // 1. Framework assets (module-agnostic prefabs + materials)
    // ------------------------------------------------------------------

    [MenuItem("Training/1 Build Framework Assets")]
    public static void BuildFrameworkAssets(){
        EnsureFolderPath(FrameworkPrefabsDir);
        EnsureFolderPath(FrameworkMaterialsDir);
        BuildMarkerGlowMaterial();
        BuildComponentMarkerPrefab();
        BuildPromptPanelPrefab();
        AssetDatabase.SaveAssets();
        Debug.Log("Training_Builder_Core: framework assets built.");
    }

    private static void BuildMarkerGlowMaterial(){
        if (AssetDatabase.LoadAssetAtPath<Material>(GlowMaterialPath) != null)
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
        AssetDatabase.CreateAsset(mat, GlowMaterialPath);
    }

    private static void BuildComponentMarkerPrefab(){
        Material glow = AssetDatabase.LoadAssetAtPath<Material>(GlowMaterialPath);

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

        PrefabUtility.SaveAsPrefabAsset(rootGO, ComponentMarkerPrefabPath);
        Object.DestroyImmediate(rootGO);
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

        PrefabUtility.SaveAsPrefabAsset(canvas.gameObject, PromptPanelPrefabPath);
        Object.DestroyImmediate(canvas.gameObject);
    }

    // ------------------------------------------------------------------
    // Module registry
    // ------------------------------------------------------------------

    public static Training_Module_Registry LoadOrCreateRegistry(){
        Training_Module_Registry registry = AssetDatabase.LoadAssetAtPath<Training_Module_Registry>(RegistryAssetPath);

        if (registry == null){
            EnsureFolderPath(FrameworkDir);
            registry = ScriptableObject.CreateInstance<Training_Module_Registry>();
            AssetDatabase.CreateAsset(registry, RegistryAssetPath);
            Debug.Log($"Training_Builder_Core: created module registry at {RegistryAssetPath}.");
        }

        return registry;
    }

    // Upsert keyed on Scene_Path (stable, and safe when Lesson references have
    // been unloaded by scene switches mid-build). New modules append — registry
    // list order is the Bootstrap menu order and the build-settings scene order.
    public static void RegisterModule(Lesson_Definition lesson, string scenePath){
        Training_Module_Registry registry = LoadOrCreateRegistry();
        Training_Module_Registry.Entry entry = registry.Modules.Find(e => e.Scene_Path == scenePath);

        if (entry == null){
            entry = new Training_Module_Registry.Entry();
            registry.Modules.Add(entry);
        }

        entry.Lesson = lesson;
        entry.Scene_Path = scenePath;
        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();
    }

    // ------------------------------------------------------------------
    // 2. Bootstrap scene (XR rig, menu, managers) — menu comes from the registry
    // ------------------------------------------------------------------

    [MenuItem("Training/2 Build Bootstrap Scene")]
    public static void BuildBootstrapScene(){
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateDirectionalLight();
        CreateFloor();

        GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefabPath);
        if (rigPrefab == null){ Debug.LogError($"Training_Builder_Core: rig prefab missing at {RigPrefabPath}"); return; }
        GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);

        // The component markers use trigger colliders; ray interactors ignore
        // triggers by default, which makes markers unselectable. Instance override
        // only — the shared rig prefab must keep Ignore so DigitalTwin trigger
        // volumes can't block its UI rays.
        foreach (XRRayInteractor ray in rig.GetComponentsInChildren<XRRayInteractor>(true))
            SetVal(ray, "m_RaycastTriggerInteraction", (int)QueryTriggerInteraction.Collide);

        // No XRInteractionManager or InputActionManager here: the rig prefab
        // carries its own manager (a second one would split interactor/interactable
        // registration), and its Player_Input actions are enabled by
        // Player_Controller / XRI's auto-enable of referenced actions.

        // Entity_Player.LaunchXR requires both UI modules and a Spawn_Point in the
        // scene: it toggles the modules by XR availability and repositions the
        // player to the spawn.
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<XRUIInputModule>();
        InputSystemUIInputModule desktopModule = eventSystem.AddComponent<InputSystemUIInputModule>();
        // Entity_Player locks the cursor; the module's default lock behavior parks
        // the pointer at (-1,-1), making UI unclickable. ScreenCenter (as in
        // DigitalTwin) lands desktop clicks at the look direction instead.
        SetVal(desktopModule, "m_CursorLockBehavior", 1);

        GameObject spawnPoint = new GameObject("SpawnPoint");
        spawnPoint.AddComponent<Spawn_Point>();
        spawnPoint.transform.position = new Vector3(0f, 0.1f, 0f);
        rig.transform.position = spawnPoint.transform.position;

        // Fade canvas under the HMD camera
        Transform mainCamera = rig.GetComponentInChildren<Camera>(true).transform;
        Canvas fadeCanvas = CreateWorldCanvas("Fade_Canvas", new Vector2(2000f, 2000f), 0.001f, Vector3.zero);
        fadeCanvas.transform.SetParent(mainCamera, false);
        fadeCanvas.transform.localPosition = new Vector3(0f, 0f, 0.35f);
        fadeCanvas.sortingOrder = 999;
        Image fadeImage = AddBackground(fadeCanvas.transform, Color.black);
        fadeImage.raycastTarget = false;
        fadeImage.enabled = false;
        Screen_Fader fader = fadeCanvas.gameObject.AddComponent<Screen_Fader>();
        SetRef(fader, "FadeImage", fadeImage);

        // Wrist HUD on the tracked left hand (the anchor starts inactive;
        // Entity_Player.LaunchXR activates it when an XR device is present)
        Transform leftController = rig.GetComponentInChildren<Entity_Player>(true).LeftHandAnchor.transform;
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
        TextMeshProUGUI status = CreateTMP(menu.transform, "Status_Text", "—", 30f, new Vector2(0f, 150f), new Vector2(720f, 60f));

        // Managers. Data_Loader gets its own object (as in DigitalTwin): its Awake
        // calls DontDestroyOnLoad, which would drag Module_Loader along and break
        // UnloadRoutine's SetActiveScene(gameObject.scene) with the DDOL scene.
        GameObject dataLoaderGO = new GameObject("Data Loader");
        Data_Loader dataLoader = dataLoaderGO.AddComponent<Data_Loader>();
        SetVal(dataLoader, "FileName", "training_data.json");

        GameObject managers = new GameObject("Managers");
        managers.AddComponent<Desktop_Click_Select>();
        Module_Loader moduleLoader = managers.AddComponent<Module_Loader>();
        SetRef(moduleLoader, "Fader", fader);
        SetRef(moduleLoader, "MenuRoot", menu.gameObject);
        Lesson_Controller controller = managers.AddComponent<Lesson_Controller>();
        SetRef(controller, "DataLoader", dataLoader);
        SetRef(controller, "ModuleLoader", moduleLoader);
        SetRef(controller, "MenuStatusText", status);
        SetRef(wristHud, "Controller", controller);

        // Module buttons from the registry: list order = menu order. Entries with
        // a lesson are wired to Start_Module; placeholders render disabled.
        // The registry is loaded HERE (after NewScene) and lessons are resolved
        // through the serialized data — opening a scene unloads previously-loaded
        // assets, so references obtained before it go stale-null.
        Training_Module_Registry moduleRegistry = LoadOrCreateRegistry();

        if (moduleRegistry.Modules.Count == 0)
            Debug.LogWarning("Training_Builder_Core: module registry is empty — run module builds (or Training/0) first, then rebuild Bootstrap for menu buttons.");

        SerializedObject registrySO = new SerializedObject(moduleRegistry);
        SerializedProperty modulesProp = registrySO.FindProperty("Modules");
        List<Object> lessons = new List<Object>();
        float buttonY = 40f;

        for (int i = 0; i < modulesProp.arraySize; i++){
            SerializedProperty entryProp = modulesProp.GetArrayElementAtIndex(i);
            Lesson_Definition lesson = entryProp.FindPropertyRelative("Lesson").objectReferenceValue as Lesson_Definition;

            if (lesson != null){
                Button button = CreateButton(menu.transform, lesson.Module_Id + "_Button", lesson.Display_Name,
                    new Vector2(0f, buttonY), new Vector2(620f, 90f));
                UnityEventTools.AddIntPersistentListener(button.onClick, controller.Start_Module, lessons.Count);
                lessons.Add(lesson);
            }
            else{
                string placeholder = entryProp.FindPropertyRelative("Placeholder_Label").stringValue;
                string label = string.IsNullOrEmpty(placeholder) ? "Coming soon" : placeholder;
                Button button = CreateButton(menu.transform, "Placeholder_Button", label, new Vector2(0f, buttonY), new Vector2(620f, 90f));
                button.interactable = false;
            }

            buttonY -= 115f;
        }

        SetRefArray(controller, "AvailableModules", lessons);

        EditorSceneManager.SaveScene(scene, moduleRegistry.Bootstrap_Scene_Path);
        Debug.Log($"Training_Builder_Core: Bootstrap scene built ({lessons.Count} modules, {moduleRegistry.Modules.Count - lessons.Count} placeholders).");
    }

    // ------------------------------------------------------------------
    // 4. Build settings — Bootstrap first, then registry scenes, then the rest
    // ------------------------------------------------------------------

    [MenuItem("Training/4 Add Scenes To Build Settings")]
    public static void AddScenesToBuildSettings(){
        Training_Module_Registry registry = LoadOrCreateRegistry();
        List<string> trainingPaths = new List<string>{ registry.Bootstrap_Scene_Path };

        foreach (Training_Module_Registry.Entry entry in registry.Modules)
            if (!string.IsNullOrEmpty(entry.Scene_Path))
                trainingPaths.Add(entry.Scene_Path);

        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();

        foreach (string path in trainingPaths)
            scenes.Add(new EditorBuildSettingsScene(path, true));

        foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            if (!trainingPaths.Contains(existing.path))
                scenes.Add(existing);

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("Training_Builder_Core: build settings updated (Bootstrap is index 0).");
    }

    [MenuItem("Training/6 Import XR Device Simulator")]
    public static void ImportDeviceSimulator(){
        foreach (var sample in UnityEditor.PackageManager.UI.Sample.FindByPackage("com.unity.xr.interaction.toolkit", "3.3.1")){
            if (sample.displayName == "XR Device Simulator"){
                sample.Import(UnityEditor.PackageManager.UI.Sample.ImportOptions.OverridePreviousImports);
                Debug.Log("Training_Builder_Core: XR Device Simulator imported. Enable 'Use XR Device Simulator in scenes' in Project Settings > XR Plug-in Management > XR Interaction Toolkit to use it.");
                return;
            }
        }

        Debug.LogError("Training_Builder_Core: XR Device Simulator sample not found for XRI 3.3.1.");
    }

    // ------------------------------------------------------------------
    // Module scene scaffolding (used by every module builder)
    // ------------------------------------------------------------------

    // Every module scene starts the same way: empty scene + light + floor.
    public static Scene NewModuleScene(){
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateDirectionalLight();
        CreateFloor();
        return scene;
    }

    // The shared per-scene lesson plumbing: prompt panel (framework prefab),
    // results panel, and a Lesson_Manager hosting a fully wired Lesson_Sequencer.
    // Callers add their registries/controllers to scaffold.Manager and SetRef
    // them onto scaffold.Sequencer.
    public static Module_Scaffold BuildLessonScaffold(Vector3 promptPos, Quaternion promptRot, Vector3 resultsPos){
        GameObject promptPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PromptPanelPrefabPath);
        if (promptPrefab == null){ Debug.LogError($"Training_Builder_Core: prompt panel prefab missing — run 'Training/1 Build Framework Assets' first."); return null; }

        GameObject prompt = (GameObject)PrefabUtility.InstantiatePrefab(promptPrefab);
        prompt.transform.position = promptPos;
        prompt.transform.rotation = promptRot;
        TextMeshProUGUI promptText = prompt.transform.Find("Background/Prompt_Text").GetComponent<TextMeshProUGUI>();
        Button continueButton = prompt.transform.Find("Background/Continue_Button").GetComponent<Button>();

        Canvas resultsCanvas = CreateWorldCanvas("Results_Panel", new Vector2(700f, 460f), 0.0012f, resultsPos);
        resultsCanvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        resultsCanvas.gameObject.AddComponent<Face_Camera>();
        AddBackground(resultsCanvas.transform, new Color(0.07f, 0.09f, 0.13f, 0.92f));
        TextMeshProUGUI resultsText = CreateTMP(resultsCanvas.transform, "Results_Text", "Results", 38f, new Vector2(0f, 70f), new Vector2(620f, 260f));
        Button retryButton = CreateButton(resultsCanvas.transform, "Retry_Button", "Retry Practice", new Vector2(-165f, -150f), new Vector2(290f, 85f));
        Button returnButton = CreateButton(resultsCanvas.transform, "Return_Button", "Return to Menu", new Vector2(165f, -150f), new Vector2(290f, 85f));

        GameObject manager = new GameObject("Lesson_Manager");
        Lesson_Sequencer sequencer = manager.AddComponent<Lesson_Sequencer>();
        SetRef(sequencer, "PromptPanel", prompt);
        SetRef(sequencer, "PromptText", promptText);
        SetRef(sequencer, "ContinueButton", continueButton);
        SetRef(sequencer, "ResultsPanel", resultsCanvas.gameObject);
        SetRef(sequencer, "ResultsText", resultsText);
        sequencer.RetryButton = retryButton;
        sequencer.ReturnButton = returnButton;

        return new Module_Scaffold{ Manager = manager, Sequencer = sequencer };
    }

    // Load-or-create a Lesson_Definition asset. Fill its fields, then call
    // EditorUtility.SetDirty(def) — RegisterModule's SaveAssets persists it.
    public static Lesson_Definition LoadOrCreateLesson(string path){
        Lesson_Definition def = AssetDatabase.LoadAssetAtPath<Lesson_Definition>(path);

        if (def == null){
            def = ScriptableObject.CreateInstance<Lesson_Definition>();
            AssetDatabase.CreateAsset(def, path);
        }

        return def;
    }

    // ------------------------------------------------------------------
    // Step factories
    // ------------------------------------------------------------------

    public static Lesson_Step Info(string id, string prompt){
        return new Lesson_Step{ Step_Id = id, Kind = Lesson_Step_Kind.Info, Prompt_Text = prompt };
    }

    public static Lesson_Step Select(string id, string display, string description, bool includeInQuiz = true){
        return new Lesson_Step{
            Step_Id = id,
            Kind = Lesson_Step_Kind.Select_Component,
            Prompt_Text = $"This is the {display.ToUpper()} — {description}.\n\nSelect the highlighted part to continue.",
            Practice_Prompt_Text = $"Select the: {display}",
            Target_Marker_Id = id,
            Include_In_Quiz = includeInQuiz
        };
    }

    public static Lesson_Step PanelAction(string id, string label, string teach,
            string practicePrompt = "Practice: perform the full sequence in the correct order."){
        return new Lesson_Step{
            Step_Id = id,
            Kind = Lesson_Step_Kind.Panel_Action,
            Prompt_Text = $"{teach}\n\n<b>Do:</b> {label}  (the highlighted control).",
            Practice_Prompt_Text = practicePrompt,
            Target_Marker_Id = id,
            Include_In_Quiz = true
        };
    }

    // ------------------------------------------------------------------
    // Interactable builders
    // ------------------------------------------------------------------

    // Instantiates the framework Component_Marker prefab sized to the given
    // world bounds, with its label text set. Parent it under the scene's
    // markers group (the transform Marker_Registry scans).
    public static void BuildComponentMarker(Transform group, string id, string display, Bounds bounds){
        GameObject markerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ComponentMarkerPrefabPath);

        GameObject markerGO = (GameObject)PrefabUtility.InstantiatePrefab(markerPrefab);
        markerGO.name = "Marker_" + id;
        markerGO.transform.SetParent(group, true);
        markerGO.transform.position = bounds.center;

        Component_Marker marker = markerGO.GetComponent<Component_Marker>();
        marker.Marker_Id = id;
        marker.Display_Name = display;
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
        labelText.text = display;
        PrefabUtility.RecordPrefabInstancePropertyModifications(labelText);
    }

    // Makes a real machine part clickable: a trigger BoxCollider + XRSimpleInteractable
    // sized to the part's bounds, an Action_Interactable that fires the action, and a
    // translucent bounding-box cube that marks it as clickable. Mirrors the
    // Component_Marker prefab but feeds Notify_Action instead of marker selection.
    public static void BuildPartAction(Transform parent, string actionId, Bounds bounds){
        Material glow = AssetDatabase.LoadAssetAtPath<Material>(GlowMaterialPath);

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

    // A world-space UI button that reports an ordered action (Startup_Action_Button)
    // plus its guided-mode highlight frame.
    public static Startup_Action_Button BuildActionButton(Transform parent, string actionId, string label, Vector2 pos, Vector2 size){
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

    // A centered, zero-size pass-through container under a panel canvas. Its children
    // use the same canvas-center-relative coordinates as direct canvas children, so a
    // tab's buttons can be toggled active/inactive without shifting their layout.
    public static GameObject BuildTabContent(Transform canvas, string name){
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(canvas, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        return go;
    }

    // Adds a Marker_State_Toggle that flips the cue color on the part renderer found
    // at nodePath when its marker is selected. Skips (with a warning) if either the
    // marker or the part renderer is missing.
    public static void WireStateToggle(GameObject manager, GameObject group, Transform modelRoot,
            Lesson_Sequencer sequencer, string markerId, string nodePath, string label, Color onColor){
        Component_Marker marker = FindMarker(group, markerId);
        Transform node = modelRoot.Find(nodePath);
        Renderer rend = node != null ? node.GetComponentInChildren<Renderer>() : null;

        if (marker == null || rend == null){
            Debug.LogWarning($"Training_Builder_Core: state toggle '{markerId}' skipped (marker {(marker != null)}, renderer {(rend != null)}).");
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

    // ------------------------------------------------------------------
    // Scene + UI factories
    // ------------------------------------------------------------------

    public static void CreateDirectionalLight(){
        GameObject lightGO = new GameObject("Directional Light");
        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    public static void CreateFloor(){
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.localScale = new Vector3(2f, 1f, 2f);
        Material floorMat = AssetDatabase.LoadAssetAtPath<Material>(FloorMatPath);

        if (floorMat != null)
            floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;
    }

    public static GameObject CreateIndicator(Transform parent, string text, Color color, Vector3 position){
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

    public static Canvas CreateWorldCanvas(string name, Vector2 sizePx, float scale, Vector3 position){
        GameObject go = new GameObject(name, typeof(RectTransform));
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        // Mouse clicks need a plain GraphicRaycaster (TrackedDevice* serves XR rays
        // only); with no event camera it falls back to Camera.main, like DigitalTwin.
        go.AddComponent<GraphicRaycaster>();
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = sizePx;
        rect.localScale = Vector3.one * scale;
        rect.position = position;
        return canvas;
    }

    public static Image AddBackground(Transform parent, Color color){
        GameObject bg = new GameObject("Background", typeof(RectTransform));
        bg.transform.SetParent(parent, false);
        bg.transform.SetAsFirstSibling();
        Stretch(bg.GetComponent<RectTransform>());
        Image image = bg.AddComponent<Image>();
        image.color = color;
        return image;
    }

    public static TextMeshProUGUI CreateTMP(Transform parent, string name, string text, float fontSize, Vector2 anchoredPos, Vector2 size){
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

    public static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size){
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

    public static void Stretch(RectTransform rect){
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static GameObject InstantiateProp(GameObject asset, Transform parent, string name, Vector3 pos, Quaternion rot){
        if (asset == null){ Debug.LogWarning($"Training_Builder_Core: prop asset for '{name}' missing."); return null; }
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
        go.transform.rotation = rot;
        return go;
    }

    public static Material LoadOrCreateMaterial(string path, Color color){
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat != null)
            return mat;

        mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // ------------------------------------------------------------------
    // Lookup + folder helpers
    // ------------------------------------------------------------------

    // Creates any missing folders along an "Assets/..." path.
    public static void EnsureFolderPath(string path){
        string[] parts = path.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++){
            string next = current + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);

            current = next;
        }
    }

    public static Bounds RendererBounds(Transform root){
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return new Bounds(root.position, Vector3.one * 0.1f);

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    public static Transform FindChild(Transform root, string name){
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name)
                return t;

        return null;
    }

    public static Component_Marker FindMarker(GameObject group, string id){
        foreach (Component_Marker m in group.GetComponentsInChildren<Component_Marker>(true))
            if (m.Marker_Id == id)
                return m;

        return null;
    }

    // ------------------------------------------------------------------
    // Serialized-property plumbing (private fields on runtime components)
    // ------------------------------------------------------------------

    public static void SetRef(Component component, string property, Object value){
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(property);

        if (prop == null){
            Debug.LogError($"Training_Builder_Core: property '{property}' not found on {component.GetType().Name}");
            return;
        }

        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetRefArray(Component component, string property, List<Object> values){
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(property);

        if (prop == null){
            Debug.LogError($"Training_Builder_Core: property '{property}' not found on {component.GetType().Name}");
            return;
        }

        prop.arraySize = values.Count;

        for (int i = 0; i < values.Count; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void AppendRefArray(Component component, string property, List<Object> values){
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(property);

        if (prop == null){
            Debug.LogError($"Training_Builder_Core: property '{property}' not found on {component.GetType().Name}");
            return;
        }

        foreach (Object value in values){
            prop.arraySize++;
            prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = value;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetVal(Component component, string property, object value){
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(property);

        if (prop == null){
            Debug.LogError($"Training_Builder_Core: property '{property}' not found on {component.GetType().Name}");
            return;
        }

        if (value is float f) prop.floatValue = f;
        else if (value is int i) prop.intValue = i;
        else if (value is bool b) prop.boolValue = b;
        else if (value is string s) prop.stringValue = s;

        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
