using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// ASRSModule1.unity holds only content, no rig (same convention Colin's
// Module1_Overview/Module2_Startup follow) — pressing Play directly on it
// would yield a dead scene. This redirects Play to ASRS_Bootstrap.unity
// instead, which spawns the rig, loads ASRSModule1 additively, and begins the
// lesson itself (no menu step to drive, unlike Colin's Training_Play_Redirect).
[InitializeOnLoad]
public static class ASRS_Play_Redirect{
    private const string ASRSBootstrapScenePath = "Assets/Scenes/ASRS_Bootstrap.unity";
    private const string ASRSModule1ScenePath = "Assets/Scenes/ASRSModule1.unity";

    static ASRS_Play_Redirect(){
        EditorSceneManager.activeSceneChangedInEditMode += (_, _) => Refresh();
        EditorSceneManager.sceneOpened += (_, _) => Refresh();
        Refresh();
    }

    private static void Refresh(){
        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.path == ASRSModule1ScenePath){
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ASRSBootstrapScenePath);
        }
        else if (EditorSceneManager.playModeStartScene != null
            && AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) == ASRSBootstrapScenePath){
            EditorSceneManager.playModeStartScene = null;
        }
    }
}
