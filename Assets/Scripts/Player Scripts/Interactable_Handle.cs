using UnityEngine;

public class Interactable_Handle : MonoBehaviour, Interact_Interface{
	public Entity_Player PlayerReference;

	bool Grabbed;
	Vector3 Velocity;

	public Interactable_Handle(){
	}

	public void Start(){
	}

	private void FixedUpdate(){
		if (Grabbed){
			Velocity = PlayerReference.ActiveHand.gameObject.transform.position - transform.position;

			GetComponent<Rigidbody>().AddForceAtPosition(Velocity, GetComponent<BoxCollider>().center, ForceMode.Impulse);
			//GetComponent<Rigidbody>().AddForce(Velocity, ForceMode.Impulse);
		}
	}

	public void Interact(Entity_Player PlayerReference){
	}

	public void AlternateInteract(Entity_Player PlayerReference){
		this.PlayerReference = PlayerReference;

		PlayerReference.LeftHandAnchor.GetComponentInChildren<Entity_XR_Hand>().OnGrabEnd.AddListener(OnGrabEnd);
		PlayerReference.RightHandAnchor.GetComponentInChildren<Entity_XR_Hand>().OnGrabEnd.AddListener(OnGrabEnd);

		Grabbed = true;
	}

	public void OnGrabEnd(){
		PlayerReference.LeftHandAnchor.GetComponentInChildren<Entity_XR_Hand>().OnGrabEnd.RemoveListener(OnGrabEnd);
		PlayerReference.RightHandAnchor.GetComponentInChildren<Entity_XR_Hand>().OnGrabEnd.RemoveListener(OnGrabEnd);

		Grabbed = false;
	}
}
