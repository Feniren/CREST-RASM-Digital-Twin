using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class Item_Parent : MonoBehaviour, Interact_Interface{
	[ReadOnly]
	public string Name;

	public bool AutomaticActivation;
    public GameObject Owner;
    public bool Pickup;
    public int Quantity;
	
	public UnityEvent OnGrabbed;

    [SerializeField]
    private string ID;

    [ContextMenu("Generate ID")]
    private void GenerateGUID(){
        ID = System.Guid.NewGuid().ToString();
    }

    public virtual void Start(){
        if (AutomaticActivation){
            ActivateEffect();
        }
    }

    public Item_Parent(){
        AutomaticActivation = false;
        Name = "";
        Pickup = false;
        Quantity = 0;
    }

    public virtual void ActivateEffect(){
        //Debug.Log("Activated");
    }

    public virtual void Interact(Entity_Player PlayerReference){
        if (Pickup){
            PlayerReference.InventoryReference.AddToInventory(Name, Quantity);

            Destroy(gameObject);
        }
    }

    public virtual void AlternateInteract(Entity_Player PlayerReference){
    }
}
