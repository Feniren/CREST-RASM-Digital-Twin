using UnityEngine;

<<<<<<< HEAD
public class Item_Lathe : Item_Station{

	private bool _isProcessing;

	public override bool IsProcessing => _isProcessing;

	public override void ProcessItem(Item_Slotted_Table table, Job_Queue queue, Spline_Animate spline){
		// TODO: implement lathe processing
	}
=======
public class Item_Lathe : Item_Parent{
	public Item_Plate ActiveTable;
>>>>>>> main

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
