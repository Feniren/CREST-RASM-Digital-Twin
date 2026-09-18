using System.Collections;

using UnityEngine;

public class Interactable_User_Interface : Interactable_Parent{
	RectTransform RectTransformReference;
	Coroutine TrackActiveHandInstance;
	Entity_XR_Hand XRHandReference;

	Vector3 PositionOffset;
	Quaternion RotationOffset;

	public Interactable_User_Interface(){
	}

	public void Awake(){
		RectTransformReference = GetComponent<RectTransform>();
	}

	public override void Interact(Entity_Player PlayerReference){
		base.Interact(PlayerReference);
	}

	public override void AlternateInteract(Entity_Player PlayerReference){
		base.AlternateInteract(PlayerReference);

		XRHandReference = PlayerReference.ActiveHand.GetComponent<Entity_XR_Hand>();

		PositionOffset = XRHandReference.transform.InverseTransformPoint(RectTransformReference.position);
		RotationOffset = (Quaternion.Inverse(XRHandReference.transform.rotation) * RectTransformReference.rotation);

		TrackActiveHandInstance = StartCoroutine(TrackActiveHand(XRHandReference.transform));

		XRHandReference.OnGrabEnd.AddListener(OnGrabEnd);
	}

	public void OnGrabEnd(){
		StopCoroutine(TrackActiveHandInstance);

		TrackActiveHandInstance = null;

		XRHandReference.OnGrabEnd.RemoveListener(OnGrabEnd);
	}

	private IEnumerator TrackActiveHand(Transform ActiveHandTransform){
		while (true){
			RectTransformReference.position = ActiveHandTransform.TransformPoint(PositionOffset);
			RectTransformReference.rotation = (ActiveHandTransform.rotation * RotationOffset);

			yield return null;
		}
	}
    
}
