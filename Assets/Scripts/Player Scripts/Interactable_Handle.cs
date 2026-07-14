using UnityEngine;

public class Interactable_Handle : MonoBehaviour, Interact_Interface{
	public Entity_Player PlayerReference;

	private BoxCollider BoxColliderReference;
	private Rigidbody RigidBodyReference;

	private float DampingForce = 80.0f;
	private float PullForce = 100.0f;

	bool Grabbed;
	Vector3 Velocity;

	public Interactable_Handle(){
	}

	public void Start(){
		BoxColliderReference = GetComponent<BoxCollider>();
		RigidBodyReference = GetComponent<Rigidbody>();
	}

	private void FixedUpdate(){
		if (Grabbed){
			Vector3 HandlePosition = transform.TransformPoint(BoxColliderReference.center);
			Vector3 Distance = (PlayerReference.ActiveHand.gameObject.transform.position - HandlePosition);

			if (Distance.magnitude < 0.01f){
				Distance = Vector3.zero;
			}

			Velocity = ((Distance * PullForce) - (RigidBodyReference.GetPointVelocity(HandlePosition) * DampingForce));

			RigidBodyReference.AddForceAtPosition(Velocity, HandlePosition, ForceMode.Force);
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
