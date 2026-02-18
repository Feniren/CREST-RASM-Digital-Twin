using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item_Rotating_Platform : Item_Parent{
    public bool Active;
    public int PlatformCount;
    public List<GameObject> PlatformList = new List<GameObject>();
    public GameObject PlatformPrefab;
    public float RotationRadius;
    public float RotationSpeed;
    public float Theta;

    public Item_Rotating_Platform(){
        PlatformCount = 1;
        RotationRadius = 10.0f;
        Theta = 0.0f;
    }

    public override void ActivateEffect(){
        base.ActivateEffect();

        Active = true;

        for (int Index = 0; Index < PlatformCount; Index++){
            PlatformList.Add(Instantiate(PlatformPrefab, (transform.position + new Vector3((RotationRadius * (Mathf.Cos((((Index - 1) * ((2.0f * Mathf.PI) / PlatformCount)) + (Mathf.PI / 2.0f))))), (RotationRadius * (Mathf.Sin((((Index - 1) * ((2.0f * Mathf.PI) / PlatformCount)) + (Mathf.PI / 2.0f))))), 0.0f)), Quaternion.identity));
        }

        Rotate();
    }

    void Rotate(){
        Theta += 0.01f;
        Theta %= (2.0f * Mathf.PI);

        for (int Index = 0; Index < PlatformList.Count; Index++){
            PlatformList[Index].transform.position = Vector3.Lerp(transform.position, (transform.position + new Vector3((RotationRadius * (Mathf.Cos((((Index - 1) * ((2.0f * Mathf.PI) / PlatformCount)) + (Mathf.PI / 2.0f) + Theta)))), (RotationRadius * (Mathf.Sin((((Index - 1) * ((2.0f * Mathf.PI) / PlatformCount)) + (Mathf.PI / 2.0f) + Theta)))), 0.0f)), 0.5f);
        }

        StartCoroutine("SetRotate");
    }

    IEnumerator SetRotate(){
        if (Active){
            yield return new WaitForSeconds((1.0f / RotationSpeed));

            Rotate();
        }
    }
}
