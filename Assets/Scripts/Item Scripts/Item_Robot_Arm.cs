using Unity.VisualScripting;
using UnityEngine;

public class Item_Robot_Arm : Item_Parent{
    Animator AnimatorReference;

    public Item_Robot_Arm(){
        Name = "Robot Arm";
        Pickup = false;
        Quantity = 1;
    }

    public override void Start(){
        base.Start();

        AnimatorReference = GetComponent<Animator>();

        AnimatorReference.Play("PickUpItem");
    }

    public override void AlternateInteract(Entity_Player PlayerReference){
    }
}
