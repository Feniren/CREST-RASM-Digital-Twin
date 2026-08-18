using UnityEngine;

public class Entity_XR_Hand_Line_Renderer : MonoBehaviour{
	public LineRenderer HandLineRenderer;

	Player_Settings PlayerSettingsReference;

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
		RaycastHit Hit;

		if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out Hit, 10.0f, 1)){
			if (Hit.collider.gameObject.GetComponentInParent<Interactable_Parent>()){
				UpdateLine(Hit.point);

				HandLineRenderer.startColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
				HandLineRenderer.endColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
			}
			else{
				UpdateLine(Hit.point);

				HandLineRenderer.startColor = new Color(1.0f, 1.0f, 1.0f, 0.05f);
				HandLineRenderer.endColor = new Color(1.0f, 1.0f, 1.0f, 0.05f);
			}
		}
		else{
			Vector3 TraceEnd = transform.position;

			TraceEnd += (transform.forward * 10.0f);

			UpdateLine(TraceEnd);

			HandLineRenderer.startColor = new Color(1.0f, 1.0f, 1.0f, 0.05f);
			HandLineRenderer.endColor = new Color(1.0f, 1.0f, 1.0f, 0.05f);
		}
	}

	Vector3 QuadraticBezier(Vector3 P0, Vector3 P1, Vector3 P2, float T){
		float TCompliment = (1.0f - T);

		return TCompliment * TCompliment * P0 + 2.0f * TCompliment * T * P1 + T * T * P2;
	}

	void UpdateLine(Vector3 LineEnd){
		Vector3 LineStart = transform.position;
		Vector3 ForwardVector = (LineEnd - LineStart);
		Vector3 ControlPoint = (LineStart + transform.forward);

		SmoothedLineEnd = Vector3.Lerp(SmoothedLineEnd, LineEnd, (PlayerSettingsReference.SmoothXRRayEndPointMovementSpeed * (Time.deltaTime * 40.0f)));
		
		ForwardVector.Normalize();

		if (!PlayerSettingsReference.SmoothXRRayEndPointMovement){
			ControlPoint *= (Vector3.Distance(LineStart, LineEnd) * 0.5f);
			ControlPoint = Vector3.Lerp(LineStart, LineEnd, 0.5f);
		}
		else{
			ControlPoint *= (Vector3.Distance(LineStart, SmoothedLineEnd) * 0.5f);
			ControlPoint = Vector3.Lerp(LineStart, SmoothedLineEnd, 0.5f);
		}

		//ControlPoint += (Vector3.Cross(ForwardVector, Vector3.up) * CurveIntensity);

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
