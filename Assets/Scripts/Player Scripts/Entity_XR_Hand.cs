using System;
using UnityEngine;
using UnityEngine.Events;

public class Entity_XR_Hand : MonoBehaviour{
	public GameObject ItemReference;
	public UnityEvent OnGrabEnd;

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
	
	public void GrabStart(Entity_Player PlayerReference, GameObject Interactable){
		PlayerReference.ActiveHand = this;

		if (Interactable.GetComponent<Item_Parent>()){
			ItemReference = Interactable;

			if (ItemReference.GetComponent<Item_Parent>().Pickup){
				HoldItem();
			}
			else{
				ItemReference.GetComponent<Interact_Interface>().AlternateInteract(PlayerReference);
			}

			ItemReference.GetComponent<Item_Parent>().OnGrabbed.Invoke();
		}
		else{
			Interactable.GetComponent<Interact_Interface>().AlternateInteract(PlayerReference);

			GetComponent<Collider>().enabled = false;
		}
	}

	public void GrabEnd(){
		if (!GetComponent<Collider>().enabled){
			if (ItemReference){
				if (ItemReference.transform.parent == gameObject.transform){
					ItemReference.transform.SetParent(null, true);

					ItemReference.layer = 1;

					ItemReference.GetComponent<Rigidbody>().isKinematic = false;
					ItemReference.GetComponent<Rigidbody>().linearVelocity = Velocity;

					GetComponent<Collider>().enabled = true;
				}
			}

			OnGrabEnd.Invoke();
		}
	}

	public void HoldItem(){
		ItemReference.transform.SetParent(gameObject.transform);

		ItemReference.layer = 3;

		ItemReference.GetComponent<Rigidbody>().isKinematic = true;

		GetComponent<Collider>().enabled = false;
	}
}
