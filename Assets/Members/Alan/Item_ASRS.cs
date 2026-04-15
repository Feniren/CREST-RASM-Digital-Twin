using System;
using System.Collections.Generic;
using UnityEngine;

public class Item_ASRS : Item_Parent
{
    public RACK_TASK task;
    public Item_Slotted_Table item = null;
    public Item_Epoxy_Block material = null;
    public List<GameObject> BlockList = new List<GameObject>();
    public Spline_Animate spline;

    public Dictionary<int, Item_Slotted_Table> TableMap = new Dictionary<int, Item_Slotted_Table>();

    private readonly Dictionary<int, Vector3> anchorPositions = new Dictionary<int, Vector3>();
    private readonly Dictionary<int, Quaternion> anchorRotations = new Dictionary<int, Quaternion>();

    public Item_ASRS()
    {
        Name = "ASRS";
        Pickup = false;
        Quantity = 1;
    }

    public override void Start()
    {
        base.Start();

        anchorPositions.Clear();
        anchorRotations.Clear();
        TableMap.Clear();

        foreach (Item_Slotted_Table table in GetComponentsInChildren<Item_Slotted_Table>(true))
        {
            if (table == null || string.IsNullOrWhiteSpace(table.TableID))
                continue;

            if (!int.TryParse(table.TableID, out int rawTableId))
            {
                Debug.LogWarning($"ASRS: Invalid TableID '{table.TableID}'.", table);
                continue;
            }

            int index;
            try
            {
                index = GetIndex(rawTableId);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Debug.LogError(ex.Message, table);
                continue;
            }

            int row = rawTableId / 10000;

            anchorPositions[index] = table.transform.position;
            anchorRotations[index] = table.transform.rotation;

            if (row == 5)
            {
                TableMap[index] = null;
                table.gameObject.SetActive(false);
            }
            else
            {
                TableMap[index] = table;
            }
        }

        Debug.Log($"ASRS: Anchors saved: {anchorPositions.Count}");
        Debug.Log($"ASRS: TableMap entries: {TableMap.Count}");
        Debug.Log($"ASRS: Vacant slots: {CountVacantSlots()}");
    }

    public override void Interact(Entity_Player PlayerReference)
    {
        base.Interact(PlayerReference);
    }

    public override void AlternateInteract(Entity_Player PlayerReference)
    {
    }

    private int CountVacantSlots()
    {
        int vacant = 0;

        foreach (var kvp in TableMap)
        {
            if (kvp.Value == null)
                vacant++;
        }

        return vacant;
    }

    public void SlotRetrieve(Item_Slotted_Table target)
    {
        if (target == null)
        {
            Debug.LogError("SlotRetrieve: target table is null.");
            return;
        }

        if (!TryGetTableIndex(target, out int index))
            return;

        if (!TableMap.TryGetValue(index, out Item_Slotted_Table rackSlot) || rackSlot == null)
        {
            Debug.LogError($"SlotRetrieve: No rack slot table found for '{target.TableID}'.", target);
            return;
        }

        if (!anchorPositions.TryGetValue(index, out Vector3 anchorPosition) ||
            !anchorRotations.TryGetValue(index, out Quaternion anchorRotation))
        {
            Debug.LogError($"SlotRetrieve: Missing saved anchor transform for index {index}.", target);
            return;
        }

        target.transform.SetParent(rackSlot.transform.parent, true);
        target.transform.position = anchorPosition;
        target.transform.rotation = anchorRotation;

        Spline_Animate splineAnimate = target.GetComponent<Spline_Animate>();
        if (splineAnimate != null)
        {
            splineAnimate.Pause();
            splineAnimate.Container = null;
        }

        TableMap[index] = target;
        target.gameObject.SetActive(true);

        Debug.Log($"SlotRetrieve: Table '{target.TableID}' placed into rack slot {index}.");
    }

    public void SlotInsert(Item_Slotted_Table target)
    {
        if (target == null)
        {
            Debug.LogError("SlotInsert: target table is null.");
            return;
        }

        if (!TryGetTableIndex(target, out int index))
            return;

        if (TableMap.TryGetValue(index, out Item_Slotted_Table occupied) && occupied != null)
        {
            Debug.LogWarning($"SlotInsert: Rack slot at index {index} is already occupied.", target);
            return;
        }

        if (!anchorPositions.TryGetValue(index, out Vector3 anchorPosition) ||
            !anchorRotations.TryGetValue(index, out Quaternion anchorRotation))
        {
            Debug.LogError($"SlotInsert: Missing saved anchor transform for index {index}.", target);
            return;
        }

        target.transform.SetParent(transform, true);
        target.transform.position = anchorPosition;
        target.transform.rotation = anchorRotation;

        Spline_Animate splineAnimate = target.GetComponent<Spline_Animate>();
        if (splineAnimate != null)
        {
            splineAnimate.Pause();
            splineAnimate.Container = null;
        }

        TableMap[index] = target;
        target.gameObject.SetActive(true);

        Debug.Log($"SlotInsert: Table '{target.TableID}' placed into rack slot {index}.");
    }

    public bool NeedsRackReturn(Item_Slotted_Table target)
    {
        if (target == null)
            return false;

        if (!TryGetTableIndex(target, out int index))
            return false;

        return !TableMap.TryGetValue(index, out Item_Slotted_Table occupied) || occupied == null;
    }

    public int GetIndex(int tableId)
    {
        int row = tableId / 10000;
        int col = tableId % 10;

        if (row < 1 || row > 12)
            throw new ArgumentOutOfRangeException(nameof(tableId), $"Row value {row} is out of range. Must be 1-12.");

        if (col < 1 || col > 6)
            throw new ArgumentOutOfRangeException(nameof(tableId), $"Column value {col} is out of range. Must be 1-6.");

        return (row - 1) * 6 + (col - 1);
    }

    private bool TryGetTableIndex(Item_Slotted_Table table, out int index)
    {
        index = -1;

        if (table == null || string.IsNullOrWhiteSpace(table.TableID))
        {
            Debug.LogError("ASRS: Table is null or missing TableID.", table);
            return false;
        }

        if (!int.TryParse(table.TableID, out int rawTableId))
        {
            Debug.LogError($"ASRS: Could not parse TableID '{table.TableID}'.", table);
            return false;
        }

        try
        {
            index = GetIndex(rawTableId);
            return true;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Debug.LogError($"ASRS: Invalid TableID '{table.TableID}'. {ex.Message}", table);
            return false;
        }
    }
}