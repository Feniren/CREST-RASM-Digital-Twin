using UnityEngine;

public class Item_ASRS : Item_Parent{
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
