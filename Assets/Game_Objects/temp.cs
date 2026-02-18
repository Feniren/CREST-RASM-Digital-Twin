using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Temp : Item_Parent{
    public override void Start(){
        base.Start();

        GetComponent<MeshRenderer>().enabled = false;
    }

    void OnCollisionEnter(Collision CollisionEvent){
        Debug.Log("Course complete");
        if (CollisionEvent.gameObject.GetComponent<Player_Controller>()){
            GetComponent<MeshRenderer>().enabled = true;
        }
    }
}