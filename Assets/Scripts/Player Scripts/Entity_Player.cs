using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity_Player : Entity, Save_Data_Interface{
    public Camera CameraReference;
    public Health_Bar HealthBarReference;
    public Item_Library ItemLibraryReference;
    public Player_Settings PlayerSettings;

    void Awake(){
        PlayerSettings = new Player_Settings();

        PlayerSettings.LookSpeedX = 0.5f;
        PlayerSettings.LookSpeedY = 0.5f;
    }

    public override void Start(){
        base.Start();

        ItemLibraryReference = GetComponent<Item_Library>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update(){
        //Quaternion Rotation = Quaternion.Euler(0.0f, CameraReference.transform.localEulerAngles.y, 0.0f);

        //transform.rotation = Rotation;
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

    public bool GetItemEquipped(){return false;
    }

    public void SetItemEquipped(bool Equipped){
    }

    public void ToggleEquippedItem(){
        if (true){
            if (InventoryReference.InstancedInventory.Count != 0){
                GameObject ItemReference = InventoryReference.InstancedInventory[0];

                ItemReference.SetActive(true);
                ItemReference.transform.rotation = Quaternion.identity;

                Debug.Log("Item should be spawned");

                InventoryReference.RemoveFromInventory(ItemReference);
            }
            else{
                Debug.Log("No Items in inventory");
            }
        }
    }

    public override void TakeDamage(Damage_Event DamageEvent){
        base.TakeDamage(DamageEvent);

        HealthBarReference.SetPercent(EntityStatistics.HealthNormalized);
    }
}
