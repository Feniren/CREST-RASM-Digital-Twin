using System.Collections;
using UnityEngine;

public class Item_RFID_Sensor : Item_Parent{
	public Item_Conveyor_Belt ConveyorBeltReference;

	GameObject Previous;

	public Item_RFID_Sensor(){
		Name = "RFID Sensor";
		Pickup = false;
		Quantity = 1;
	}

	public void OnTriggerEnter(Collider OverlappedCollider){
		GameObject OverlappedObject = OverlappedCollider.gameObject;

		if (OverlappedObject.GetComponent<Item_Plate>()){
			if (OverlappedObject != Previous){
				if (OverlappedObject.GetComponent<Item_Plate>().Item){
					Previous = OverlappedObject;

					ConveyorBeltReference.ToggleMovement();

					Invoke(nameof(PauseConveyorBelt), 3.0f);
				}
			}
		}
	}

	public void PauseConveyorBelt(){
		ConveyorBeltReference.ToggleMovement();
	}
}
