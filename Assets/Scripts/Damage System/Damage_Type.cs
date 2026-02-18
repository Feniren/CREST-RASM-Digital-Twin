using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Damage_Type{
    public virtual float ApplyDamage(Damage_Event DamageEvent){
        Entity DamageCauser = null;
        Entity DamagedEntity = null;
        float FinalDamage = 0.0f;
        float MitigatedDamage = 0.0f;

        if (DamageEvent.DamageCauser){
            DamageCauser = DamageEvent.DamageCauser.GetComponent<Entity>();
        }

        if (DamageEvent.DamagedEntity){
            DamagedEntity = DamageEvent.DamagedEntity.GetComponent<Entity>();
        }

        switch (DamageEvent.DamageCategory){
            case Damage_Category.Physical:
                if (DamagedEntity){
                    MitigatedDamage = (DamagedEntity.EntityStatistics.PhysicalResistance / 2.0f);
                }

                if (DamageCauser){
                    FinalDamage = DamageCauser.EntityStatistics.PhysicalDamage;
                }

                break;

            case Damage_Category.Spell:
                if (DamagedEntity){
                    MitigatedDamage = (DamagedEntity.EntityStatistics.SpellResistance / 2.0f);
                }

                if (DamageCauser){
                    FinalDamage = DamageCauser.EntityStatistics.SpellDamage;
                }

                break;
        }

        FinalDamage += DamageEvent.BaseDamage;
        FinalDamage *= DamageEvent.DamageRatio;
        FinalDamage -= MitigatedDamage;

        FinalDamage = Mathf.FloorToInt(FinalDamage);

        if (FinalDamage < 1.0f){
            FinalDamage = 1.0f;
        }

        return FinalDamage;
    }
}
