using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Module scenes (Module1_Overview etc.) hold only content — the XR rig, camera,
// and Lesson_Controller live in Bootstrap, which loads modules additively.
// Pressing Play in a module scene therefore yields a dead scene. This redirect
// makes Play start Bootstrap instead and auto-enters the module that was open,
// through the normal Lesson_Controller.Start_Module path.
// A module scene is recognized by containing a Lesson_Sequencer (Bootstrap does
// not); Bootstrap's path is the single const below.
// Note: playModeStartScene is ignored if "Enter Play Mode Options -> Reload
// Scene" is ever disabled — the redirect silently stops working in that case.
[InitializeOnLoad]
public static class Training_Play_Redirect{
    private const string AutoStartKey = "Training.AutoStartScene";

    // Shared with Training_Validator's Bootstrap-reference check.
    internal const string BootstrapScenePath = "Assets/Members/Colin/Scenes/Bootstrap.unity";

    private static bool autoStartHooked;
    private static bool autoStartDone;

    static Training_Play_Redirect(){
        EditorSceneManager.activeSceneChangedInEditMode += (_, _) => Refresh();
        EditorSceneManager.sceneOpened += (_, _) => Refresh();
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        if (EditorApplication.isPlayingOrWillChangePlaymode)
            HookAutoStart();
        else
            Refresh();
    }

    // Mirrors Lesson_Controller.OnModuleLoaded: a module scene is one with a
    // Lesson_Sequencer in it.
    private static bool IsModuleScene(Scene scene){
        if (!scene.IsValid() || !scene.isLoaded)
            return false;

        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<Lesson_Sequencer>(true) != null)
                return true;

        return false;
    }

    private static void Refresh(){
        Scene activeScene = SceneManager.GetActiveScene();

        if (IsModuleScene(activeScene)){
            SceneAsset bootstrap = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);

            if (bootstrap == null){
                Debug.LogError($"Training_Play_Redirect: no scene at '{BootstrapScenePath}' — update BootstrapScenePath.");
                EditorSceneManager.playModeStartScene = null;
                SessionState.EraseString(AutoStartKey);
                return;
            }

            EditorSceneManager.playModeStartScene = bootstrap;
            SessionState.SetString(AutoStartKey, activeScene.name);
        }
        else{
            EditorSceneManager.playModeStartScene = null;
            SessionState.EraseString(AutoStartKey);
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state){
        switch (state){
            case PlayModeStateChange.ExitingEditMode:
                autoStartDone = false;
                // The redirect plays the saved asset, not the open scene —
                // offer to save so module edits aren't silently missing.
                if (SessionState.GetString(AutoStartKey, null) != null && SceneManager.GetActiveScene().isDirty)
                    EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                break;
            case PlayModeStateChange.EnteredPlayMode:
                HookAutoStart();
                break;
            case PlayModeStateChange.EnteredEditMode:
                autoStartDone = false;
                Refresh();
                break;
        }
    }

    private static void HookAutoStart(){
        // IsNullOrEmpty, not null: a stale/aborted session can leave "" behind,
        // which would otherwise trigger a doomed auto-start lookup.
        if (autoStartHooked || autoStartDone || string.IsNullOrEmpty(SessionState.GetString(AutoStartKey, null)))
            return;

        autoStartHooked = true;
        EditorApplication.update += AutoStartWhenReady;
    }

    // Waits for the first frame so every Awake/Start has run (Data_Loader loads
    // the save file in Start and would otherwise overwrite lesson progress).
    private static void AutoStartWhenReady(){
        if (!EditorApplication.isPlaying || Time.frameCount < 1)
            return;

        EditorApplication.update -= AutoStartWhenReady;
        autoStartHooked = false;
        autoStartDone = true;
        string sceneName = SessionState.GetString(AutoStartKey, null);

        Lesson_Controller controller = Object.FindFirstObjectByType<Lesson_Controller>();

        if (controller == null){
            Debug.LogError($"Training_Play_Redirect: no Lesson_Controller found in start scene; cannot auto-start '{sceneName}'");
            return;
        }

        if (!controller.Start_Module_By_Scene(sceneName))
            Debug.LogError($"Training_Play_Redirect: no Lesson_Definition with Scene_Name '{sceneName}' on Lesson_Controller.");
    }
}
