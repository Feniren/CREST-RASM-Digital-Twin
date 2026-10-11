using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;
using System.Linq;

public class Data_Loader : MonoBehaviour{
    private Save_Data SaveData;
    private List<Save_Data_Interface> SaveableObjects;
    private File_Data FileData;

    [SerializeField]
    private string FileName;

	[SerializeField]
	public Item_Library ItemLibrary;

	[SerializeField]
	public Widget_Library WidgetLibrary;

	[ReadOnly]
	public XRManagerSettings XRManager;

	public static GameObject Instance;

	static Data_Loader DataLoaderReference;

    private void Awake(){
        if (Instance != null && Instance != this){
            Debug.LogWarning("Data Loader duplicate detected");

			Destroy(gameObject);

			return;
        }

		Instance = gameObject;
		DataLoaderReference = this;

		DontDestroyOnLoad(Instance);

		StartCoroutine(LaunchXR());
    }

    private void OnApplicationQuit(){
        SaveGame();

		if (XRManager.isInitializationComplete){
			XRManager.StopSubsystems();
			XRManager.DeinitializeLoader();
		}
    }

	private void OnDisable(){
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	private void OnEnable(){
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void Start(){
    }
	
	private void OnSceneLoaded(Scene scene, LoadSceneMode mode){
		FileData = new File_Data(Application.persistentDataPath, FileName);
        SaveableObjects = FindSaveableObjects();

        Debug.Log("Saveable objects found: " + SaveableObjects.Count);

        LoadGame();
	}

	private List<Save_Data_Interface> FindSaveableObjects(){
        IEnumerable<Save_Data_Interface> SaveableObjects = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<Save_Data_Interface>();

        return new List<Save_Data_Interface>(SaveableObjects);
    }

	public static Data_Loader GetDataLoader(){
		return DataLoaderReference;
	}

	IEnumerator LaunchXR(){
		XRManager = XRGeneralSettings.Instance.Manager;

		yield return XRManager.InitializeLoader();
	}

	public void LoadGame(){
        SaveData = FileData.Load();

        if (SaveData == null){
            NewGame();
        }

        foreach (Save_Data_Interface SaveableObject in SaveableObjects){
            SaveableObject.LoadData(SaveData);
        }
    }

	public void NewGame(){
        SaveData = new Save_Data();
    }

    public void SaveGame(){
        foreach (Save_Data_Interface SaveableObject in SaveableObjects){
            SaveableObject.SaveData(ref SaveData);
        }

        FileData.Save(SaveData);
    }
}
