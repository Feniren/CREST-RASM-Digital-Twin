using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Finished blocks are set down on the table in rows by category, in the order categories
/// first appear, each with a flat label in front of it. Table_Top is the grid origin on the
/// top surface: rows advance along its +X (away from the operator), slots along its +Z.
/// </summary>
public class NC_Output_Table : MonoBehaviour
{
    public Transform Table_Top;
    public int Rows = 5;
    public int Slots_Per_Row = 3;
    public float Row_Pitch = 0.15f;
    public float Slot_Pitch = 0.16f;
    public float Label_Font_Size = 9f;

    readonly List<string> rows = new List<string>();
    readonly List<int> filled = new List<int>();
    readonly List<GameObject> placed = new List<GameObject>();

    public void Clear()
    {
        foreach (var go in placed)
            if (go != null) Destroy(go);
        placed.Clear();
        rows.Clear();
        filled.Clear();
    }

    public void Place(GameObject block, string category, string text)
    {
        int row = rows.IndexOf(category);
        if (row < 0)
        {
            rows.Add(category);
            filled.Add(0);
            row = rows.Count - 1;
            if (row >= Rows) Debug.LogWarning("NC table: " + category + " is category " + (row + 1) + " but the table has " + Rows + " rows");
            placed.Add(Make_Label(new Vector3(row * Row_Pitch + 0.05f, 0.001f, -0.065f), category, Label_Font_Size * 1.4f, new Vector2(10f, 2f), TextAlignmentOptions.Center));
        }
        int slot = filled[row]++;
        if (slot >= Slots_Per_Row) Debug.LogWarning("NC table: row " + category + " is full; " + block.name + " overhangs");

        var local = new Vector3(row * Row_Pitch, 0f, slot * Slot_Pitch);
        block.transform.SetParent(Table_Top, false);
        block.transform.localPosition = local;
        block.transform.localRotation = Quaternion.identity;
        block.transform.localScale = Vector3.one;
        placed.Add(block);
        placed.Add(Make_Label(local + new Vector3(-0.026f, 0.001f, 0.05f), text, Label_Font_Size, new Vector2(10f, 4.6f), TextAlignmentOptions.TopLeft));
    }

    // A flat label read from the operator's side: text runs toward -Z, lines stack away (+X).
    GameObject Make_Label(Vector3 local, string text, float size, Vector2 rect, TextAlignmentOptions align)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(Table_Top, false);
        go.transform.localPosition = local;
        go.transform.localRotation = Quaternion.LookRotation(Vector3.down, Vector3.right);
        go.transform.localScale = Vector3.one * 0.01f;
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.rectTransform.sizeDelta = rect;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = Color.black;
        tmp.alignment = align;
        return go;
    }
}
