using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// One-shot swap of Module 2's mill: replaces the PM8000_Training instance with the
// IntellitekMill_Training wrapper Module 1 already uses, and rebinds the two
// Startup_State_Controller references that pointed into the old prefab (the kaig
// power-switch renderer and the Demo_Block). Also removes the unwired raw
// IntellitekMill prop that predated the swap. Refuses to run once PM8000_Training
// is gone, so a second invocation is a no-op with an error rather than a duplicate.
public static class M2_Mill_Swap{
    private const string SceneName = "Module2_Startup";
    private const string StationRootName = "Module2";
    private const string OldMillName = "PM8000_Training";
    private const string WrapperPath = "Assets/Members/Colin/Training/Prefabs/IntellitekMill_Training.prefab";
    private const string StrayMillGuid = "673c7cc71a415124094f4eeb7a99b08a";
    private const string PowerSwitchName = "kaig";
    private const string DemoBlockName = "Demo_Block";
    private const string ViceName = "Vice";

    // Where the PM8000 stood. The wrapper's internal alignment puts the Intellitek
    // geometry in the same place from the same root transform, which is what lets the
    // hand-placed Action_mill_power_on hitbox stay put.
    private static readonly Vector3 MillPosition = new Vector3(1.681f, 0f, 3.44f);
    private static readonly Vector3 MillEuler = new Vector3(0f, 270f, 0f);

    // Module 1's current hand-tuned rest poses (scene overrides there), mirrored so
    // both modules show the vice and block in the same spot. Not taken from the
    // wrapper defaults, which predate the 2026-08-03 classifier re-bake.
    private static readonly Vector3 VicePosition = new Vector3(-0.0433f, 1.1757f, 1.9961f);
    private static readonly Vector3 BlockPosition = new Vector3(-0.0953f, 1.228f, 1.924f);

    [MenuItem("Training/9 M2 - Swap Mill To Intellitek")]
    public static void Swap(){
        if (Application.isPlaying){
            Debug.LogError("M2_Mill_Swap: exit play mode — edits made now are discarded.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();

        if (scene.name != SceneName){
            Debug.LogError($"M2_Mill_Swap: open {SceneName} first (active scene is {scene.name}).");
            return;
        }

        GameObject station = GameObject.Find(StationRootName);

        if (station == null){
            Debug.LogError($"M2_Mill_Swap: no {StationRootName} in open scene.");
            return;
        }

        Transform oldMill = station.transform.Find(OldMillName);

        if (oldMill == null){
            Debug.LogError($"M2_Mill_Swap: no {OldMillName} under {StationRootName} — already swapped?");
            return;
        }

        Startup_State_Controller controller = Object.FindFirstObjectByType<Startup_State_Controller>();

        if (controller == null){
            Debug.LogError("M2_Mill_Swap: no Startup_State_Controller in open scene.");
            return;
        }

        GameObject wrapperAsset = AssetDatabase.LoadAssetAtPath<GameObject>(WrapperPath);

        if (wrapperAsset == null){
            Debug.LogError($"M2_Mill_Swap: missing {WrapperPath}.");
            return;
        }

        GameObject mill = (GameObject)PrefabUtility.InstantiatePrefab(wrapperAsset, station.transform);
        mill.transform.SetSiblingIndex(oldMill.GetSiblingIndex());
        mill.transform.localPosition = MillPosition;
        mill.transform.localRotation = Quaternion.Euler(MillEuler);

        // Resolve everything the rebind needs before touching the old mill, so a miss
        // leaves the scene exactly as it was.
        Transform powerSwitch = FindSingle(mill.transform, PowerSwitchName, out int powerSwitchCount);
        Renderer powerRenderer = powerSwitch != null ? powerSwitch.GetComponent<Renderer>() : null;
        Transform block = mill.transform.Find(DemoBlockName);
        Transform vice = mill.transform.Find(ViceName);

        if (powerRenderer == null || block == null || vice == null){
            Object.DestroyImmediate(mill);
            Debug.LogError($"M2_Mill_Swap: wrapper is missing a piece (kaig x{powerSwitchCount}, " +
                $"renderer {powerRenderer != null}, block {block != null}, vice {vice != null}) — scene untouched.");
            return;
        }

        vice.localPosition = VicePosition;
        block.localPosition = BlockPosition;
        // Matches the old instance's inactive override; Reset_Cold() re-hides it at
        // runtime, but the saved scene should agree.
        block.gameObject.SetActive(false);

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("MillPowerPart").objectReferenceValue = powerRenderer;
        so.FindProperty("DemoBlock").objectReferenceValue = block.gameObject;
        so.ApplyModifiedProperties();

        Object.DestroyImmediate(oldMill.gameObject);
        int strays = DeleteStrayMills(scene, mill);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"M2_Mill_Swap: {OldMillName} -> {wrapperAsset.name} at {MillPosition}, " +
            $"MillPowerPart -> {PowerSwitchName}, DemoBlock -> {DemoBlockName}, " +
            $"stray raw mills deleted: {strays}. Scene saved.");
    }

    // The pre-swap scene carried a bare IntellitekMill.prefab instance as an unwired
    // prop; match by source prefab so a coincidental rename can't delete the new mill.
    private static int DeleteStrayMills(Scene scene, GameObject keep){
        int deleted = 0;

        foreach (GameObject root in scene.GetRootGameObjects()){
            if (root == keep)
                continue;

            string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);

            if (AssetDatabase.AssetPathToGUID(path) != StrayMillGuid)
                continue;

            Debug.Log($"M2_Mill_Swap: deleting stray {root.name} at {root.transform.position}.");
            Object.DestroyImmediate(root);
            deleted++;
        }

        return deleted;
    }

    private static Transform FindSingle(Transform root, string name, out int count){
        Transform found = null;
        count = 0;

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true)){
            if (t.name != name)
                continue;

            count++;
            found = t;
        }

        return count == 1 ? found : null;
    }
}
