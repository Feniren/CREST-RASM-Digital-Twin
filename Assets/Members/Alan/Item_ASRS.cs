using UnityEngine;
using System.Collections.Generic;

public class Item_ASRS : Item_Station{

    private bool _isProcessing;

    public override bool IsProcessing => _isProcessing;

    public List<Item_Slotted_Table> TableList = new List<Item_Slotted_Table>();

	public Item_ASRS(){
		Name = "ASRS";
		Pickup = false;
		Quantity = 1;
	}

	public override void Start(){
		base.Start();
	}

    public override void ProcessItem(Item_Slotted_Table table, Job_Queue queue, Spline_Animate spline)
    {
        // TODO: implement lathe processing
    }

    public override void Interact(Entity_Player PlayerReference){
		base.Interact(PlayerReference);
	}

	public override void AlternateInteract(Entity_Player PlayerReference){
	}
}
