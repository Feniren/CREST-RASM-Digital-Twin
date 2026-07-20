using System;
using System.Collections.Generic;

using UnityEngine;

[CreateAssetMenu(menuName = "Game/Item List")]
public class Item_List : ScriptableObject{
	[SerializeField]
	private List<GameObject> Items = new List<GameObject>();

	private Dictionary<string, GameObject> ItemDictionary;

	private void OnEnable(){
		BuildDictionary();
	}

	private void BuildDictionary(){
	}
}
