using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Data_Loader : MonoBehaviour{
    private Save_Data SaveData;
    private List<Save_Data_Interface> SaveableObjects;
    private File_Data FileData;

    [SerializeField]
    private string FileName;

    public Data_Loader instance{get; private set;}

    private void Awake(){
        if (instance != null){
            Debug.LogError("Save Data Loader duplicate detected");
        }

        instance = this;
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

    private List<Save_Data_Interface> FindSaveableObjects(){
        IEnumerable<Save_Data_Interface> SaveableObjects = FindObjectsOfType<MonoBehaviour>().OfType<Save_Data_Interface>();

        return new List<Save_Data_Interface>(SaveableObjects);
    }
}
