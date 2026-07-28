using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Checks a module scene against its Lesson_Definition.
//
// This replaces the correctness-by-construction the scene builders used to provide.
// While scenes were generated, a step id and a marker id could not disagree because
// both came from the same constant. Hand-authored scenes can disagree, and the
// failure is silent: Lesson_Sequencer matches ids by string at runtime, so a mismatch
// produces a step that can never be completed, with no error anywhere.
//
// It also catches drift the builders never could — they overwrote hand edits instead
// of reporting them.
//
// Open a module scene and run Training/7. Findings are logged one per line: the
// console truncates long multi-line messages.
public static class Training_Validator{
    [MenuItem("Training/7 Validate Open Module Scene")]
    public static void ValidateOpenScene(){
        Scene scene = SceneManager.GetActiveScene();
        List<string> errors = new List<string>();
        List<string> warnings = new List<string>();

        Lesson_Sequencer sequencer = Object.FindFirstObjectByType<Lesson_Sequencer>(FindObjectsInactive.Include);
        Lesson_Definition lesson = null;

        if (sequencer == null){
            errors.Add($"no Lesson_Sequencer in '{scene.name}' — the scene cannot run a lesson.");
        }
        else{
            lesson = FindLesson(sequencer, scene, errors);
            CheckSequencerRefs(sequencer, lesson, errors);
        }

        CheckBuildSettings(scene, errors);
        CheckDuplicateModuleIds(errors);

        if (lesson != null){
            CheckBootstrapReferences(lesson, warnings);
            CheckStepTargets(lesson, errors, warnings);
        }

        foreach (string error in errors)
            Debug.LogError("Training_Validator: " + error);

        foreach (string warning in warnings)
            Debug.LogWarning("Training_Validator: " + warning);

        if (errors.Count == 0 && warnings.Count == 0)
            Debug.Log($"Training_Validator: '{scene.name}' OK — every step resolves to a marker or action in the scene.");
        else
            Debug.Log($"Training_Validator: '{scene.name}' — {errors.Count} error(s), {warnings.Count} warning(s).");
    }

    // The scene's own Lesson reference replaces the deleted module registry as the
    // scene -> lesson mapping.
    private static Lesson_Definition FindLesson(Lesson_Sequencer sequencer, Scene scene, List<string> errors){
        Lesson_Definition lesson = sequencer.Lesson;

        if (lesson == null){
            errors.Add("Lesson_Sequencer.Lesson is empty — assign this module's Lesson_Definition asset.");
            return null;
        }

        if (lesson.Scene_Name != scene.name)
            errors.Add($"'{lesson.name}' has Scene_Name '{lesson.Scene_Name}' but the open scene is '{scene.name}' — Bootstrap loads by that name, so it would start a different scene.");

        return lesson;
    }

    // Module_Loader.Load_Module uses LoadSceneAsync by name — the scene must be an
    // enabled Build Settings entry or loading fails in a build. The list is
    // hand-maintained and shared with other members' scenes; never regenerate it.
    private static void CheckBuildSettings(Scene scene, List<string> errors){
        foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes){
            if (entry.path != scene.path)
                continue;

            if (!entry.enabled)
                errors.Add($"'{scene.path}' is in Build Settings but disabled — Module_Loader cannot load it in a build.");

            return;
        }

