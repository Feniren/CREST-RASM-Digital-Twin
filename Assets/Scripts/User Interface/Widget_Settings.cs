using UnityEngine;

public class Widget_Settings : Widget_Parent{
	public Widget_List_Item CameraButton;
	public Widget_List_Item MouseButton;
	public Widget_List_Item XRButton;
	public Widget_List_Item CancelButton;
	public Widget_Switcher WidgetSwitcher;

	public Widget_Settings(){
		Name = "Settings";
	}

	public override void OnDisable(){
		base.OnDisable();

		CameraButton.Button.onClick.RemoveListener(OnCameraPressed);
		MouseButton.Button.onClick.RemoveListener(OnMousePressed);
		XRButton.Button.onClick.RemoveListener(OnXRPressed);
		CancelButton.Button.onClick.RemoveListener(OnCancelPressed);
	}

	public override void OnEnable(){
		base.OnEnable();

		CameraButton.Button.onClick.AddListener(OnCameraPressed);
		MouseButton.Button.onClick.AddListener(OnMousePressed);
		XRButton.Button.onClick.AddListener(OnXRPressed);
		CancelButton.Button.onClick.AddListener(OnCancelPressed);
	}

	public override void Start(){
		base.Start();

		WidgetSwitcher.AddChild(CreateWidget("Camera Settings", WidgetSwitcher.transform));
		WidgetSwitcher.AddChild(CreateWidget("Mouse Settings", WidgetSwitcher.transform));
		WidgetSwitcher.AddChild(CreateWidget("XR Settings", WidgetSwitcher.transform));
	}

	public void OnCameraPressed(){
		WidgetSwitcher.SetActiveWidget(0);
	}

	public void OnMousePressed(){
		WidgetSwitcher.SetActiveWidget(1);
	}

	public void OnXRPressed(){
		WidgetSwitcher.SetActiveWidget(2);
	}

	public void OnCancelPressed(){
		DataLoader.SaveGame();

		CreateWidget("System", true);
	}
}
