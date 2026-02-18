using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Save_Data{
    public Serialized_Dictionary<string, int> StaticInventory;
    public Vector3 PlayerLocation;
    public Quaternion PlayerRotation;
    public Vector3 PlayerScale;

    public Save_Data(){
        StaticInventory = new Serialized_Dictionary<string, int>();
        PlayerLocation = new Vector3(0.0f, 20.0f, 0.0f);
        PlayerRotation = Quaternion.identity;
        PlayerScale = Vector3.one;
    }
}
