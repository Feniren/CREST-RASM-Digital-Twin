using UnityEngine;

public class Item_Mill : Item_Parent{
	public Item_Slotted_Table ActiveTable;

	public Item_Mill(){
		Name = "Mill";
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
