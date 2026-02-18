using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item_Projectile_Spell : Item_Projectile{
    public override void Start(){
        base.Start();

        BaseDamage = 10.0f;
        DamageCategory = Damage_Category.Spell;
    }
}
