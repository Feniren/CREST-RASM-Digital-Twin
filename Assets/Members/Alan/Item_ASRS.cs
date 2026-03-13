using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public enum rack_task
{
	RETRIEVE , INSERT
}



public class Item_ASRS : Item_Parent{
	public List<Machine_Job> Jobs = new List<Machine_Job>();
	public rack_task task;
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

    



	// this 
	public GameObject Retreive(string table_id)
	{
		int id = int.Parse(table_id);
        Item_Slotted_Table occupied = TableList[(id % 1000) * 6 + (id % 10)];

        if (occupied != null)
        {
			TableList[(id % 1000) * 6 + (id % 10)] = null;
             return occupied.Item;
        }
		return null;
	}

	public void Insert (Item_Slotted_Table table)
	{
		int id = int.Parse(table.TableID);
		Item_Slotted_Table occupied = TableList[(id % 1000) * 6 + (id % 10)];

        if ( occupied == null)
		{
			TableList[(id % 1000) * 6 + (id % 10)] = table;
		}
		throw new System.InvalidOperationException("Slot is occupied.");
    }
}