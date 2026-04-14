using UnityEngine;
using System.Collections.Generic;

public class Item_CIM_Conveyor_Control_Box : Item_Parent{
    public GameObject ConveyorBeltReference;

    bool Active = true;
    float BaseSpeed = 0.325f;

    public Item_CIM_Conveyor_Control_Box(){
        Name = "CIM Conveyor Control Box";
        Pickup = false;
        Quantity = 1;
    }

    public override void Start(){
        base.Start();
    }

    public override void Interact(Entity_Player PlayerReference){
        base.Interact(PlayerReference);
    }

    public override void AlternateInteract(Entity_Player PlayerReference){
        ConveyorBeltReference.GetComponent<Item_Conveyor_Belt>().ToggleMovement();
    }
}
