using System;
using UnityEngine;

public class Entity_XR_Hand : MonoBehaviour{
	GameObject ItemReference;
	Vector3 PreviousPosition;
	Vector3 Velocity;
	
	public Entity_XR_Hand(){
	}

	public void Start(){
		PreviousPosition = transform.position;
	}

	public void Update(){
		Velocity = ((transform.position - PreviousPosition) / Time.deltaTime);

		PreviousPosition = transform.position;
	}
	
	public void GrabStart(Entity_Player PlayerReference, GameObject Item){
		ItemReference = Item;

		if (ItemReference.GetComponent<Item_Parent>().Pickup){
			ItemReference.transform.SetParent(gameObject.transform);

			ItemReference.layer = 3;

			ItemReference.GetComponent<Rigidbody>().isKinematic = true;

			GetComponent<Collider>().enabled = false;
		}
		else{
			ItemReference.GetComponent<Item_Parent>().AlternateInteract(PlayerReference);
		}
	}

	public void GrabEnd(){
		if (!GetComponent<Collider>().enabled){
			if (ItemReference.transform.parent == gameObject.transform){
				ItemReference.transform.SetParent(null, true);

				ItemReference.layer = 1;

				ItemReference.GetComponent<Rigidbody>().isKinematic = false;
				ItemReference.GetComponent<Rigidbody>().linearVelocity = Velocity;

				GetComponent<Collider>().enabled = true;
			}
		}
	}
}
