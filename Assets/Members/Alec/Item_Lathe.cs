using UnityEngine;

public class Item_Lathe : Item_Parent{
	public Item_Plate ActiveTable;

	public Item_Lathe(){
		Name = "Lathe";
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
