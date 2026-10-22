using UnityEngine;

public class Interactable_Parent : MonoBehaviour, Interact_Interface{
	public Interactable_Parent(){
	}

	public virtual void Interact(Entity_Player PlayerReference){
	}

	public virtual void AlternateInteract(Entity_Player PlayerReference){
	}
}
