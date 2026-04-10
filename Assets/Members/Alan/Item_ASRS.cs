using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Item_ASRS : Item_Parent{
	public RACK_TASK task;
	public Item_Slotted_Table item = null;
	public Item_Epoxy_Block material = null;
	public List<GameObject> BlockList = new List<GameObject>();

	public Dictionary<int, Item_Slotted_Table> TableMap = new Dictionary<int, Item_Slotted_Table>();
	private Dictionary<int, Vector3> anchorPositions = new Dictionary<int, Vector3>();
	private Dictionary<int, Quaternion> anchorRotations = new Dictionary<int, Quaternion>();

	public Item_ASRS(){
		Name = "ASRS";
		Pickup = false;
		Quantity = 1;
	}


    public override void Start()
    {
        base.Start();

        foreach (Item_Slotted_Table table in GetComponentsInChildren<Item_Slotted_Table>(true))
        {
            if (string.IsNullOrWhiteSpace(table.TableID)) continue;
            int index = GetIndex(int.Parse(table.TableID));
            int row = int.Parse(table.TableID) / 10000;

            Debug.Log($"Found table: '{table.TableID}' | Row: {row} | Active: {table.gameObject.activeSelf}");

            anchorPositions[index] = table.transform.position;
            anchorRotations[index] = table.transform.rotation;

            if (row == 5)
            {
                TableMap[index] = null;
                table.gameObject.SetActive(false);
                Debug.Log($"Row 5 table '{table.TableID}' marked vacant and deactivated.");
            }
            else
            {
                TableMap[index] = table;
            }
        }

        Debug.Log($"ASRS: Total anchors saved: {anchorPositions.Count}");
        Debug.Log($"ASRS: TableMap entries: {TableMap.Count}");
        Debug.Log($"ASRS: Vacant slots: {CountVacantSlots()}");
    }

    private int CountVacantSlots()
    {
        int vacant = 0;
        foreach (var kvp in TableMap)
            if (kvp.Value == null) vacant++;
        return vacant;
    }

    public override void Interact(Entity_Player PlayerReference){
		base.Interact(PlayerReference);
	}

	public override void AlternateInteract(Entity_Player PlayerReference){
	}

	//public void Retrieve(Item_Slotted_Table target)
	//{
	//	if (target == null)
	//		throw new ArgumentNullException(nameof(target), "Target table cannot be null.");

	//	if (string.IsNullOrWhiteSpace(target.TableID))
	//		throw new ArgumentException("Table ID cannot be null or empty.");

	//	int index = GetIndex(int.Parse(target.TableID));

	//	if (!TableMap.TryGetValue(index, out Item_Slotted_Table occupied) || occupied == null)
	//		throw new InvalidOperationException("No item exists in the requested slot.");

	//	TableMap[index] = null;

	//	GameObject retrievedItem = occupied.Item;
	//	if (retrievedItem != null)
	//	{
	//		target.Item = retrievedItem;
	//		retrievedItem.transform.SetParent(target.transform, false);
	//		retrievedItem.transform.position = anchorPositions[index];
	//		retrievedItem.transform.rotation = anchorRotations[index];
	//		Debug.Log("Placed Item_Epoxy_Block onto table: " + target.TableID);
	//	}
	//}

	//public void Insert(Item_Slotted_Table table)
	//{
	//	if (table == null)
	//		throw new ArgumentNullException(nameof(table), "Table object cannot be null.");

	//	if (string.IsNullOrWhiteSpace(table.TableID))
	//		throw new ArgumentException("Table ID cannot be null or empty.");

	//	int index = GetIndex(int.Parse(table.TableID));

	//	if (TableMap.TryGetValue(index, out Item_Slotted_Table occupied) && occupied != null)
	//		throw new InvalidOperationException("Slot is occupied.");

	//	TableMap[index] = table;
	//}
    /*
	 * 
	 * TESTER FUNCTIONS 
	 * 
	*/

    //public void BlockRetrieve(Item_Slotted_Table target)
    //{
    //	if (target == null)
    //	{
    //		Debug.LogError("BlockRetrieve: target table is null.");
    //		return;
    //	}

    //	if (BlockList.Count == 0)
    //	{
    //		Debug.LogError("BlockRetrieve: BlockList is empty.");
    //		return;
    //	}

    //	int index = GetIndex(int.Parse(target.TableID));

    //	GameObject block = BlockList[0];
    //	BlockList.RemoveAt(0);

    //	block.SetActive(true);
    //	target.Item = block;
    //	block.transform.SetParent(target.transform, true);
    //	block.transform.position = anchorPositions[index];
    //	block.transform.rotation = anchorRotations[index];
    //	block.transform.localPosition = Vector3.zero;

    //	Debug.Log($"BlockRetrieve: Placed block onto table '{target.TableID}'. Remaining: {BlockList.Count}");
    //}

    //public void BlockInsert(Item_Slotted_Table target)
    //{
    //	if (target == null)
    //	{
    //		Debug.LogError("BlockInsert: target table is null.");
    //		return;
    //	}

    //	if (target.Item == null)
    //	{
    //		Debug.LogError("BlockInsert: table '" + target.TableID + "' has no block to insert.");
    //		return;
    //	}

    //	GameObject block = target.Item;
    //	target.Item = null;

    //	block.transform.SetParent(transform, true);
    //	block.SetActive(false);
    //	BlockList.Add(block);

    //	Debug.Log($"BlockInsert: Returned block from table '{target.TableID}' to BlockList. Total: {BlockList.Count}");
    //}

    public void SlotRetrieve(Item_Slotted_Table target)
    {
        if (target == null)
        {
            Debug.LogError("SlotRetrieve: target table is null.");
            return;
        }
        Debug.Log($"TableID raw value: '{target.TableID}' | Parsed: {int.Parse(target.TableID)} | row: {int.Parse(target.TableID) / 10000}");
        int index = GetIndex(int.Parse(target.TableID));

        if (!TableMap.TryGetValue(index, out Item_Slotted_Table rackSlot) || rackSlot == null)
        {
            Debug.LogError($"SlotRetrieve: No table found in rack slot for TableID '{target.TableID}'.");
            return;
        }

        target.transform.SetParent(rackSlot.transform.parent, true);
        target.transform.position = anchorPositions[index];
        target.transform.rotation = anchorRotations[index];

        TableMap[index] = target;

        Debug.Log($"SlotRetrieve: Table '{target.TableID}' retrieved into rack slot at index {index}.");
    }

    public void SlotInsert(Item_Slotted_Table target)
    {
        if (target == null)
        {
            Debug.LogError("SlotInsert: target table is null.");
            return;
        }

        int index = GetIndex(int.Parse(target.TableID));

        if (TableMap.TryGetValue(index, out Item_Slotted_Table occupied) && occupied != null)
        {
            Debug.LogWarning($"SlotInsert: Rack slot at index {index} is already occupied. Skipping.");
            return;
        }

        target.transform.SetParent(transform, true);
        target.transform.position = anchorPositions[index];
        target.transform.rotation = anchorRotations[index];

        TableMap[index] = target;

        Debug.Log($"SlotInsert: Table '{target.TableID}' inserted into rack at index {index}.");
    }

    public bool NeedsRackReturn(Item_Slotted_Table target)
    {
        if (target == null) return false;

        int index = GetIndex(int.Parse(target.TableID));

        // If the slot is empty in the map, the table hasn't been returned yet
        return !TableMap.TryGetValue(index, out Item_Slotted_Table occupied) || occupied == null;
    }

    public int GetIndex(int table_id)
	{
		int row = table_id / 10000;
		int col = table_id % 10;

		if (row < 1 || row > 12)
			throw new ArgumentOutOfRangeException(nameof(table_id), $"Row value {row} is out of range. Must be 1-12.");

		if (col < 1 || col > 6)
			throw new ArgumentOutOfRangeException(nameof(table_id), $"Column value {col} is out of range. Must be 1-6.");

		return (row - 1) * 6 + (col - 1);
	}
}
