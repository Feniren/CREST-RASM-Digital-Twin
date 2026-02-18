using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity : MonoBehaviour{
    public Entity_Statistics EntityStatistics;
    public TextAsset EntityStatSheet;
    public Inventory InventoryReference;

    public GameObject RespawnPoint;

    public virtual void Start(){
        EntityStatistics = new Entity_Statistics();
        InventoryReference = GetComponent<Inventory>();

        EntityStatistics.EntityReference = this;

        CSV csv = new CSV(EntityStatSheet);

        csv.ReadEntityStats(this);

        EntityStatistics.HealthCurrent = EntityStatistics.HealthMax;
        EntityStatistics.HealthNormalized = 1.0f;
        EntityStatistics.MovementSpeed = 15.0f;
    }

    void OnCollisionEnter(Collision CollisionEvent){
        EntityStatistics.JumpCurrent = 0;

        if (CollisionEvent.collider.gameObject.GetComponent<Item_Projectile>()){
            if (CollisionEvent.collider.gameObject.GetComponent<Item_Projectile>().Owner != gameObject){
                Item_Projectile Projectile = CollisionEvent.collider.gameObject.GetComponent<Item_Projectile>();

                Damage_Event DamageEvent = new Damage_Event();

                DamageEvent.BaseDamage = Projectile.BaseDamage;
                DamageEvent.DamageCategory = Projectile.DamageCategory;
                DamageEvent.DamageCauser = CollisionEvent.collider.gameObject;
                DamageEvent.DamagedEntity = gameObject;
                DamageEvent.DamageRatio = 1.0f;
                DamageEvent.DamageTypeClass = Projectile.DamageTypeClass;

                TakeDamage(DamageEvent);

                Destroy(CollisionEvent.collider.gameObject);
            }
        }
    }

    public virtual void LevelUp(){
        EntityStatistics.Level++;

        CSV csv = new CSV(EntityStatSheet);

        csv.ReadEntityStats(this);

        Debug.Log(gameObject.name + " leveled up to level " + EntityStatistics.Level);
    }

    public virtual void TakeDamage(Damage_Event DamageEvent){
        float FinalDamage;

        FinalDamage = DamageEvent.ApplyDamage();

        EntityStatistics.DamageHealth(FinalDamage);

        Debug.Log(DamageEvent.DamageCauser.name + " hit " + gameObject.name + " for " + FinalDamage + " damage. " + gameObject.name + " has " + EntityStatistics.HealthCurrent + " health remaining.");
    }
}
