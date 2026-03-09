using UnityEngine;

public class Item_Laser_Engraver : Item_Parent{
	public Item_Laser_Engraver(){
		Name = "Laser Engraver";
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
