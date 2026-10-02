using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Save_Data{
    public Vector3 PlayerLocation;
    public Quaternion PlayerRotation;
    public Vector3 PlayerScale;
	public Player_Settings PlayerSettings;
	public Serialized_Dictionary<string, int> StaticInventory;

    public Save_Data(){
        PlayerLocation = new Vector3(0.0f, 5.0f, 0.0f);
        PlayerRotation = Quaternion.identity;
        PlayerScale = Vector3.one;
		PlayerSettings = new Player_Settings();
		StaticInventory = new Serialized_Dictionary<string, int>();

		PlayerSettings.LookSpeedX = 0.5f;
		PlayerSettings.LookSpeedY = 0.5f;
		PlayerSettings.XRRayEndpointInterpolation = true;
		PlayerSettings.XRRayEndpointInterpolationSpeed = 0.2f;
		PlayerSettings.XRRayThickness = 0.05f;
    }
}
