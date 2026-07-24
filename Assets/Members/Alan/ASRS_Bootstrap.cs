using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Standalone entry point for ASRS training. Plays the same role as Colin's
// Bootstrap.unity (owns the rig + the one XR Interaction Manager, loads the
// content scene additively) but trimmed to a single always-on module — no
// menu, no save/progress tracking. Lives on a "Managers" object in
// ASRS_Bootstrap.unity; ASRSModule1.unity itself stays rig-less content, same
// convention Colin's Module1_Overview/Module2_Startup follow.
public class ASRS_Bootstrap : MonoBehaviour{
    [SerializeField] private string ModuleSceneName = "ASRSModule1";
    [SerializeField] private Lesson_Definition Definition;

    private IEnumerator Start(){
        yield return SceneManager.LoadSceneAsync(ModuleSceneName, LoadSceneMode.Additive);

        Scene scene = SceneManager.GetSceneByName(ModuleSceneName);
        SceneManager.SetActiveScene(scene);

        Lesson_Sequencer sequencer = null;

        foreach (GameObject root in scene.GetRootGameObjects()){
            sequencer = root.GetComponentInChildren<Lesson_Sequencer>(true);

            if (sequencer != null)
                break;
        }

        if (sequencer == null){
            Debug.LogError($"ASRS_Bootstrap: no Lesson_Sequencer found in scene '{ModuleSceneName}'.");
            yield break;
        }

        sequencer.Begin(Definition, Lesson_Mode.Guided);
    }
}
