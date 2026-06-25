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

		float RandomOffset = Random.Range(0.0f, 1.0f);

        AnimatorReference = GetComponent<Animator>();

		AnimatorReference.SetFloat("StartOffset", RandomOffset);

        //AnimatorReference.Play("PickUpItem");
    }

    public override void AlternateInteract(Entity_Player PlayerReference){
    }
}
