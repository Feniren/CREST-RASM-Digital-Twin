using System.Collections;

using UnityEngine;

public class Spring_Arm : MonoBehaviour{
	[SerializeField]
	LayerMask CollisionMask;

	[SerializeField]
	Vector3 FirstPersonLocation;

	[SerializeField]
	float SphereCastPadding;

	[SerializeField]
	float SphereCastRadius;

	float CurrentLength;
	TransformPoint Origin;
	Player_Settings PlayerSettings;
	Coroutine SpringArmOperation;

	public Spring_Arm(){
	}

	void Awake(){
		Origin = new TransformPoint();
	}

	public void OnDisable(){
		DisableSpringArm();
	}

	void Start(){
		PlayerSettings = Entity_Player.GetLocalPlayer().PlayerSettings;
		CurrentLength = PlayerSettings.ThirdPersonCameraDistance;
	}

	IEnumerator CalculateSpringArm(){
		while (true){
			Vector3 Direction;
			RaycastHit Hit;
			float SphereCastLength;

			Origin.Location = Entity_Player.GetLocalPlayer().gameObject.transform.position;

			if (PlayerSettings.ThirdPersonControlRotation){
				Origin.Rotation = transform.rotation;
			}

			Origin.Location += PlayerSettings.ThirdPersonCameraOffset;

			Direction = -Origin.GetForwardVector();

			SphereCastLength = PlayerSettings.ThirdPersonCameraDistance;

			if (Physics.SphereCast(Origin.Location, SphereCastRadius, Direction, out Hit, PlayerSettings.ThirdPersonCameraDistance, CollisionMask, QueryTriggerInteraction.Ignore)){
				SphereCastLength = Mathf.Max((Hit.distance - SphereCastPadding), 0.0f);
			}

			if (CurrentLength > SphereCastLength){
				CurrentLength = SphereCastLength;
			}
			else{
				CurrentLength = Mathf.Lerp(CurrentLength, SphereCastLength, (1.0f - Mathf.Exp(-PlayerSettings.ThirdPersonCameraSpringArmForce * Time.deltaTime)));
			}

			transform.position = (Origin.Location + (Direction * CurrentLength));

			if (!PlayerSettings.ThirdPersonControlRotation){
				transform.LookAt(Origin.Location);
			}

			yield return null;
		}
	}

	void DisableSpringArm(){
		if (SpringArmOperation != null){
			StopCoroutine(SpringArmOperation);

			SpringArmOperation = null;
		}
	}

	public TransformPoint GetOrigin(){
		return Origin;
	}

	public void SetThirdPerson(bool Enabled){
		if (!Entity_Player.GetLocalPlayer().PlayerSettings.XREnabled){
			if (Enabled){
				if (SpringArmOperation == null){
					SpringArmOperation = StartCoroutine(CalculateSpringArm());
				}
			}
			else{
				DisableSpringArm();

				transform.localPosition = FirstPersonLocation;
			}
		}
	}
}
