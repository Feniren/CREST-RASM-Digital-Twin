using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Play-mode debug drivers for no-headset flow testing. Module-agnostic — they
// operate on whatever Lesson_Sequencer / registries are live. Module-specific
// click helpers (doors, power switches, ...) live in each member's debug menu.
public static class Training_Debug{
    [MenuItem("Training/8 Debug - Start Module 1")]
    public static void DebugStartModule1(){ StartModule(0); }

    [MenuItem("Training/8 Debug - Start Module 2")]
    public static void DebugStartModule2(){ StartModule(1); }

    private static void StartModule(int index){
        if (!Application.isPlaying){ Debug.LogError("Training_Debug: enter play mode first (from Bootstrap)."); return; }
        Lesson_Controller controller = Object.FindFirstObjectByType<Lesson_Controller>();
        if (controller == null){ Debug.LogError("Training_Debug: no Lesson_Controller found."); return; }
        controller.Start_Module(index);
    }

    // Drives the currently-loaded lesson to completion synchronously (guided then
    // practice — the guided->practice transition is synchronous). Verifies the
    // whole flow reaches its end state without 30 manual steps.
    [MenuItem("Training/8 Debug - Auto Run To Completion")]
    public static void DebugAutoRunToCompletion(){
        if (!Application.isPlaying){ Debug.LogError("Training_Debug: enter play mode first."); return; }
        Lesson_Sequencer seq = Object.FindFirstObjectByType<Lesson_Sequencer>();
        if (seq == null){ Debug.LogError("Training_Debug: no Lesson_Sequencer (load a module first)."); return; }

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

        Debug.Log($"Training_Debug: auto-run finished after {guard} steps; completed={seq.Current_Step == null}; persistentDataPath={Application.persistentDataPath}");
    }

    [MenuItem("Training/8 Debug - Auto Step (Correct)")]
    public static void DebugAutoStep(){
        DebugStep(true);
    }

    [MenuItem("Training/8 Debug - Auto Step (Wrong Answer)")]
    public static void DebugAutoStepWrong(){
        DebugStep(false);
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

    [MenuItem("Training/8 Debug - Click Wrist Reset")]
    public static void DebugClickWristReset(){
        if (!Application.isPlaying) return;
        Wrist_HUD hud = Object.FindFirstObjectByType<Wrist_HUD>(FindObjectsInactive.Include);

        if (hud != null && hud.gameObject.activeInHierarchy){
            ((Button)new SerializedObject(hud).FindProperty("ResetButton").objectReferenceValue).onClick.Invoke();
            Debug.Log("Training_Debug: wrist reset button invoked.");
            return;
        }

        // Without a tracked controller the XRI modality manager deactivates the
        // controller GameObjects (and the wrist HUD with them) — call the action directly.
        Object.FindFirstObjectByType<Lesson_Controller>().Restart_Phase();
        Debug.Log("Training_Debug: wrist HUD inactive (no XR device) — Restart_Phase called directly.");
    }

    [MenuItem("Training/8 Debug - Toggle Time Scale 5x")]
    public static void DebugTimeScale(){
        Time.timeScale = Mathf.Approximately(Time.timeScale, 1f) ? 5f : 1f;
        Debug.Log($"Training_Debug: timeScale = {Time.timeScale}");
    }

    private static void DebugStep(bool correct){
        if (!Application.isPlaying) return;
        Lesson_Sequencer sequencer = Object.FindFirstObjectByType<Lesson_Sequencer>();

        if (sequencer == null || sequencer.Current_Step == null){
            Debug.Log("Training_Debug: no active lesson step.");
            return;
        }

        Lesson_Step step = sequencer.Current_Step;
        Debug.Log($"Training_Debug: auto-step '{step.Step_Id}' ({step.Kind}) {sequencer.Step_Index + 1}/{sequencer.Step_Count} mode {sequencer.Mode}");

        switch (step.Kind){
            case Lesson_Step_Kind.Info:
                ((Button)new SerializedObject(sequencer).FindProperty("ContinueButton").objectReferenceValue).onClick.Invoke();
                break;

            case Lesson_Step_Kind.Select_Component:{
                Marker_Registry registry = Object.FindFirstObjectByType<Marker_Registry>();
                Component_Marker target = registry.Resolve(step.Target_Marker_Id);
                Debug.Log($"Training_Debug: registry holds {registry.All.Count} markers; resolve '{step.Target_Marker_Id}' -> {(target != null ? target.name : "NULL")}");

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
                Debug.Log("Training_Debug: demo step — waiting for Demo_Finished.");
                break;
        }
    }
}
