using System;
using System.Collections.Generic;

using UnityEngine;

[CreateAssetMenu(menuName = "Text/Text Line")]
public class Text_Line : ScriptableObject{
	[SerializeField]
	[TextArea]
	private string TextLine;

	[SerializeField]
	private List<string> Responses = new List<string>();

	[SerializeField]
	private List<ScriptableObject> NextLines = new List<ScriptableObject>();

	public string GetTextLine(){
		return TextLine;
	}
}
