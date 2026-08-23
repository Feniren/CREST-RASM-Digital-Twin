using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface Save_Data_Interface{
    void LoadData(Save_Data SaveData);

    void SaveData(ref Save_Data SaveData);
}
