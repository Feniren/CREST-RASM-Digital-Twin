using UnityEngine;

public class Widget_Settings : Widget_Parent{
	public Widget_Switcher WidgetSwitcherReference;

	public Widget_Settings(){
		Name = "Settings";
	}

	public void Start(){
		WidgetSwitcherReference.AddChild(CreateWidget("Mouse Settings", WidgetSwitcherReference.transform));
		WidgetSwitcherReference.AddChild(CreateWidget("XR Settings", WidgetSwitcherReference.transform));
	}

	public void OnMousePressed(){
		WidgetSwitcherReference.SetActiveWidget(0);
	}

	public void OnXRPressed(){
		WidgetSwitcherReference.SetActiveWidget(1);
	}

	public void OnCancelPressed(){
		CreateWidget("System", true);
	}
}
