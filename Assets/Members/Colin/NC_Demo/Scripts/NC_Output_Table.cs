using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Finished blocks are set down on authored slots: one row per category in the order categories
/// first appear, each block with a label in front of it. Rows, their category labels and their
/// slots are scene objects; the part label is a prefab instantiated under the slot, so its pose
/// relative to the slot is authored on the prefab.
/// </summary>
public class NC_Output_Table : MonoBehaviour
{
    [Serializable]
    public class Row
    {
        public TMP_Text Label;          // shows the category; hidden until the row is used
        public Transform[] Slots;       // one block each, filled in order
        [NonSerialized] public string Category;
        [NonSerialized] public int Filled;
    }

    [SerializeField] private Row[] Rows;
    [SerializeField] private TMP_Text Part_Label_Prefab;

    readonly List<GameObject> placed = new List<GameObject>();

    void Awake()
    {
        if (Rows == null || Rows.Length == 0 || Part_Label_Prefab == null)
        {
            Debug.LogWarning("NC_Output_Table: Rows and Part_Label_Prefab must be assigned.");
            enabled = false;
            return;
        }
        foreach (var row in Rows)
        {
            if (row.Label != null && row.Slots != null && row.Slots.Length > 0) continue;
            Debug.LogWarning("NC_Output_Table: every row needs a Label and at least one slot.");
            enabled = false;
            return;
        }
        Clear();
    }

    /// <summary>Destroys every placed block and label and hides the row labels.</summary>
    public void Clear()
    {
        foreach (var go in placed)
            if (go != null) Destroy(go);
        placed.Clear();
        foreach (var row in Rows)
        {
            row.Category = null;
            row.Filled = 0;
            row.Label.gameObject.SetActive(false);
        }
    }

    public void Place(GameObject block, string category, string text)
    {
        var row = Find_Row(category);
        if (row == null)
        {
            Debug.LogWarning("NC_Output_Table: no free row for category '" + category + "'; " + block.name + " stays where it is.");
            return;
        }
        int slot = row.Filled++;
        if (slot >= row.Slots.Length)
        {
            Debug.LogWarning("NC_Output_Table: row '" + category + "' is full; " + block.name + " shares its last slot.");
            slot = row.Slots.Length - 1;
        }

        var anchor = row.Slots[slot];
        block.transform.SetParent(anchor, false);
        block.transform.localPosition = Vector3.zero;
        block.transform.localRotation = Quaternion.identity;
        placed.Add(block);

        var label = Instantiate(Part_Label_Prefab, anchor);   // keeps the prefab's pose relative to the slot
        label.text = text;
        placed.Add(label.gameObject);
    }

    // The row already holding this category, else the first unused one (null when all are taken).
    Row Find_Row(string category)
    {
        foreach (var row in Rows)
            if (row.Category == category) return row;
        foreach (var row in Rows)
        {
            if (row.Category != null) continue;
            row.Category = category;
            row.Label.text = category;
            row.Label.gameObject.SetActive(true);
            return row;
        }
        return null;
    }
}
