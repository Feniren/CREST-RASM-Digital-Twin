using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class File_Data{
    private string DirectoryPath;
    private string FileName;

    public File_Data(string DirectoryPath, string FileName){
        this.DirectoryPath = DirectoryPath;
        this.FileName = FileName;
    }

    public Save_Data Load(){
        string FullPath = Path.Combine(DirectoryPath, FileName);

        Save_Data LoadedData = null;

        if (File.Exists(FullPath)){
            try{
                string StoredData;

                using (FileStream Stream = new FileStream(FullPath, FileMode.Open)){
                    using (StreamReader Reader = new StreamReader(Stream)){
                        StoredData = Reader.ReadToEnd();
                    }
                }

                LoadedData = JsonUtility.FromJson<Save_Data>(StoredData);
            }
            catch (Exception e){
                Debug.LogError(e + ". Failed to load from file");
            }
        }

        return LoadedData;
    }

    public void Save(Save_Data SaveData){
        string FullPath = Path.Combine(DirectoryPath, FileName);

        try{
            Directory.CreateDirectory(Path.GetDirectoryName(FullPath));

            string StoredData = JsonUtility.ToJson(SaveData, true);

            using (FileStream Stream = new FileStream(FullPath, FileMode.Create)){
                using (StreamWriter Writer = new StreamWriter(Stream)){
                    Writer.Write(StoredData);
                }
            }
        }
        catch (Exception e){
            Debug.LogError(e + ". Failed to save to file");
        }
    }
}
