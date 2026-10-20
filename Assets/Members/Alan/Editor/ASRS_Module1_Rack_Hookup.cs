using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Hooks a freshly placed ASRS Variant 1 into Module 1's lesson in the open
// scene, for when it replaces an older rack that's been kept but
// deactivated:
//   1. Picks the ACTIVE Item_ASRS as the lesson rack (the deactivated one is
//      left alone, never deleted).
//   2. Copies the old rack's lesson settings that a fresh prefab copy
//      doesn't have (EpoxyBlockPrefab, MaterialList, BlockList).
//   3. Wires the rack's own hidden row-5 slots with
//      Item_ASRS_Manual_Slot_Insert (step "LoadASRS").
//   4. Makes the lesson's loose slotted table ("SlottedTableSingleBuffer (U)")
//      carryable and gives it row-5 slot 050001's TableID, so it has an
//      empty home slot to go back into.
//   5. Points the "LoadSlottedTable" step at that table, so it completes
//      once the epoxy block is placed on it.
// Everything is logged; re-running is safe.
public static class ASRS_Module1_Rack_Hookup
{
    private const string LessonTableName = "SlottedTableSingleBuffer (U)";
    private const string LessonTableId = "050001";
    private const string LoadTableActionId = "LoadSlottedTable";
    private static readonly string[] LessonRackSettings = { "EpoxyBlockPrefab", "MaterialList", "BlockList" };

    [MenuItem("ASRS/Hook Up ASRS Variant 1 (Module 1)")]
    public static void HookUp()
    {
        SequenceManager sequenceManager = Object.FindFirstObjectByType<SequenceManager>();
        if (sequenceManager == null)
        {
            Debug.LogError("ASRS_Module1_Rack_Hookup: no SequenceManager found in the open scene.");
            return;
        }

        List<Item_ASRS> racks = Object.FindObjectsByType<Item_ASRS>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();
        List<Item_ASRS> active = racks.Where(r => r.gameObject.activeInHierarchy).ToList();
        Item_ASRS oldRack = racks.FirstOrDefault(r => !r.gameObject.activeInHierarchy);

        foreach (Item_ASRS r in racks)
            Debug.Log($"ASRS_Module1_Rack_Hookup: found Item_ASRS '{r.name}' ({(r.gameObject.activeInHierarchy ? "active" : "inactive")}).", r);

        if (active.Count == 0)
        {
            Debug.LogError("ASRS_Module1_Rack_Hookup: no ACTIVE Item_ASRS in the scene — activate the new ASRS Variant 1 first.");
            return;
        }

        Item_ASRS rack = active.FirstOrDefault(r => r.name.Contains("Variant")) ?? active[0];
        if (active.Count > 1)
            Debug.LogWarning($"ASRS_Module1_Rack_Hookup: {active.Count} active racks — using '{rack.name}'. Deactivate the others so only one runs the rack logic.", rack);

        if (oldRack != null)
            CopyLessonSettings(oldRack, rack);

        int slots = Item_ASRS_Manual_Slot_Insert_Setup.WireVacantRow(rack, sequenceManager);

        Item_Slotted_Table lessonTable = FindLessonTable(rack);
        if (lessonTable != null)
        {
            PrepareLessonTable(lessonTable);
            PointLoadStepAt(sequenceManager, lessonTable);
        }
        else
        {
            Debug.LogWarning($"ASRS_Module1_Rack_Hookup: no loose '{LessonTableName}' found — steps '{LoadTableActionId}' and 'LoadASRS' need a carryable table with TableID {LessonTableId}.");
        }

        EditorSceneManager.MarkSceneDirty(sequenceManager.gameObject.scene);
        Selection.activeGameObject = rack.gameObject;
        Debug.Log($"ASRS_Module1_Rack_Hookup: '{rack.name}' hooked up — {slots} row-5 slot(s) wired. Save the scene.", rack);
    }

    private static void CopyLessonSettings(Item_ASRS from, Item_ASRS to)
    {
        SerializedObject source = new SerializedObject(from);
        SerializedObject target = new SerializedObject(to);

        foreach (string name in LessonRackSettings)
        {
            SerializedProperty prop = source.FindProperty(name);
            if (prop != null && target.FindProperty(name) != null)
            {
                target.CopyFromSerializedProperty(prop);
                Debug.Log($"ASRS_Module1_Rack_Hookup: copied {name} from '{from.name}' to '{to.name}'.", to);
            }
        }

        target.ApplyModifiedProperties();
    }

    // The loose table by name, ignoring any table that's part of a rack.
    private static Item_Slotted_Table FindLessonTable(Item_ASRS rack)
    {
        return Object.FindObjectsByType<Item_Slotted_Table>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(t => t.name == LessonTableName && t.GetComponentInParent<Item_ASRS>(true) == null);
    }

    private static void PrepareLessonTable(Item_Slotted_Table table)
    {
        SerializedObject so = new SerializedObject(table);
        so.FindProperty("TableID").stringValue = LessonTableId;
        so.FindProperty("Pickup").boolValue = true;
        so.ApplyModifiedProperties();

        Debug.Log($"ASRS_Module1_Rack_Hookup: '{table.name}' is now carryable with TableID {LessonTableId} (its empty home slot in row 5).", table);
    }

    private static void PointLoadStepAt(SequenceManager sequenceManager, Item_Slotted_Table table)
    {
        SerializedObject so = new SerializedObject(sequenceManager);
        SerializedProperty steps = so.FindProperty("steps");

        for (int i = 0; i < steps.arraySize; i++)
        {
            SerializedProperty step = steps.GetArrayElementAtIndex(i);
            if (step.FindPropertyRelative("requiredActionId").stringValue != LoadTableActionId)
                continue;

            // The previous Item_Plate lives on a marker that's inactive in
            // the scene, so it could never receive the block — replaced by
            // the real slotted table.
            step.FindPropertyRelative("RequiredOccupiedTable").objectReferenceValue = null;
            step.FindPropertyRelative("RequiredLoadedSlottedTable").objectReferenceValue = table;
            Debug.Log($"ASRS_Module1_Rack_Hookup: step {i + 1} ('{LoadTableActionId}') now waits for an item on '{table.name}'.", table);
        }

        so.ApplyModifiedProperties();
    }
}
