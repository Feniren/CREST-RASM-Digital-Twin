using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item_Cannon : Item_Parent{
    public GameObject ProjectilePrefab;
    public GameObject SpawnPoint;

    public float Cooldown;
    public float ProjectileForce;

    public override void Start(){
        base.Start();

        StartCoroutine(ShootCooldown(Cooldown));
    }

    void Shoot(float ProjectileForce){
        GameObject ProjectileReference;

        ProjectileReference = Instantiate(ProjectilePrefab, SpawnPoint.transform.position, Quaternion.identity);

        ProjectileReference.GetComponent<Item_Projectile>().Owner = gameObject;

        ProjectileReference.GetComponent<Item_Projectile>().RigidBodyReference.AddForce((SpawnPoint.transform.forward * ProjectileForce), ForceMode.Impulse);

        StartCoroutine(ShootCooldown(Cooldown));
    }

    IEnumerator ShootCooldown(float Cooldown){
        yield return new WaitForSeconds(Cooldown);

        Shoot(ProjectileForce);
    }
}
