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

    List<XRDisplaySubsystem> XRList = new List<XRDisplaySubsystem>();

    void Awake(){
        PlayerSettings = new Player_Settings();

        PlayerSettings.LookSpeedX = 0.5f;
        PlayerSettings.LookSpeedY = 0.5f;

        PlayerSettings.XREnabled = true;
    }

    public override void Start(){
        base.Start();

        SubsystemManager.GetSubsystems<XRDisplaySubsystem>(XRList);

        for (int i = 0; i < XRList.Count; i++){
            if (XRList[i].running){
                Debug.Log("XR Device running");

                PlayerSettings.XREnabled = false;

                CameraReference.GetComponent<TrackedPoseDriver>().enabled = true;
                LeftHandAnchor.SetActive(true);
                RightHandAnchor.SetActive(true);
            }
        }

        PlayerSettings.XREnabled = true;

        if (!PlayerSettings.XREnabled){
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        }

        ItemLibraryReference = GetComponent<Item_Library>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Instantiate(HUDReference);
    }

    void Update(){
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
