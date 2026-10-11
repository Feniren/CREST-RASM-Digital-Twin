using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Management;

public class Entity_Player : Entity, Save_Data_Interface{
    public GameObject HUDReference;
	public GameObject ItemAnchor;
    public GameObject LeftHandAnchor;
    public GameObject RightHandAnchor;

	[ReadOnly]
	public Item_Library ItemLibraryReference;

	[ReadOnly]
	public Player_Settings PlayerSettings;

    public Camera CameraReference;
    public Health_Bar HealthBarReference;

	public static GameObject Instance;

	static Entity_Player PlayerReference;

	Player_Controller ControllerReference;
	Data_Loader DataLoader;
	XRManagerSettings XRManager;

	public InputSystemUIInputModule DesktopEventSystem;
	public XRUIInputModule VREventSystem;
	
	public Entity_XR_Hand ActiveHand;

    void Awake(){
		SetLocalPlayer();

		GetComponent<Rigidbody>().useGravity = false;
    }

    public override void Start(){
        base.Start();

		ControllerReference = GetComponent<Player_Controller>();
		DataLoader = Data_Loader.GetDataLoader();
		ItemLibraryReference = DataLoader.ItemLibrary;
		XRManager = DataLoader.XRManager;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Instantiate(HUDReference);

		ConnectXR();
	}

    void Update(){
    }

	public void LoadData(Save_Data SaveData){
        gameObject.transform.position = SaveData.PlayerLocation;
        gameObject.transform.rotation = SaveData.PlayerRotation;
        gameObject.transform.localScale = SaveData.PlayerScale;
		PlayerSettings = SaveData.PlayerSettings;

		Debug.Log("Save Data Loaded");
    }

    public void SaveData(ref Save_Data SaveData){
        SaveData.PlayerLocation = gameObject.transform.position;
        SaveData.PlayerRotation = gameObject.transform.rotation;
        SaveData.PlayerScale = gameObject.transform.localScale;
		SaveData.PlayerSettings = PlayerSettings;
    }

	void ConnectXR(){
		VREventSystem = FindFirstObjectByType<XRUIInputModule>();
		DesktopEventSystem = FindFirstObjectByType<InputSystemUIInputModule>();

		if (XRManager.activeLoader){
			if (XRSettings.isDeviceActive){
				VREventSystem.enabled = true;
				DesktopEventSystem.enabled = false;

				PlayerSettings.XREnabled = true;

				CameraReference.GetComponent<TrackedPoseDriver>().enabled = true;
				LeftHandAnchor.SetActive(true);
				RightHandAnchor.SetActive(true);

				Debug.Log("XR Device running");
			}
		}
		else{
			VREventSystem.enabled = false;
			DesktopEventSystem.enabled = true;

			PlayerSettings.XREnabled = false;

			LeftHandAnchor.SetActive(false);
			RightHandAnchor.SetActive(false);

			Debug.Log("XR Device not detected");
		}

		Spawn();

		ControllerReference.EnableInput();

		GetComponent<Rigidbody>().useGravity = true;
	}

	public static Entity_Player GetLocalPlayer(){
		return PlayerReference;
	}

	void SetLocalPlayer(){
		Instance = gameObject;
		PlayerReference = this;
	}

	void Spawn(){
		SpawnPoint = FindFirstObjectByType<Spawn_Point>().gameObject;

		gameObject.transform.position = SpawnPoint.transform.position;
	}

    public override void TakeDamage(Damage_Event DamageEvent){
        base.TakeDamage(DamageEvent);

        HealthBarReference.SetPercent(EntityStatistics.HealthNormalized);
    }
}
