using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;





public class Item_ASRS : Item_Parent{
	// public List<Machine_Job> Jobs = new List<Machine_Job>();
	public RACK_TASK task;
	public Item_Slotted_Table item = null;
	public Item_Epoxy_Block material = null;
	public List<Item_Slotted_Table> TableList = new List<Item_Slotted_Table>();

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

    

	public GameObject Retrieve(string table_id)
	{
    
        if (string.IsNullOrWhiteSpace(table_id))
            throw new ArgumentException("Table ID cannot be null or empty.");

        int id = int.Parse(table_id);
        int table_index = GetIndex(id);

        if (table_index < 0 || table_index >= TableList.Count)
            throw new IndexOutOfRangeException($"Computed index {table_index} is out of bounds.");

        Item_Slotted_Table occupied = TableList[table_index];

        if (occupied == null)
            throw new InvalidOperationException("No item exists in the requested slot.");

        TableList[table_index] = null;
        return occupied.Item;
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