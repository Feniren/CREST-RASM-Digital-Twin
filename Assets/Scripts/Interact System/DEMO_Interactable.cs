using UnityEngine;

public class DEMO_Interactable : MonoBehaviour{
	bool Activated;

	public Material BlueGlass;
	public Material RedMaterial;

	MeshRenderer MeshRendererReference;

	public DEMO_Interactable(){
		Activated = false;
	}

	public void Start(){
		MeshRendererReference = GetComponent<MeshRenderer>();

		GetComponent<Interactable_Select>().OnInteractBegin.AddListener(ToggleColor);
	}

	public void ToggleColor(){
		if (Activated){
			MeshRendererReference.material = BlueGlass;
		}
		else{
			MeshRendererReference.material = RedMaterial;
		}

		Activated = !Activated;
	}


}
