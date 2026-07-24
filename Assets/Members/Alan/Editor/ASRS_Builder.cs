using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.UI;

// Builds a standalone entry point for ASRS training, independent of Colin's
// Bootstrap.unity/Training_Builder pipeline (which stays untouched — it's his
// shared scene for the mill modules). Mirrors the same rig/manager/input
// pattern his BuildBootstrapScene uses, trimmed to a single always-on module.
public static class ASRS_Builder{
    private const string ASRSBootstrapScenePath = "Assets/Scenes/ASRS_Bootstrap.unity";
    private const string ASRSModule1ScenePath = "Assets/Scenes/ASRSModule1.unity";
    private const string ASRSLessonPath = "Assets/Members/Alan/ASRSM1_Lesson.asset";
    private const string RigPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.3.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
    private const string InputActionsPath = "Assets/Samples/XR Interaction Toolkit/3.3.1/Starter Assets/XRI Default Input Actions.inputactions";
    private const string FloorMatPath = "Assets/Materials/Light_Grey.mat";

    [MenuItem("ASRS Training/0 Build Everything")]
    public static void BuildEverything(){
        CleanupASRSModule1Scene();
        BuildASRSBootstrapScene();
        AddScenesToBuildSettings();
        Debug.Log("ASRS_Builder: full build complete.");
    }

    // ------------------------------------------------------------------
    // 1. ASRSModule1 scene hygiene
    // ------------------------------------------------------------------

    // ASRSModule1.unity was hand-built and forked from a scene that already had
    // a standalone XR Interaction Manager sitting in it (no rig attached) and a
    // duplicate Marker_Registry pointed at the same MarkersRoot/Sequencer as the
    // real one. The stray manager competes with ASRS_Bootstrap's once this loads
    // additively on top of it — interactors and Component_Markers each auto-bind
    // to whichever manager exists first, so they can silently end up on
    // different managers and never see each other's select events. The
    // duplicate registry double-wires every marker's Selected event to
    // Notify_Marker_Selected, so one correct click fires it twice and silently
    // skips a step. This removes both.
    [MenuItem("ASRS Training/1 Cleanup ASRSModule1 Scene")]
    public static void CleanupASRSModule1Scene(){
        var moduleScene = EditorSceneManager.OpenScene(ASRSModule1ScenePath, OpenSceneMode.Single);
        GameObject orphanManager = null;

        foreach (GameObject root in moduleScene.GetRootGameObjects())
            if (root.name == "XR Interaction Manager" && root.GetComponent<XRInteractionManager>() != null){ orphanManager = root; break; }

        if (orphanManager != null){
            Object.DestroyImmediate(orphanManager);
            Debug.Log("ASRS_Builder: removed orphaned XR Interaction Manager from ASRSModule1.");
        }
        else{
            Debug.Log("ASRS_Builder: no orphaned XR Interaction Manager found in ASRSModule1 (already clean).");
        }

        Marker_Registry[] registries = Object.FindObjectsByType<Marker_Registry>(FindObjectsSortMode.None);

        if (registries.Length > 1){
            Marker_Registry keep = null;
            foreach (Marker_Registry r in registries)
                if (r.GetComponent<Lesson_Sequencer>() != null){ keep = r; break; }
            if (keep == null) keep = registries[0];

            foreach (Marker_Registry r in registries){
                if (r == keep) continue;

                foreach (Part_Highlighter ph in Object.FindObjectsByType<Part_Highlighter>(FindObjectsSortMode.None)){
                    SerializedObject phSO = new SerializedObject(ph);
                    SerializedProperty regProp = phSO.FindProperty("Registry");
                    if (regProp.objectReferenceValue == (Object)r){
                        regProp.objectReferenceValue = keep;
                        phSO.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                Object.DestroyImmediate(r.gameObject);
                Debug.Log("ASRS_Builder: removed duplicate Marker_Registry from ASRSModule1.");
            }
        }
        else{
            Debug.Log("ASRS_Builder: no duplicate Marker_Registry found in ASRSModule1 (already clean).");
        }

        EditorSceneManager.MarkSceneDirty(moduleScene);
        EditorSceneManager.SaveScene(moduleScene);
    }

    // ------------------------------------------------------------------
    // 2. ASRS Bootstrap scene
    // ------------------------------------------------------------------

    [MenuItem("ASRS Training/2 Build ASRS Bootstrap Scene")]
    public static void BuildASRSBootstrapScene(){
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateDirectionalLight();
        CreateFloor();

        GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefabPath);
        if (rigPrefab == null){ Debug.LogError($"ASRS_Builder: rig prefab missing at {RigPrefabPath}"); return; }
        GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);

        // Component_Marker uses trigger colliders; XRI casters ignore triggers by
        // default, which makes markers unselectable. Same fix Colin applies in
        // BuildBootstrapScene.
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

        GameObject managers = new GameObject("Managers");
        ASRS_Bootstrap bootstrap = managers.AddComponent<ASRS_Bootstrap>();
        SetVal(bootstrap, "ModuleSceneName", "ASRSModule1");
        Lesson_Definition asrsLesson = AssetDatabase.LoadAssetAtPath<Lesson_Definition>(ASRSLessonPath);
        if (asrsLesson == null) Debug.LogWarning($"ASRS_Builder: {ASRSLessonPath} missing — Definition left unassigned.");
        SetRef(bootstrap, "Definition", asrsLesson);

        EditorSceneManager.SaveScene(scene, ASRSBootstrapScenePath);
        Debug.Log("ASRS_Builder: ASRS Bootstrap scene built.");
    }

    // ------------------------------------------------------------------
    // 3. Build settings
    // ------------------------------------------------------------------

    // Appends without touching any entries Colin's own AddScenesToBuildSettings
    // manages (Bootstrap/Module1_Overview/Module2_Startup) or their order.
    [MenuItem("ASRS Training/3 Add Scenes To Build Settings")]
    public static void AddScenesToBuildSettings(){
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        if (scenes.FindIndex(s => s.path == ASRSBootstrapScenePath) < 0)
            scenes.Add(new EditorBuildSettingsScene(ASRSBootstrapScenePath, true));

        if (scenes.FindIndex(s => s.path == ASRSModule1ScenePath) < 0)
            scenes.Add(new EditorBuildSettingsScene(ASRSModule1ScenePath, true));

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("ASRS_Builder: build settings updated.");
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

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

    private static void SetRef(Component component, string property, Object value){
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(property);

        if (prop == null){
            Debug.LogError($"ASRS_Builder: property '{property}' not found on {component.GetType().Name}");
            return;
        }

        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetVal(Component component, string property, object value){
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(property);

        if (prop == null){
            Debug.LogError($"ASRS_Builder: property '{property}' not found on {component.GetType().Name}");
            return;
        }

        if (value is float f) prop.floatValue = f;
        else if (value is int i) prop.intValue = i;
        else if (value is bool b) prop.boolValue = b;
        else if (value is string s) prop.stringValue = s;

        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
