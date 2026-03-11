using UnityEngine;
using System.Collections.Generic;

public class Item_ASRS : Item_Parent{
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
}
