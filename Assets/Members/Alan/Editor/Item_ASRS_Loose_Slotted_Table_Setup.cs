using UnityEditor;
using UnityEngine;

// Spawns a loose, carryable Item_Slotted_Table near the rack in the
// currently open scene, tagged with a vacant row-5 slot's own TableID and
// flagged Pickup so the existing grab/carry system (Entity_XR_Hand) will
// actually let the player hold it. Without an object like this there is
// nothing in the scene the player COULD carry back into the rack — the
// row-5 slots are the rack's own hidden "ghost" tables (Item_ASRS hides
// them via SetTableVisibility but keeps them exactly at their real anchor,
// as the drop TARGET Item_ASRS_Manual_Slot_Insert listens on — not
// something meant to be grabbed or moved itself).
public static class Item_ASRS_Loose_Slotted_Table_Setup
{
    private const string PrefabPath = "Assets/Game_Objects/SlottedTableSingleBuffer.prefab";
    private const string DefaultTableId = "050001";

    [MenuItem("ASRS/Add Loose Slotted Table Near Rack")]
    public static void AddLooseTable()
    {
        Item_ASRS rack = Object.FindFirstObjectByType<Item_ASRS>();
        if (rack == null)
        {
            Debug.LogError("Item_ASRS_Loose_Slotted_Table_Setup: no Item_ASRS found in the open scene.");
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"Item_ASRS_Loose_Slotted_Table_Setup: couldn't find prefab at '{PrefabPath}'.");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(instance, "Add Loose Slotted Table");
        instance.name = $"SlottedTable_{DefaultTableId} (Loose - Place In Rack)";

        // In front of the rack, not nested under it — Item_ASRS.Start() scans
        // its OWN children into TableMap, so this has to stay a sibling or
        // it'd collide with the real row-5 seed table that already carries
        // the same TableID.
        instance.transform.SetPositionAndRotation(
            rack.transform.position + rack.transform.forward * 1.5f + Vector3.up * 0.1f,
            rack.transform.rotation);

        if (!instance.TryGetComponent(out Item_Slotted_Table table))
        {
            Debug.LogError("Item_ASRS_Loose_Slotted_Table_Setup: prefab has no Item_Slotted_Table component.", instance);
            return;
        }

        SerializedObject so = new SerializedObject(table);
        so.FindProperty("TableID").stringValue = DefaultTableId;
        so.FindProperty("Pickup").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log($"Item_ASRS_Loose_Slotted_Table_Setup: added a loose, carryable slotted table (TableID {DefaultTableId}) in front of the rack. " +
                  "Reposition it in-editor if it doesn't land somewhere sensible, check its Collider isn't blocking grab raycasts if it won't pick up, then save the scene.");
    }
}
