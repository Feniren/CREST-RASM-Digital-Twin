using System.Collections;
using UnityEngine;

public class Item_RFID_Sensor : Item_Parent{
	public Item_Conveyor_Belt ConveyorBeltReference;
	public Item_CNC_Machine CNCMachineReference;

	public Item_Parent TargetItem;

	public Item_RFID_Sensor(){
		Name = "RFID Sensor";
		Pickup = false;
		Quantity = 1;
	}

	public void OnTriggerEnter(Collider OverlappedCollider){
		GameObject OverlappedObject = OverlappedCollider.gameObject;

		if (!CNCMachineReference.ProcessingItem){
			if (OverlappedObject.GetComponent<Item_Plate>()){
				if (OverlappedObject.GetComponent<Item_Plate>().Item){
					ConveyorBeltReference.ToggleMovement();

					Destroy(OverlappedObject.GetComponent<Item_Plate>().Item);

					Invoke(nameof(PauseConveyorBelt), 3.0f);

					GameObject NewItem = Instantiate(FindFirstObjectByType<Data_Loader>().ItemLibraryReference.GetItemFromName("Epoxy Penholder"), Vector3.zero, Quaternion.identity);

					OverlappedObject.GetComponent<Item_Plate>().Item = NewItem;
					OverlappedObject.GetComponent<Item_Plate>().SetItem();
				}
			}
		}
	}

	public void PauseConveyorBelt(){
		ConveyorBeltReference.ToggleMovement();
	}
}
