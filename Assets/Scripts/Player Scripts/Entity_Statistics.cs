using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Entity_Statistics{
    public Entity EntityReference;

    public Entity_Statistics(){
        ExperiencePoints = 0;
        HealthCurrent = 0.0f;
        HealthMax = 0.0f;
        HealthNormalized = 0.0f;
        JumpCurrent = 0;
        JumpMax = 0;
        Level = 0;
        MovementSpeed = 0.0f;
        PhysicalDamage = 0.0f;
        PhysicalResistance = 0.0f;
        SpellDamage = 0.0f;
        SpellResistance = 0.0f;
    }

    public int ExperiencePoints;
    public float HealthCurrent;
    public float HealthMax;
    public float HealthNormalized;
    public int JumpCurrent;
    public int JumpMax;
    public int Level;
    public float MovementSpeed;
    public float PhysicalDamage;
    public float PhysicalResistance;
    public float SpellDamage;
    public float SpellResistance;

    public void DamageHealth(float FinalDamage){
        HealthCurrent -= FinalDamage;

        Mathf.Clamp(HealthCurrent, 0.0f, HealthMax);

        HealthNormalized = (HealthCurrent / HealthMax);

        if (HealthCurrent <= 0.0f){
            EntityReference.gameObject.transform.position = EntityReference.RespawnPoint.transform.position;

            HealthCurrent = HealthMax;

            HealthNormalized = 1.0f;

            Debug.Log("You died");
        }
    }
}