        errors.Add($"'{scene.path}' is not in Build Settings — Module_Loader cannot load it. Add it by hand (File > Build Settings); do not touch other members' entries.");
    }

    // Module_Id is the save-file key: two lessons sharing an id silently read and
    // write the same progress/score slot, so the menu shows the wrong per-module
    // status with no error anywhere.
    private static void CheckDuplicateModuleIds(List<string> errors){
        Dictionary<string, string> seen = new Dictionary<string, string>();

        foreach (string guid in AssetDatabase.FindAssets("t:Lesson_Definition")){
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Lesson_Definition def = AssetDatabase.LoadAssetAtPath<Lesson_Definition>(path);

            if (def == null || string.IsNullOrEmpty(def.Module_Id))
                continue;

            if (seen.TryGetValue(def.Module_Id, out string other))
                errors.Add($"Module_Id '{def.Module_Id}' is used by both '{other}' and '{path}' — duplicate save keys silently share one progress/score slot.");
            else
                seen.Add(def.Module_Id, path);
        }
    }

    // A lesson that validates clean but is not in Lesson_Controller.AvailableModules
    // never appears on the menu — the old registry check covered this; a GUID scan
    // of the committed Bootstrap scene covers it now.
    private static void CheckBootstrapReferences(Lesson_Definition lesson, List<string> warnings){
        string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(lesson));

        if (!System.IO.File.Exists(Training_Play_Redirect.BootstrapScenePath))
            return;

        if (!System.IO.File.ReadAllText(Training_Play_Redirect.BootstrapScenePath).Contains(guid))
            warnings.Add($"'{lesson.name}' is not referenced by Bootstrap.unity — add it to Lesson_Controller.AvailableModules or it will never appear in the menu.");
    }

    // Every unconditional field here is dereferenced without a null check somewhere
    // in Lesson_Sequencer.Awake or Begin, so an empty slot is a play-mode exception.
    // Registry is null-guarded at runtime and only needed by Select_Component steps
    // (M2 has none and legitimately leaves it empty).
    private static void CheckSequencerRefs(Lesson_Sequencer sequencer, Lesson_Definition lesson, List<string> errors){
        string[] required = { "PromptPanel", "PromptText", "ContinueButton", "ResultsPanel", "ResultsText" };
        SerializedObject so = new SerializedObject(sequencer);

        foreach (string field in required){
            SerializedProperty prop = so.FindProperty(field);

            if (prop == null)
                errors.Add($"Lesson_Sequencer has no field '{field}' — this validator is out of date with the runtime.");
            else if (prop.objectReferenceValue == null)
                errors.Add($"Lesson_Sequencer.{field} is empty.");
        }

        if (lesson != null && lesson.Steps.Exists(s => s.Kind == Lesson_Step_Kind.Select_Component)){
            SerializedProperty registryProp = so.FindProperty("Registry");

            if (registryProp == null)
                errors.Add("Lesson_Sequencer has no field 'Registry' — this validator is out of date with the runtime.");
            else if (registryProp.objectReferenceValue == null)
                errors.Add("Lesson_Sequencer.Registry is empty but the lesson has Select_Component steps.");
        }

        if (sequencer.RetryButton == null) errors.Add("Lesson_Sequencer.RetryButton is empty.");
        if (sequencer.ReturnButton == null) errors.Add("Lesson_Sequencer.ReturnButton is empty.");
    }

    private static void CheckStepTargets(Lesson_Definition lesson, List<string> errors, List<string> warnings){
        // Collected the way the runtime registries collect them — from under their
        // serialized root, including inactive objects (tab contents start disabled).
        // A marker sitting outside MarkersRoot is invisible to Marker_Registry.
        Dictionary<string, int> markers = CollectMarkerIds(errors);
        Dictionary<string, int> actions = CollectActionIds(errors);
        HashSet<string> used = new HashSet<string>();
        bool hasDemoController = Object.FindFirstObjectByType<Training_Demo_Controller>(FindObjectsInactive.Include) != null;

        foreach (Lesson_Step step in lesson.Steps){
            if (step.Kind == Lesson_Step_Kind.Select_Component){
                used.Add(step.Target_Marker_Id);

                if (!markers.ContainsKey(step.Target_Marker_Id))
                    errors.Add($"step '{step.Step_Id}' targets marker '{step.Target_Marker_Id}', which no Component_Marker under MarkersRoot provides — the step can never be completed.");
            }
            else if (step.Kind == Lesson_Step_Kind.Panel_Action){
                used.Add(step.Target_Marker_Id);

                if (!actions.ContainsKey(step.Target_Marker_Id))
                    errors.Add($"step '{step.Step_Id}' targets action '{step.Target_Marker_Id}', which no action button under ButtonsRoot provides — the step can never be completed.");
            }
            else if (step.Kind != Lesson_Step_Kind.Info && !hasDemoController){
                errors.Add($"step '{step.Step_Id}' is a {step.Kind} demo step but the scene has no Training_Demo_Controller.");
            }
        }

        foreach (KeyValuePair<string, int> marker in markers)
            if (marker.Value > 1)
                errors.Add($"marker id '{marker.Key}' appears {marker.Value} times — Marker_Registry keeps only the last, so the others are dead.");

        foreach (KeyValuePair<string, int> action in actions)
            if (action.Value > 1)
                errors.Add($"action id '{action.Key}' appears {action.Value} times — Action_Button_Registry keeps only the last, so the others are dead.");

        // Unused ids are legitimate for distractors (Module 2 ships three), so these
        // are warnings, not errors — but an unused id is also what a typo looks like.
        foreach (string id in markers.Keys)
            if (!used.Contains(id))
                warnings.Add($"marker '{id}' is in the scene but no step targets it.");

        foreach (string id in actions.Keys)
            if (!used.Contains(id) && !id.StartsWith("distractor_"))
                warnings.Add($"action '{id}' is in the scene but no step targets it (name it 'distractor_*' if that is deliberate).");
    }

    private static Dictionary<string, int> CollectMarkerIds(List<string> errors){
        Dictionary<string, int> ids = new Dictionary<string, int>();
        Marker_Registry registry = Object.FindFirstObjectByType<Marker_Registry>(FindObjectsInactive.Include);

        if (registry == null)
            return ids;

        Transform root = RootOf(registry, "MarkersRoot");

        if (root == null){
            errors.Add("Marker_Registry.MarkersRoot is empty — no markers will be registered.");
            return ids;
        }

        foreach (Component_Marker marker in root.GetComponentsInChildren<Component_Marker>(true))
            Count(ids, marker.Marker_Id);

        foreach (Component_Marker marker in Object.FindObjectsByType<Component_Marker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (!marker.transform.IsChildOf(root))
                errors.Add($"Component_Marker '{marker.Marker_Id}' ({marker.name}) is outside MarkersRoot — Marker_Registry will not find it.");

        return ids;
    }

    private static Dictionary<string, int> CollectActionIds(List<string> errors){
        Dictionary<string, int> ids = new Dictionary<string, int>();
        Action_Button_Registry registry = Object.FindFirstObjectByType<Action_Button_Registry>(FindObjectsInactive.Include);

        if (registry == null)
            return ids;

        Transform root = RootOf(registry, "ButtonsRoot");

        if (root == null){
            errors.Add("Action_Button_Registry.ButtonsRoot is empty — no actions will be registered.");
            return ids;
        }

        foreach (Startup_Action_Button button in root.GetComponentsInChildren<Startup_Action_Button>(true))
            Count(ids, button.Action_Id);

        foreach (Action_Interactable interactable in root.GetComponentsInChildren<Action_Interactable>(true))
            Count(ids, interactable.Action_Id);

        return ids;
    }

    private static Transform RootOf(Component registry, string field){
        SerializedProperty prop = new SerializedObject(registry).FindProperty(field);
        return prop != null ? prop.objectReferenceValue as Transform : null;
    }

    private static void Count(Dictionary<string, int> ids, string id){
        if (string.IsNullOrEmpty(id))
            return;

        ids.TryGetValue(id, out int seen);
        ids[id] = seen + 1;
    }
}
