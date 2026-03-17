using TMPro;
using UnityEngine;

public class Item_Laser_Engraver : Item_Parent{
	public e_Item_Slotted_Table ActiveTable;
	public string EngraveText;
	public Item_Laser_Engraver(){
		Name = "Laser Engraver";
		Pickup = false;
		Quantity = 1;
	}

	public override void Start(){
		base.Start();
		Engrave(ActiveTable);
	}

	public override void Interact(Entity_Player PlayerReference){
		base.Interact(PlayerReference);
	}

	public override void AlternateInteract(Entity_Player PlayerReference){
	}

	public void Engrave(e_Item_Slotted_Table EngraveTable) {
		Debug.Log("Laser engrave attempt...");
		EngraveTable.ApplyEngraving(EngraveText);
	}
}
