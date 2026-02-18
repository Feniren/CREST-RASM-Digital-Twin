using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Object_Destroyer : MonoBehaviour{
    void OnCollisionEnter(Collision CollisionEvent){

        if (CollisionEvent.collider.gameObject.GetComponent<Entity>()){
            Damage_Event DamageEvent = new Damage_Event();

            DamageEvent.BaseDamage = 1000000.0f;
            DamageEvent.DamageCategory = Damage_Category.Physical;
            DamageEvent.DamageCauser = gameObject;
            DamageEvent.DamagedEntity = CollisionEvent.collider.gameObject;
            DamageEvent.DamageRatio = 1.0f;
            DamageEvent.DamageTypeClass = new Damage_Type();

            CollisionEvent.collider.gameObject.GetComponent<Entity>().TakeDamage(DamageEvent);
        }
        else{
            Destroy(CollisionEvent.collider.gameObject);
        }
    }
}
