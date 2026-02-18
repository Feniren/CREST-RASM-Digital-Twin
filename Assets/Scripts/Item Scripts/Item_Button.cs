using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item_Button : Item_Parent{
    public List<GameObject> LinkedObjects;

    void OnCollisionEnter(Collision CollisionEvent){
        if (CollisionEvent.collider.gameObject.GetComponent<Item_Projectile>()){
            if (LinkedObjects.Count > 0){
                foreach (GameObject LinkedObject in LinkedObjects){
                    if (LinkedObject.GetComponent<Item_Parent>()){
                        LinkedObject.GetComponent<Item_Parent>().ActivateEffect();
                    }
                }
            }
        }
    }
}
