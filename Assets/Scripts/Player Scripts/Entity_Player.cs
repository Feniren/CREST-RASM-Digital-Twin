using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.InputSystem.XR;

public class Entity_Player : Entity, Save_Data_Interface{
    public GameObject HUDReference;
	public GameObject ItemAnchor;
    public GameObject LeftHandAnchor;
    public GameObject RightHandAnchor;

    public Camera CameraReference;
    public Health_Bar HealthBarReference;
    public Item_Library ItemLibraryReference;
    public Player_Settings PlayerSettings;
	
	public Entity_XR_Hand ActiveHand;

    List<InputDevice> XRList = new List<InputDevice>();

    void Awake(){
        PlayerSettings = new Player_Settings();

        PlayerSettings.LookSpeedX = 0.5f;
        PlayerSettings.LookSpeedY = 0.5f;
    }

    public override void Start(){
        base.Start();

		StartCoroutine(LaunchXR(0.5f));

        ItemLibraryReference = GetComponent<Item_Library>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Instantiate(HUDReference);
    }

    void Update(){
    }

	private IEnumerator LaunchXR(float Timeout){
		yield return new WaitForSeconds(Timeout);

		if (XRSettings.isDeviceActive){
			Debug.Log("XR Device running");

			PlayerSettings.XREnabled = true;

			CameraReference.GetComponent<TrackedPoseDriver>().enabled = true;
			LeftHandAnchor.SetActive(true);
			RightHandAnchor.SetActive(true);
		}
		else{
			XRGeneralSettings.Instance.Manager.DeinitializeLoader();

			PlayerSettings.XREnabled = false;

			LeftHandAnchor.SetActive(false);
			RightHandAnchor.SetActive(false);

			Debug.Log("XR Device not detected");
		}
	}

    public void LoadData(Save_Data SaveData){
        gameObject.transform.position = SaveData.PlayerLocation;
        gameObject.transform.rotation = SaveData.PlayerRotation;
        gameObject.transform.localScale = SaveData.PlayerScale;
    }

    public void SaveData(ref Save_Data SaveData){
        SaveData.PlayerLocation = gameObject.transform.position;
        SaveData.PlayerRotation = gameObject.transform.rotation;
        SaveData.PlayerScale = gameObject.transform.localScale;
    }

    public override void TakeDamage(Damage_Event DamageEvent){
        base.TakeDamage(DamageEvent);

        HealthBarReference.SetPercent(EntityStatistics.HealthNormalized);
    }
}
