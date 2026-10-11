using UnityEngine;

public class Widget_Control : Widget_Parent{
	public Widget_List_Item CancelButton;
	public Widget_List_Item DesktopButton;
	public Widget_List_Item XRButton;
	public Widget_Switcher WidgetSwitcher;

	public Widget_Control(){
		Name = "Control";
	}

	public override void OnDisable(){
		base.OnDisable();

		DesktopButton.Button.onClick.RemoveListener(OnDesktopPressed);
		XRButton.Button.onClick.RemoveListener(OnXRPressed);
		CancelButton.Button.onClick.RemoveListener(OnCancelPressed);
	}

	public override void OnEnable(){
		base.OnEnable();

		DesktopButton.Button.onClick.AddListener(OnDesktopPressed);
		XRButton.Button.onClick.AddListener(OnXRPressed);
		CancelButton.Button.onClick.AddListener(OnCancelPressed);
	}

	public override void Start(){
		base.Start();

		WidgetSwitcher.AddChild(CreateWidget("Desktop Control", WidgetSwitcher.transform));
		WidgetSwitcher.AddChild(CreateWidget("XR Control", WidgetSwitcher.transform));
	}

	void OnDesktopPressed(){
		WidgetSwitcher.SetActiveWidget(0);
	}

	void OnXRPressed(){
		WidgetSwitcher.SetActiveWidget(1);
	}

	public void OnCancelPressed(){
		CreateWidget("System", true);
	}
}
