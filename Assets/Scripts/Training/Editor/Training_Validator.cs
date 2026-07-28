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

        Lesson_Definition lesson = FindLessonForScene(scene.path, errors);
        Lesson_Sequencer sequencer = Object.FindFirstObjectByType<Lesson_Sequencer>(FindObjectsInactive.Include);

        if (sequencer == null)
            errors.Add($"no Lesson_Sequencer in '{scene.name}' — the scene cannot run a lesson.");

        if (sequencer != null)
            CheckSequencerRefs(sequencer, errors);

        if (lesson != null && sequencer != null)
            CheckStepTargets(lesson, errors, warnings);

        foreach (string error in errors)
            Debug.LogError("Training_Validator: " + error);

        foreach (string warning in warnings)
            Debug.LogWarning("Training_Validator: " + warning);

        if (errors.Count == 0 && warnings.Count == 0)
            Debug.Log($"Training_Validator: '{scene.name}' OK — every step resolves to a marker or action in the scene.");
        else
            Debug.Log($"Training_Validator: '{scene.name}' — {errors.Count} error(s), {warnings.Count} warning(s).");
    }

    private static Lesson_Definition FindLessonForScene(string scenePath, List<string> errors){
        string[] guids = AssetDatabase.FindAssets("t:Training_Module_Registry");

        if (guids.Length == 0){
            errors.Add("no Training_Module_Registry asset found.");
            return null;
        }

        Training_Module_Registry registry = AssetDatabase.LoadAssetAtPath<Training_Module_Registry>(AssetDatabase.GUIDToAssetPath(guids[0]));

        foreach (Training_Module_Registry.Entry entry in registry.Modules){
            if (entry.Scene_Path != scenePath)
                continue;

            if (entry.Lesson == null)
                errors.Add($"registry entry for '{scenePath}' has no Lesson_Definition.");

            return entry.Lesson;
        }

        errors.Add($"'{scenePath}' is not in the module registry — it will never appear in the Bootstrap menu.");
        return null;
    }

    // Every one of these is dereferenced without a null check somewhere in
    // Lesson_Sequencer.Awake or Begin, so an empty slot is a play-mode exception.
    private static void CheckSequencerRefs(Lesson_Sequencer sequencer, List<string> errors){
        string[] required = { "PromptPanel", "PromptText", "ContinueButton", "ResultsPanel", "ResultsText", "Registry" };
        SerializedObject so = new SerializedObject(sequencer);

        foreach (string field in required){
            SerializedProperty prop = so.FindProperty(field);

            if (prop == null)
                errors.Add($"Lesson_Sequencer has no field '{field}' — this validator is out of date with the runtime.");
            else if (prop.objectReferenceValue == null)
                errors.Add($"Lesson_Sequencer.{field} is empty.");
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
