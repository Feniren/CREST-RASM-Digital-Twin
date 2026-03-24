using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Player_Settings{
    public Player_Settings(){
        LookSpeedX = 0.0f;
        LookSpeedY = 0.0f;
        XREnabled = false;
    }

    public float LookSpeedX;
    public float LookSpeedY;
    public bool XREnabled;
}
