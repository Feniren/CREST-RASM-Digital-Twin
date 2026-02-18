using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Item_Library : MonoBehaviour{
    [SerializeField]
    public List<GameObject> ItemLibrary = new List<GameObject>();

    public GameObject Find(string Item){
        for (int i = 0; i < ItemLibrary.Count; i++){
            if (ItemLibrary[i].GetComponent<Item_Parent>().Name.Equals(Item)){
                return ItemLibrary[i];
            }
        }

        return null;
    }
}
