using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

// Adds everything Entity_Player actually needs to run into whichever scene
// is currently open — not just the player prefab itself. Entity_Player.
// Start() hard-requires a Data_Loader (for ItemLibraryReference) and a
// Spawn_Point (it does FindFirstObjectByType<Spawn_Point>().gameObject with
// no null-check) — normally these are only present because a scene like
// ASRSModule1/2 is loaded on top of a persistent Bootstrap scene that
// already has them. A scene opened and played on its own (e.g.
// RackmovementTest, for isolated rack-system testing) has neither, so
// dropping in just the player prefab would still crash on Play.
public static class RackMovement_Player_Setup
{
    private const string ArticulatedBodyPath = "Assets/Game_Objects/ArticulatedBody.prefab";
    private const string EventSystemPath = "Assets/Game_Objects/EventSystem.prefab";

    [MenuItem("ASRS/Add Player Rig To Current Scene")]
    public static void AddPlayerRig()
    {
        bool addedAnything = false;

        // --- Player body (ArticulatedBody) ---
        if (Object.FindFirstObjectByType<Entity_Player>() != null)
        {
            Debug.LogWarning("RackMovement_Player_Setup: an Entity_Player already exists in this scene — skipping the player prefab.");
        }
        else
        {
            GameObject articulatedBodyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArticulatedBodyPath);
            if (articulatedBodyPrefab == null)
            {
                Debug.LogError($"RackMovement_Player_Setup: couldn't find prefab at '{ArticulatedBodyPath}'.");
            }
            else
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(articulatedBodyPrefab);
                Undo.RegisterCreatedObjectUndo(instance, "Add Player Rig");
                addedAnything = true;
                Debug.Log("RackMovement_Player_Setup: added ArticulatedBody (Entity_Player + hand anchors + XR/desktop input).");
            }
        }

        // --- EventSystem (VR + desktop UI input) ---
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            Debug.LogWarning("RackMovement_Player_Setup: an EventSystem already exists in this scene — skipping.");
        }
        else
        {
            GameObject eventSystemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EventSystemPath);
            if (eventSystemPrefab == null)
            {
                Debug.LogError($"RackMovement_Player_Setup: couldn't find prefab at '{EventSystemPath}'.");
            }
            else
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(eventSystemPrefab);
                Undo.RegisterCreatedObjectUndo(instance, "Add Player Rig");
                addedAnything = true;
                Debug.Log("RackMovement_Player_Setup: added EventSystem (XRUIInputModule + InputSystemUIInputModule).");
            }
        }

        // --- Data_Loader (Entity_Player.Start() reads ItemLibraryReference
        // off whichever one it finds — without one, Start() null-refs
        // immediately) ---
        if (Object.FindFirstObjectByType<Data_Loader>() != null)
        {
            Debug.LogWarning("RackMovement_Player_Setup: a Data_Loader already exists in this scene — skipping.");
        }
        else
        {
            GameObject dataLoaderObj = new GameObject("Data_Loader");
            Undo.RegisterCreatedObjectUndo(dataLoaderObj, "Add Player Rig");
            Data_Loader dataLoader = dataLoaderObj.AddComponent<Data_Loader>();
            SetPrivateField(dataLoader, "FileName", "rackmovement_test_data.json");
            addedAnything = true;
            Debug.Log("RackMovement_Player_Setup: added a Data_Loader (separate save file — 'rackmovement_test_data.json' — so testing here never touches the real save data). Its Item Library field is left unassigned, matching every other scene in the project — none currently has one wired.");
        }

        // --- Spawn_Point (Entity_Player.Start() does
        // FindFirstObjectByType<Spawn_Point>().gameObject with no
        // null-check — this scene crashes on Play without one) ---
        if (Object.FindFirstObjectByType<Spawn_Point>() != null)
        {
            Debug.LogWarning("RackMovement_Player_Setup: a Spawn_Point already exists in this scene — skipping.");
        }
        else
        {
            GameObject spawnPointObj = new GameObject("Spawn_Point");
            Undo.RegisterCreatedObjectUndo(spawnPointObj, "Add Player Rig");
            spawnPointObj.AddComponent<Spawn_Point>();

            // Spawn near the rack if one's in the scene, so testing doesn't
            // start with a long walk — otherwise the origin, repositioned by
            // hand afterward like every other placement in this project.
            Item_ASRS rack = Object.FindFirstObjectByType<Item_ASRS>();
            if (rack != null)
                spawnPointObj.transform.position = rack.transform.position + rack.transform.forward * 2f + Vector3.up * 0.1f;

            addedAnything = true;
            Debug.Log("RackMovement_Player_Setup: added a Spawn_Point. Reposition it in-editor if it doesn't land somewhere sensible.");
        }

        if (addedAnything)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("RackMovement_Player_Setup: done. Save the scene, then press Play to test with a working player.");
        }
        else
        {
            Debug.Log("RackMovement_Player_Setup: everything the player needs was already present — nothing added.");
        }
    }

    private static void SetPrivateField(Object target, string fieldName, object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);

        if (prop == null)
        {
            Debug.LogError($"RackMovement_Player_Setup: property '{fieldName}' not found on {target.GetType().Name}");
            return;
        }

        prop.stringValue = (string)value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
