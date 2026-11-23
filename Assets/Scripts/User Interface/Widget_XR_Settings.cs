using UnityEngine;
using UnityEngine.UI;

using System;

public class Widget_XR_Settings : Widget_Parent{
	Player_Settings PlayerSettings;

	public Widget_Labeled_Slider RayThicknessSlider;
	public Widget_Labeled_Slider RayEndpointInterpolationSpeedSlider;
	public Widget_Labeled_Toggle RayEndpointInterpolationToggle;

	public Widget_XR_Settings(){
		Name = "XR Settings";
	}

	public void Awake(){
		RayThicknessSlider.SliderName.text = "Ray Thickness";
		RayEndpointInterpolationSpeedSlider.SliderName.text = "Ray Endpoint Interpolation Speed";
		RayEndpointInterpolationToggle.ToggleName.text = "Ray Endpoint Interpolation";
	}

	public override void OnDisable(){
		base.OnDisable();

		RayThicknessSlider.Slider.onValueChanged.RemoveListener(OnRayThicknessValueChanged);
		RayEndpointInterpolationSpeedSlider.Slider.onValueChanged.RemoveListener(OnRayEndpointInterpolationSpeedValueChanged);
		RayEndpointInterpolationToggle.Toggle.onValueChanged.RemoveListener(OnRayEndpointInterpolationValueChanged);
	}

	public override void OnEnable(){
		base.OnEnable();

		RayThicknessSlider.Slider.onValueChanged.AddListener(OnRayThicknessValueChanged);
		RayEndpointInterpolationSpeedSlider.Slider.onValueChanged.AddListener(OnRayEndpointInterpolationSpeedValueChanged);
		RayEndpointInterpolationToggle.Toggle.onValueChanged.AddListener(OnRayEndpointInterpolationValueChanged);
	}

	public override void Start(){
		base.Start();

		PlayerSettings = Entity_Player.GetLocalPlayer().PlayerSettings;

		RayThicknessSlider.Slider.value = PlayerSettings.XRRayThickness;
		RayThicknessSlider.Slider.minValue = 0.01f;
		RayThicknessSlider.Slider.maxValue = 0.1f;

		RayEndpointInterpolationSpeedSlider.Slider.value = PlayerSettings.XRRayEndpointInterpolationSpeed;
		RayEndpointInterpolationSpeedSlider.Slider.minValue = 0.01f;
		RayEndpointInterpolationSpeedSlider.Slider.maxValue = 1.0f;

		RayEndpointInterpolationToggle.Toggle.isOn = PlayerSettings.XRRayEndpointInterpolation;
	}

	public void OnRayThicknessValueChanged(float Value){
		Value = (float)Math.Round(Value, 2);

		PlayerSettings.XRRayThickness = Value;
	}

	public void OnRayEndpointInterpolationSpeedValueChanged(float Value){
		Value = (float)Math.Round(Value, 2);

		PlayerSettings.XRRayEndpointInterpolationSpeed = Value;
	}

	public void OnRayEndpointInterpolationValueChanged(bool Value){
		PlayerSettings.XRRayEndpointInterpolation = Value;
	}
}
