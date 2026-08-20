using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Player_Settings{
    public Player_Settings(){
        LookSpeedX = 0.0f;
        LookSpeedY = 0.0f;
		SmoothXRRayEndPointMovement = false;
		SmoothXRRayEndPointMovementSpeed = 0.0f;
        XREnabled = false;
		XRRayThickness = 0.0f;
    }

    public float LookSpeedX;
    public float LookSpeedY;
	public bool SmoothXRRayEndPointMovement;
	public float SmoothXRRayEndPointMovementSpeed;
    public bool XREnabled;
	public float XRRayThickness;
}
