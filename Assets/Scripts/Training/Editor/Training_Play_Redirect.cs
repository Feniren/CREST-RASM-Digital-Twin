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
// not). Each station has its own Bootstrap (listed below); the redirect starts
// whichever one lists the open module's lesson in AvailableModules.
// Note: playModeStartScene is ignored if "Enter Play Mode Options -> Reload
// Scene" is ever disabled — the redirect silently stops working in that case.
[InitializeOnLoad]
public static class Training_Play_Redirect{
    private const string AutoStartKey = "Training.AutoStartScene";

    // Shared with Training_Validator's Bootstrap-reference check. The first
    // entry is the fallback when no Bootstrap references the module's lesson.
    internal static readonly string[] BootstrapScenePaths = {
        "Assets/Members/Colin/Scenes/Bootstrap.unity",
        "Assets/Members/Ethan/LaserEngraver/LaserBootstrap.unity",
    };

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
    private static Lesson_Sequencer FindSequencer(Scene scene){
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        foreach (GameObject root in scene.GetRootGameObjects()){
            Lesson_Sequencer sequencer = root.GetComponentInChildren<Lesson_Sequencer>(true);

            if (sequencer != null)
                return sequencer;
        }

        return null;
    }

    // GUID scan of the committed Bootstrap scenes, same as Training_Validator.
    internal static string FindBootstrapFor(Lesson_Definition lesson){
        string guid = lesson != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(lesson)) : null;

        if (!string.IsNullOrEmpty(guid))
            foreach (string path in BootstrapScenePaths)
                if (System.IO.File.Exists(path) && System.IO.File.ReadAllText(path).Contains(guid))
                    return path;

        return null;
    }

    private static void Refresh(){
        Scene activeScene = SceneManager.GetActiveScene();

        Lesson_Sequencer sequencer = FindSequencer(activeScene);

        if (sequencer != null){
            string bootstrapPath = FindBootstrapFor(sequencer.Lesson) ?? BootstrapScenePaths[0];
            SceneAsset bootstrap = AssetDatabase.LoadAssetAtPath<SceneAsset>(bootstrapPath);

            if (bootstrap == null){
                Debug.LogError($"Training_Play_Redirect: no scene at '{bootstrapPath}' — update BootstrapScenePaths.");
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
