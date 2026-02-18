using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item_Projectile : Item_Parent{
    public Rigidbody RigidBodyReference;

    public float BaseDamage;
    public Damage_Category DamageCategory;
    public Damage_Type DamageTypeClass;

    public override void Start(){
        base.Start();

        BaseDamage = 10.0f;
        DamageCategory = Damage_Category.Physical;
        DamageTypeClass = new Damage_Type();

        Destroy(gameObject, 60.0f);
    }
}
