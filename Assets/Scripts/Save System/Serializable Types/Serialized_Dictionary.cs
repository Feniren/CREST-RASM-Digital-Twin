using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Serialized_Dictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver {
    [SerializeField]
    private List<TKey> SerializedKeys = new List<TKey>();

    [SerializeField]
    private List<TValue> SerializedValues = new List<TValue>();

    public Serialized_Dictionary(){
    }

    public Serialized_Dictionary(List<KeyValuePair<TKey, TValue>> List){
        Clear();

        for (int i = 0; i < List.Count; i++){
            Add(List[i].Key, List[i].Value);
        }
    }
    public void OnBeforeSerialize(){
        SerializedKeys.Clear();
        SerializedValues.Clear();

        foreach (KeyValuePair<TKey, TValue> Pair in this){
            SerializedKeys.Add(Pair.Key);
            SerializedValues.Add(Pair.Value);
        }
    }

    public void OnAfterDeserialize(){
        Clear();

        for (int i = 0; i < SerializedKeys.Count; i++){
            Add(SerializedKeys[i], SerializedValues[i]);
        }
    }
}
