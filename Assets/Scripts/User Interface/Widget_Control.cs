using UnityEngine;

public class Widget_Control : Widget_Parent{
	public Widget_Control(){
		Name = "Control";
	}

	public void OnCancelPressed(){
		CreateWidget("System", true);
	}
}
