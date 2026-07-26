using UnityEngine;
using UnityEngine.Events;

public class Interactable_Select : Interactable_Parent{
	public UnityEvent OnInteractBegin;

	public Interactable_Select(){
	}

	public override void Interact(Entity_Player PlayerReference){
		base.Interact(PlayerReference);

		OnInteractBegin.Invoke();
	}

	public override void AlternateInteract(Entity_Player PlayerReference){
		base.Interact(PlayerReference);
	}
    
}
