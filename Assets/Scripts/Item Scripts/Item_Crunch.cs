using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item_Crunch : Item_Parent{
    public AudioSource Audio;
    Item_Crunch(){
        Name = "Crunch";
        Pickup = true;
        Quantity = 1;
    }

    public override void Start(){
        base.Start();

        Audio = GetComponent<AudioSource>();
    }

    public override void AlternateInteract(Entity_Player PlayerReference){
        base.AlternateInteract(PlayerReference);

        Audio.Play(0);

        Debug.Log("Crunch");
    }
}
