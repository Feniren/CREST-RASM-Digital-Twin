using UnityEditor;
using UnityEngine;

// Wires Item_ASRS_Manual_Slot_Insert onto every row-5 slot table under the
// rack in the currently open scene. Row 5 is Item_ASRS's own hardcoded
// "vacant slot" demo row (Item_ASRS.cs: isVacantRow = row == 5) — those
// GameObjects stay in the hierarchy once the rack hides them at Start()
// (SetTableVisibility only disables the MeshRenderer, so the Collider and
// this new component stay live), which is exactly what makes them usable as
// drop targets without building any new trigger geometry.
public static class Item_ASRS_Manual_Slot_Insert_Setup
{
    private const int VacantRow = 5;

    [MenuItem("ASRS/Wire Manual Slot Insert (Row 5)")]
    public static void WireVacantRow()
    {
        Item_ASRS rack = Object.FindFirstObjectByType<Item_ASRS>();
        if (rack == null)
        {
            Debug.LogError("Item_ASRS_Manual_Slot_Insert_Setup: no Item_ASRS found in the open scene.");
            return;
        }

        // Optional — only present in a lesson scene like ASRSModule1. Left
        // null in a standalone testing scene (e.g. RackmovementTest), which
        // Item_ASRS_Manual_Slot_Insert already treats as "no lesson gating".
        SequenceManager sequenceManager = Object.FindFirstObjectByType<SequenceManager>();

        int wired = 0;

        foreach (Item_Slotted_Table table in rack.GetComponentsInChildren<Item_Slotted_Table>(true))
        {
            if (table == null || string.IsNullOrWhiteSpace(table.TableID))
                continue;

            if (!int.TryParse(table.TableID, out int rawId))
                continue;

            if (rawId / 10000 != VacantRow)
                continue;

            GameObject go = table.gameObject;

            Collider collider = go.GetComponent<Collider>();
            if (collider == null)
            {
                Debug.LogWarning($"Item_ASRS_Manual_Slot_Insert_Setup: '{go.name}' (TableID {table.TableID}) has no Collider — skipping.", go);
                continue;
            }

            if (!collider.isTrigger)
            {
                Undo.RecordObject(collider, "Wire Manual Slot Insert");
                collider.isTrigger = true;
            }

            Item_ASRS_Manual_Slot_Insert insert = go.GetComponent<Item_ASRS_Manual_Slot_Insert>();
            if (insert == null)
                insert = Undo.AddComponent<Item_ASRS_Manual_Slot_Insert>(go);

            SerializedObject so = new SerializedObject(insert);
            so.FindProperty("rack").objectReferenceValue = rack;
            if (sequenceManager != null)
                so.FindProperty("sequenceManager").objectReferenceValue = sequenceManager;
            so.ApplyModifiedPropertiesWithoutUndo();

            wired++;
            Debug.Log($"Item_ASRS_Manual_Slot_Insert_Setup: wired slot {table.TableID} ('{go.name}').", go);
        }

        if (wired == 0)
            Debug.LogWarning($"Item_ASRS_Manual_Slot_Insert_Setup: found no row-{VacantRow} slot tables under '{rack.name}'.");
        else
            Debug.Log($"Item_ASRS_Manual_Slot_Insert_Setup: done — wired {wired} row-{VacantRow} slot(s). Save the scene.");
    }
}
