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
		RayThicknessSlider.SliderNameText.text = "Ray Thickness";
		RayEndpointInterpolationSpeedSlider.SliderNameText.text = "Ray Endpoint Interpolation Speed";
		RayEndpointInterpolationToggle.ToggleNameText.text = "Ray Endpoint Interpolation";
	}

	public override void OnDisable(){
		base.OnDisable();

		RayThicknessSlider.SliderReference.onValueChanged.RemoveListener(OnRayThicknessValueChanged);
		RayEndpointInterpolationSpeedSlider.SliderReference.onValueChanged.RemoveListener(OnRayEndpointInterpolationSpeedValueChanged);
		RayEndpointInterpolationToggle.ToggleReference.onValueChanged.RemoveListener(OnRayEndpointInterpolationValueChanged);
	}

	public override void OnEnable(){
		base.OnEnable();

		RayThicknessSlider.SliderReference.onValueChanged.AddListener(OnRayThicknessValueChanged);
		RayEndpointInterpolationSpeedSlider.SliderReference.onValueChanged.AddListener(OnRayEndpointInterpolationSpeedValueChanged);
		RayEndpointInterpolationToggle.ToggleReference.onValueChanged.AddListener(OnRayEndpointInterpolationValueChanged);
	}

	public void Start(){
		PlayerSettings = Entity_Player.GetLocalPlayer().PlayerSettings;

		RayThicknessSlider.SliderReference.value = PlayerSettings.XRRayThickness;
		RayThicknessSlider.SliderReference.minValue = 0.01f;
		RayThicknessSlider.SliderReference.maxValue = 0.1f;

		RayEndpointInterpolationSpeedSlider.SliderReference.value = PlayerSettings.XRRayEndpointInterpolationSpeed;
		RayEndpointInterpolationSpeedSlider.SliderReference.minValue = 0.01f;
		RayEndpointInterpolationSpeedSlider.SliderReference.maxValue = 1.0f;

		RayEndpointInterpolationToggle.ToggleReference.isOn = PlayerSettings.XRRayEndpointInterpolation;
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
