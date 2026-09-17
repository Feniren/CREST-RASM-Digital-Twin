using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public class Item_ASRS : Item_CNC_Machine{
	// public List<Machine_Job> Jobs = new List<Machine_Job>();
	public RACK_TASK task;
	public Item_Slotted_Table item = null;
	public Item_Epoxy_Block material = null;
	public List<Item_Slotted_Table> TableList = new List<Item_Slotted_Table>();
	public List<GameObject> BlockList = new List<GameObject>();

	public Item_ASRS(){
		Name = "ASRS";
		Pickup = false;
		Quantity = 1;
	}


    public override void Start(){
		base.Start();

	}

    public override void Interact(Entity_Player PlayerReference){
		base.Interact(PlayerReference);
	}

	public override void AlternateInteract(Entity_Player PlayerReference){
	}



	public void Retrieve(Item_Slotted_Table target)
	{
        if (target == null)
            throw new ArgumentNullException(nameof(target), "Target table cannot be null.");

        if (string.IsNullOrWhiteSpace(target.TableID))
            throw new ArgumentException("Table ID cannot be null or empty.");

        int id = int.Parse(target.TableID);
        int table_index = GetIndex(id);

        if (table_index < 0 || table_index >= TableList.Count)
            throw new IndexOutOfRangeException($"Computed index {table_index} is out of bounds.");

        Item_Slotted_Table occupied = TableList[table_index];

        if (occupied == null)
            throw new InvalidOperationException("No item exists in the requested slot.");

        TableList[table_index] = null;

        GameObject retrievedItem = occupied.Item;
        if (retrievedItem != null)
        {
            target.Item = retrievedItem;
            retrievedItem.transform.SetParent(target.AnchorPoint.transform, false);
            retrievedItem.transform.position = target.AnchorPoint.transform.position;
            retrievedItem.transform.rotation = target.AnchorPoint.transform.rotation;
            Debug.Log("Placed Item_Epoxy_Block onto table: " + target.TableID);
        }
	}

	public void Insert(Item_Slotted_Table table)
	{
        if (table == null)
            throw new ArgumentNullException(nameof(table), "Table object cannot be null.");

        if (string.IsNullOrWhiteSpace(table.TableID))
            throw new ArgumentException("Table ID cannot be null or empty.");

        int id = int.Parse(table.TableID);
        int table_index = GetIndex(id);

        if (table_index < 0 || table_index >= TableList.Count)
            throw new IndexOutOfRangeException($"Computed index {table_index} is out of bounds.");

        Item_Slotted_Table occupied = TableList[table_index];

        if (occupied != null)
            throw new InvalidOperationException("Slot is occupied.");

        TableList[table_index] = table;
	}

	// TEMP TESTER: retrieves the first block from BlockList onto the target table
	public void BlockRetrieve(Item_Slotted_Table target)
	{
		if (target == null)
		{
			Debug.LogError("BlockRetrieve: target table is null.");
			return;
		}

		if (BlockList.Count == 0)
		{
			Debug.LogError("BlockRetrieve: BlockList is empty.");
			return;
		}

		GameObject block = BlockList[0];
		BlockList.RemoveAt(0);

		block.SetActive(true);
		target.Item = block;
		block.transform.SetParent(target.AnchorPoint.transform, true);
		block.transform.position = target.AnchorPoint.transform.position;
		block.transform.rotation = target.AnchorPoint.transform.localRotation;
		block.transform.localPosition = Vector3.zero;

		Debug.Log($"BlockRetrieve: Placed block onto table '{target.TableID}'. Remaining: {BlockList.Count}");
	}

	// TEMP TESTER: takes the block off the target table and returns it to BlockList
	public void BlockInsert(Item_Slotted_Table target)
	{
		if (target == null)
		{
			Debug.LogError("BlockInsert: target table is null.");
			return;
		}

		if (target.Item == null)
		{
			Debug.LogError("BlockInsert: table '" + target.TableID + "' has no block to insert.");
			return;
		}

		GameObject block = target.Item;
		target.Item = null;

		block.transform.SetParent(transform, true);
		block.SetActive(false);
		BlockList.Add(block);

		Debug.Log($"BlockInsert: Returned block from table '{target.TableID}' to BlockList. Total: {BlockList.Count}");
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