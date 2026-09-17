using System;
using System.Collections.Generic;

using UnityEngine;

[CreateAssetMenu(menuName = "Widget/Widget Library")]
public class Widget_Library : ScriptableObject{
	[SerializeField]
	private List<GameObject> Widgets = new List<GameObject>();

	private Dictionary<string, GameObject> WidgetDictionary;

	private void OnEnable(){
		BuildDictionary();
	}

	private void BuildDictionary(){
		WidgetDictionary = new Dictionary<string, GameObject>();

		for (int Index = 0; Index < Widgets.Count; Index++){
			Widget_Parent WidgetReference = Widgets[Index].GetComponent<Widget_Parent>();

			WidgetDictionary.Add(WidgetReference.Name, Widgets[Index]);
		}
	}

	public GameObject GetWidgetFromName(string Name){
		if (WidgetDictionary == null){
			BuildDictionary();
		}

		if (WidgetDictionary.TryGetValue(Name, out GameObject WidgetPrefab)){
			return WidgetPrefab;
		}
		else{
			Debug.LogError(Name + " not found as a Widget");

			return null;
		}
	}
}
