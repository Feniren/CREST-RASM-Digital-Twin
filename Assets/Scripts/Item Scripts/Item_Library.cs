using System;
using System.Collections.Generic;

using UnityEngine;

[CreateAssetMenu(menuName = "Item/Item Library")]
public class Item_Library : ScriptableObject{
	[SerializeField]
	private List<GameObject> Items = new List<GameObject>();

	private Dictionary<string, GameObject> ItemDictionary;

	private void OnEnable(){
		BuildDictionary();
	}

	private void BuildDictionary(){
		ItemDictionary = new Dictionary<string, GameObject>();

		for (int Index = 0; Index < Items.Count; Index++){
			Item_Parent ItemReference = Items[Index].GetComponent<Item_Parent>();

			ItemDictionary.Add(ItemReference.Name, Items[Index]);
		}
	}

	public GameObject GetItemFromName(string Name){
		if (ItemDictionary == null){
			BuildDictionary();
		}

		if (ItemDictionary.TryGetValue(Name, out GameObject ItemPrefab)){
			return ItemPrefab;
		}
		else{
			Debug.LogError(Name + " not found as an Item");

			return null;
		}
	}
}
