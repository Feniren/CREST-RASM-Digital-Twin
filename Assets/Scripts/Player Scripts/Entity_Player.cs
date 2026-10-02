using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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

	[ReadOnly]
	public Item_Library ItemLibraryReference;

    public Camera CameraReference;
    public Health_Bar HealthBarReference;
	public Player_Settings PlayerSettings;

	public static GameObject Instance;

	static Entity_Player PlayerReference;

	Player_Controller ControllerReference;
	Data_Loader DataLoader;

	public InputSystemUIInputModule DesktopEventSystem;
	public XRUIInputModule VREventSystem;
	
	public Entity_XR_Hand ActiveHand;

    void Awake(){
		SetLocalPlayer();

		GetComponent<Rigidbody>().useGravity = false;
    }

	public void OnDestroy(){
		if (Instance == gameObject){
			Instance = null;
		}
	}

	public void OnDisable(){
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	public void OnEnable(){
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

    public override void Start(){
        base.Start();

		ControllerReference = GetComponent<Player_Controller>();
		DataLoader = FindFirstObjectByType<Data_Loader>();
		ItemLibraryReference = DataLoader.ItemLibrary;

		StartCoroutine(LaunchXR(0.1f));

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Instantiate(HUDReference);
	}

    void Update(){
    }

	public void OnSceneLoaded(Scene scene, LoadSceneMode mode){
		if (Instance == null){
			SetLocalPlayer();
		}
	}

	public static Entity_Player GetLocalPlayer(){
		return PlayerReference;
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

		ControllerReference.EnableInput();

		GetComponent<Rigidbody>().useGravity = true;
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

	void SetLocalPlayer(){
		if ((Instance != null) && (Instance != gameObject)){
			Debug.LogWarning("Reference not set to local player");

			return;
		}

		Instance = gameObject;
		PlayerReference = this;
	}

    public override void TakeDamage(Damage_Event DamageEvent){
        base.TakeDamage(DamageEvent);

        HealthBarReference.SetPercent(EntityStatistics.HealthNormalized);
    }
}
