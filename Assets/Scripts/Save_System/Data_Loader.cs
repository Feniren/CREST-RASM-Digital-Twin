using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class Data_Loader : MonoBehaviour{
    private Save_Data SaveData;
    private List<Save_Data_Interface> SaveableObjects;
    private File_Data FileData;

    [SerializeField]
    private string FileName;

	[SerializeField]
	public Item_Library ItemLibraryReference;

    public Data_Loader Instance{get; private set;}

    private void Awake(){
        if (Instance != null && Instance != this){
            Debug.LogError("Save Data Loader duplicate detected");

			Destroy(gameObject);

			return;
        }

        Instance = this;

		DontDestroyOnLoad(gameObject);
    }

    private void Start(){
        FileData = new File_Data(Application.persistentDataPath, FileName);
        SaveableObjects = FindSaveableObjects();

        Debug.Log(SaveableObjects.Count);

        LoadGame();
    }

    public void NewGame(){
        SaveData = new Save_Data();
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

    public void SaveGame(){
        foreach (Save_Data_Interface SaveableObject in SaveableObjects){
            SaveableObject.SaveData(ref SaveData);
        }

        FileData.Save(SaveData);
    }

    private void OnApplicationQuit(){
        SaveGame();
    }

	private void OnDisable(){
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	private void OnEnable(){
		SceneManager.sceneLoaded += OnSceneLoaded;
	}
	
	private void OnSceneLoaded(Scene scene, LoadSceneMode mode){
		/*GameObject PlayerReference = FindFirstObjectByType<Entity_Player>().gameObject;

		PlayerReference.transform.position += new Vector3(0.0f, 10.0f, 0.0f);

		Debug.Log("New Scene loaded. Player position adjusted");*/
	}

	private List<Save_Data_Interface> FindSaveableObjects(){
        IEnumerable<Save_Data_Interface> SaveableObjects = FindObjectsOfType<MonoBehaviour>().OfType<Save_Data_Interface>();

        return new List<Save_Data_Interface>(SaveableObjects);
    }
}
