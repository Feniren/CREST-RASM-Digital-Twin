using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item_Appear : Item_Parent{
    public bool Toggle = false;

    public override void Start(){
        base.Start();

        gameObject.SetActive(false);
    }

    public override void ActivateEffect(){
        if (Toggle){
            gameObject.SetActive(!gameObject.activeSelf);
        }
        else{
            gameObject.SetActive(true);
        }
    }
}
