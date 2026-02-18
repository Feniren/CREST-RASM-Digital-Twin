using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Item_Parent : MonoBehaviour{
    public bool AutomaticActivation;
    public GameObject Owner;
    public bool Pickup;
    public int Quantity;
    public string Name;

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
