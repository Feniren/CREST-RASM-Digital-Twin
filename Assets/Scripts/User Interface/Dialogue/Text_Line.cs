using System;
using System.Collections.Generic;

using UnityEngine;

[CreateAssetMenu(menuName = "Text/Text Line")]
public class Text_Line : ScriptableObject{
	[SerializeField]
	GameObject TextLineManager;

	[SerializeField]
	List<Text_Data> TextLines = new List<Text_Data>();

	public GameObject CreateTextLineManager(){
		GameObject TextLineManagerInstance = Instantiate(TextLineManager);

		TextLineManagerInstance.GetComponent<Text_Line_Manager>().SetTextLines(TextLines);

		return TextLineManagerInstance;
	}

	public List<Text_Data> GetTextLines(){
		return TextLines;
	}
}
