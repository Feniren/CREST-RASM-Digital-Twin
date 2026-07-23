using System.Collections;

using UnityEngine;
using UnityEngine.Events;

public class Item_RFID_Sensor : Item_Parent{
	public Item_Conveyor_Belt ConveyorBeltReference;
	public Item_CNC_Machine CNCMachineReference;

	public Item_Parent TargetItem;
	public Item_Plate SensedPlate;

	public UnityEvent OnTargetFound;

	public Item_RFID_Sensor(){
		Name = "RFID Sensor";
		Pickup = false;
		Quantity = 1;
	}

	public void OnTriggerEnter(Collider OverlappedCollider){
		GameObject OverlappedObject = OverlappedCollider.gameObject;

		if (!CNCMachineReference.ProcessingItem){
			if (OverlappedObject.GetComponent<Item_Plate>()){
				SensedPlate = OverlappedObject.GetComponent<Item_Plate>();

				if (TargetItem){
					if (SensedPlate.Item){
						if (SensedPlate.Item.GetComponentInParent<Item_Parent>().GetType() == TargetItem.GetType()){
							ConveyorBeltReference.ToggleMovement();

							TargetItem = null;

							OnTargetFound.Invoke();

							Invoke(nameof(PauseConveyorBelt), 3.0f);
						}
					}
				}
			}
		}
	}

	public void OnTriggerExit(Collider OverlappedCollider){
		GameObject OverlappedObject = OverlappedCollider.gameObject;

		if (OverlappedObject.GetComponent<Item_Plate>()){
			SensedPlate = null;
		}
	}

	public void PauseConveyorBelt(){
		ConveyorBeltReference.ToggleMovement();
	}
}
