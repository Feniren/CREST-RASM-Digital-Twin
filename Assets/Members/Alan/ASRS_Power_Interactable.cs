using UnityEngine;

// Power on/off toggle for the ASRS Controller-USB's power switch. Same
// pattern as DEMO_Interactable (Assets/Scripts/Interact System/) — an
// Interactable_Select on the same GameObject fires OnInteractBegin, which
// swaps the MeshRenderer's material — just semantically on/off instead of a
// color demo, and with the state readable by other scripts.
public class ASRS_Power_Interactable : MonoBehaviour{
	public bool IsPoweredOn { get; private set; }

	public Material PoweredOnMaterial;
	public Material PoweredOffMaterial;

	[Header("Lesson (optional)")]
	[Tooltip("Leave empty to use this switch standalone with no lesson gating.")]
	public SequenceManager Sequence_Manager;
	public string PowerOnActionId = "controller_power_on";

	MeshRenderer MeshRendererReference;

	public void Start(){
		MeshRendererReference = GetComponent<MeshRenderer>();

		GetComponent<Interactable_Select>().OnInteractBegin.AddListener(TogglePower);
	}

	public void TogglePower(){
		IsPoweredOn = !IsPoweredOn;

		MeshRendererReference.material = IsPoweredOn ? PoweredOnMaterial : PoweredOffMaterial;

		if (IsPoweredOn && Sequence_Manager != null)
			Sequence_Manager.NotifyAction(PowerOnActionId);
	}
}
