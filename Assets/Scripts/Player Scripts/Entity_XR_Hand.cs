using System;
using UnityEngine;

public class Entity_XR_Hand : MonoBehaviour{
	GameObject ItemReference;
	
	public Entity_XR_Hand(){
	}
	
	public void GrabStart(Entity_Player PlayerReference, GameObject Item){
		ItemReference = Item;

		if (ItemReference.GetComponent<Item_Parent>().Pickup){
			ItemReference.transform.SetParent(gameObject.transform);

			ItemReference.GetComponent<Rigidbody>().isKinematic = true;

			GetComponent<Collider>().enabled = false;
		}
		else{
			ItemReference.GetComponent<Item_Parent>().AlternateInteract(PlayerReference);
		}
	}

	public void GrabEnd(){
		if (!GetComponent<Collider>().enabled){
			ItemReference.transform.SetParent(null, true);
			
			ItemReference.GetComponent<Rigidbody>().isKinematic = false;

			GetComponent<Collider>().enabled = true;
		}
	}
}
