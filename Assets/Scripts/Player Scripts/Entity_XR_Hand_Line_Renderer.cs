using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class Entity_XR_Hand_Line_Renderer : MonoBehaviour{
	public LineRenderer HandLineRenderer;

	Player_Settings PlayerSettingsReference;
	XRRayInteractor RayInteractorReference;

	[SerializeField]
	private int SegmentCount = 20;

	[SerializeField]
	private float CurveIntensity = 0.0f;

	Vector3[] LinePoints;
	Vector3 SmoothedLineEnd;

	public Entity_XR_Hand_Line_Renderer(){
	}

	void Awake(){
		HandLineRenderer = GetComponentInChildren<LineRenderer>();
		RayInteractorReference = GetComponentInChildren<XRRayInteractor>();

		LinePoints = new Vector3[SegmentCount];

		HandLineRenderer.useWorldSpace = true;
		HandLineRenderer.positionCount = SegmentCount;
	}

	void Start(){
		PlayerSettingsReference = GetComponentInParent<Entity_Player>().PlayerSettings;
		HandLineRenderer.startWidth = PlayerSettingsReference.XRRayThickness;
		HandLineRenderer.endWidth = (PlayerSettingsReference.XRRayThickness * 0.6f);

		SmoothedLineEnd = Vector3.zero;
	}

	void Update(){
		RaycastHit InteractableHit;
		RaycastResult UserInterfaceHit;
		bool ValidInteractableHit;
		bool ValidUserInterfaceHit;

		ValidInteractableHit = Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out InteractableHit, 10.0f, 1);
		ValidUserInterfaceHit = RayInteractorReference.TryGetCurrentUIRaycastResult(out UserInterfaceHit);

		if (ValidInteractableHit && ValidUserInterfaceHit){
			if (UserInterfaceHit.distance > InteractableHit.distance){
				if (InteractableHit.collider.gameObject.GetComponentInParent<Interactable_Parent>()){
					UpdateLine(InteractableHit.point);

					SetValidColor();
				}
				else{
					UpdateLine(InteractableHit.point);

					SetInvalidColor();
				}
			}
			else{
				UpdateLine(UserInterfaceHit.worldPosition);

				SetValidColor();
			}
		}
		else if (ValidInteractableHit){
			if (InteractableHit.collider.gameObject.GetComponentInParent<Interactable_Parent>()){
				UpdateLine(InteractableHit.point);

				SetValidColor();
			}
			else{
				UpdateLine(InteractableHit.point);

				SetInvalidColor();
			}
		}
		else if (ValidUserInterfaceHit){
			UpdateLine(UserInterfaceHit.worldPosition);

			SetValidColor();
		}
		else{
			Vector3 TraceEnd = transform.position;

			TraceEnd += (transform.forward * 10.0f);

			UpdateLine(TraceEnd);

			SetInvalidColor();
		}
	}

	Vector3 QuadraticBezier(Vector3 P0, Vector3 P1, Vector3 P2, float T){
		float TCompliment = (1.0f - T);

		return TCompliment * TCompliment * P0 + 2.0f * TCompliment * T * P1 + T * T * P2;
	}

	void SetInvalidColor(){
		HandLineRenderer.startColor = new Color(1.0f, 1.0f, 1.0f, 0.05f);
		HandLineRenderer.endColor = new Color(1.0f, 1.0f, 1.0f, 0.05f);
	}

	void SetValidColor(){
		HandLineRenderer.startColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
		HandLineRenderer.endColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
	}

	void UpdateLine(Vector3 LineEnd){
		Vector3 LineStart = transform.position;
		Vector3 ForwardVector = (LineEnd - LineStart);
		Vector3 ControlPoint;

		if (Mathf.Abs((LineEnd.magnitude - SmoothedLineEnd.magnitude)) < 2.0f){
			SmoothedLineEnd = Vector3.Lerp(SmoothedLineEnd, LineEnd, (PlayerSettingsReference.SmoothXRRayEndPointMovementSpeed * (Time.deltaTime * 40.0f)));
		}
		else{
			SmoothedLineEnd = LineEnd;
		}
		
		ForwardVector.Normalize();

		if (!PlayerSettingsReference.SmoothXRRayEndPointMovement){
			ControlPoint = Vector3.Lerp(LineStart, LineEnd, 0.5f);
		}
		else{
			ControlPoint = (transform.forward * Vector3.Distance(LineStart, SmoothedLineEnd));

			ControlPoint *= 0.5f;
			ControlPoint += LineStart;

			//ControlPoint += (Vector3.Cross(ForwardVector, Vector3.up) * CurveIntensity);
		}

		for (int Index = 0; Index < SegmentCount; Index++){
			float T = ((float)Index / (float)(SegmentCount - 1));

			if (!PlayerSettingsReference.SmoothXRRayEndPointMovement){
				LinePoints[Index] = QuadraticBezier(LineStart, ControlPoint, LineEnd, T);
			}
			else{
				LinePoints[Index] = QuadraticBezier(LineStart, ControlPoint, SmoothedLineEnd, T);
			}
		}

		HandLineRenderer.SetPositions(LinePoints);
	}
}
