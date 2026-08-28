using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class Serialized_List<T> : List<T>, ISerializationCallbackReceiver{
    [SerializeField]
    private List<T> SerializedKeys = new List<T>();

    [SerializeField]
    private List<T> SerializedValues = new List<T>();

    public Serialized_List()
    {
    }

    public Serialized_List(List<KeyValuePair<string, int>> List)
    {
        Clear();

        for (int i = 0; i < List.Count; i++)
        {
            //Add(List[i].Key, List[i].Value);
        }
    }
    public void OnBeforeSerialize()
    {
        SerializedKeys.Clear();
        SerializedValues.Clear();

        //foreach (KeyValuePair<TKey, TValue> Pair in this)
        //{
            //SerializedKeys.Add(Pair.Key);
            //SerializedValues.Add(Pair.Value);
        //}
    }

    public void OnAfterDeserialize()
    {
        Clear();

        /*for (int i = 0; i < Keys.Count; i++)
        {
            Add(SerializedKeys[i], SerializedValues[i]);
        }*/
    }
}
