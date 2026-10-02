using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Player_Settings{
    public Player_Settings(){
        LookSpeedX = 0.0f;
        LookSpeedY = 0.0f;
		XRRayEndpointInterpolation = false;
		XRRayEndpointInterpolationSpeed = 0.0f;
        XREnabled = false;
		XRRayThickness = 0.0f;
    }

    public float LookSpeedX;
    public float LookSpeedY;
	public bool XRRayEndpointInterpolation;
	public float XRRayEndpointInterpolationSpeed;
    public bool XREnabled;
	public float XRRayThickness;
}
