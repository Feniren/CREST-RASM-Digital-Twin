using UnityEngine;
using UnityEngine.InputSystem;

public class Inventory_Bag : MonoBehaviour, Interact_Interface{
	public GameObject ItemInstance;
	public Entity_Player PlayerReference;

	public Inventory_Bag(){
	}

	public void Interact(Entity_Player PlayerReference){
	}

	public void Start(){
		PlayerReference.LeftHandAnchor.GetComponentInChildren<Entity_XR_Hand>().OnGrabEnd.AddListener(OnGrabEnd);
		PlayerReference.RightHandAnchor.GetComponentInChildren<Entity_XR_Hand>().OnGrabEnd.AddListener(OnGrabEnd);
	}

	public void AlternateInteract(Entity_Player PlayerReference){
		if (PlayerReference.InventoryReference.StaticInventory.Count > 0){
			ItemInstance = Instantiate(PlayerReference.ItemLibraryReference.GetItemFromName(PlayerReference.InventoryReference.StaticInventory[^1].Key), (PlayerReference.ActiveHand.gameObject.transform.position + (PlayerReference.ActiveHand.gameObject.transform.forward * 0.1f)), Quaternion.identity);

			PlayerReference.InventoryReference.RemoveFromInventory(PlayerReference.InventoryReference.StaticInventory[^1].Key, 1);

			PlayerReference.ActiveHand.ItemReference = ItemInstance;

			PlayerReference.ActiveHand.HoldItem();

			Debug.Log("Item found in bag. Holding");
		}
		else{
			Debug.Log("No Items found in bag");
		}
	}

	public void OnGrabEnd(){
		Collider[] OverlappedObjects;

		Debug.Log("OnGrab end invoked, trying to add to bag");

		OverlappedObjects = Physics.OverlapBox(transform.position, new Vector3(0.5f, 2.0f, 0.5f));

		Debug.Log("Bag overlapped with " + OverlappedObjects.Length + " objects");

		for (int Index = 0; Index < OverlappedObjects.Length; Index++){
			if (OverlappedObjects[Index].gameObject.GetComponentInParent<Item_Parent>()){
				if (OverlappedObjects[Index].gameObject.GetComponentInParent<Item_Parent>().Pickup){
					Item_Parent Item = OverlappedObjects[Index].GetComponentInParent<Item_Parent>();

					Debug.Log("Item exists. Adding to inventory");

					PlayerReference.InventoryReference.AddToInventory(Item.Name, 1);

					Destroy(OverlappedObjects[Index]);
				}
			}
		}
	}
}
