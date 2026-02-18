using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Damage_Event{

    public Damage_Event(){
        BaseDamage = 0.0f;
        DamageCategory = Damage_Category.Physical;
        DamageCauser = null;
        DamagedEntity = null;
        DamageRatio = 0.0f;
        DamageTypeClass = null;
    }

    public float BaseDamage;
    public Damage_Category DamageCategory;
    public GameObject DamageCauser;
    public GameObject DamagedEntity;
    public float DamageRatio;
    public Damage_Type DamageTypeClass;

    public virtual float ApplyDamage(){
        float FinalDamage;

        if (DamageTypeClass != null){
            FinalDamage = DamageTypeClass.ApplyDamage(this);
        }
        else{
            FinalDamage = 0.0f;
        }

        return FinalDamage;
    }
}
