using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Module scenes (Module1_Overview etc.) hold only content — the XR rig, camera,
// and Lesson_Controller live in Bootstrap, which loads modules additively.
// Pressing Play in a module scene therefore yields a dead scene. This redirect
// makes Play start Bootstrap instead and auto-enters the module that was open,
// through the normal Lesson_Controller.Start_Module path.
// Note: playModeStartScene is ignored if "Enter Play Mode Options -> Reload
// Scene" is ever disabled — the redirect silently stops working in that case.
[InitializeOnLoad]
public static class Training_Play_Redirect{
    private const string BootstrapScenePath = "Assets/Members/Colin/Scenes/Bootstrap.unity";
    private const string AutoStartKey = "Training.AutoStartScene";

    private static readonly string[] ModuleScenePaths = {
        "Assets/Members/Colin/Scenes/Module1_Overview.unity",
        "Assets/Members/Colin/Scenes/Module2_Startup.unity",
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

    private static void Refresh(){
        Scene activeScene = SceneManager.GetActiveScene();

        if (System.Array.IndexOf(ModuleScenePaths, activeScene.path) >= 0){
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);
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
        if (autoStartHooked || autoStartDone || SessionState.GetString(AutoStartKey, null) == null)
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

        SerializedProperty modules = new SerializedObject(controller).FindProperty("AvailableModules");

        for (int i = 0; i < modules.arraySize; i++){
            Lesson_Definition definition = modules.GetArrayElementAtIndex(i).objectReferenceValue as Lesson_Definition;

            if (definition != null && definition.Scene_Name == sceneName){
                controller.Start_Module(i);
                return;
            }
        }

        Debug.LogError($"Training_Play_Redirect: no Lesson_Definition with Scene_Name '{sceneName}' on Lesson_Controller");
    }
}
