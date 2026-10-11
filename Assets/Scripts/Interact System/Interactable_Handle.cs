using UnityEngine;

public class Interactable_Handle : Interactable_Parent{
	BoxCollider BoxColliderReference;
	Entity_Player PlayerReference;
	Rigidbody RigidBodyReference;

	float DampingForce = 80.0f;
	float PullForce = 100.0f;

	bool Grabbed;
	Vector3 Velocity;

	public Interactable_Handle(){
	}

	public void Start(){
		BoxColliderReference = GetComponent<BoxCollider>();
		PlayerReference = Entity_Player.GetLocalPlayer();
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

	public override void Interact(Entity_Player PlayerReference){
		base.Interact(PlayerReference);
	}

	public override void AlternateInteract(Entity_Player PlayerReference){
		base.Interact(PlayerReference);

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
