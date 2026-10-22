using System.Collections.Generic;

using UnityEngine;

public class Widget_Switcher : Widget_Parent{
	public List<GameObject> Children = new List<GameObject>();
	public GameObject ActiveChild;
	public int ActiveChildIndex;

	public Widget_Switcher(){
		Name = "Widget Switcher";
	}
}
