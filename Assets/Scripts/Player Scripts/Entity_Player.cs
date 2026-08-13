using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.InputSystem.UI;
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

	public InputSystemUIInputModule DesktopEventSystem;
	public XRUIInputModule VREventSystem;
	
	public Entity_XR_Hand ActiveHand;

    void Awake(){
        PlayerSettings = new Player_Settings();

        PlayerSettings.LookSpeedX = 0.5f;
        PlayerSettings.LookSpeedY = 0.5f;

		GetComponent<Rigidbody>().useGravity = false;
    }

    public override void Start(){
        base.Start();

		StartCoroutine(LaunchXR(0.1f));

        ItemLibraryReference = FindFirstObjectByType<Data_Loader>().ItemLibraryReference;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Instantiate(HUDReference);
	}

    void Update(){
    }

	private IEnumerator LaunchXR(float Timeout){
		yield return new WaitForSeconds(Timeout);

		VREventSystem = FindFirstObjectByType<XRUIInputModule>();
		DesktopEventSystem = FindFirstObjectByType<InputSystemUIInputModule>();

		if (XRSettings.isDeviceActive){
			VREventSystem.enabled = true;
			DesktopEventSystem.enabled = false;

			PlayerSettings.XREnabled = true;

			CameraReference.GetComponent<TrackedPoseDriver>().enabled = true;
			LeftHandAnchor.SetActive(true);
			RightHandAnchor.SetActive(true);

			Debug.Log("XR Device running");
		}
		else{
			XRGeneralSettings.Instance.Manager.DeinitializeLoader();

			VREventSystem.enabled = false;
			DesktopEventSystem.enabled = true;

			PlayerSettings.XREnabled = false;

			LeftHandAnchor.SetActive(false);
			RightHandAnchor.SetActive(false);

			Debug.Log("XR Device not detected");
		}

		SpawnPoint = FindFirstObjectByType<Spawn_Point>().gameObject;

		gameObject.transform.position = SpawnPoint.transform.position;

		GetComponent<Rigidbody>().useGravity = true;
	}

    public void LoadData(Save_Data SaveData){
        gameObject.transform.position = SaveData.PlayerLocation;
        gameObject.transform.rotation = SaveData.PlayerRotation;
        gameObject.transform.localScale = SaveData.PlayerScale;

		Debug.Log("Save Data Loaded");
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
