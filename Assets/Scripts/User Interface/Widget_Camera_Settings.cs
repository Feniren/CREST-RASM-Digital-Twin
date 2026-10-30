using UnityEngine;

using System;

public class Widget_Camera_Settings : Widget_Parent{
	public Widget_Labeled_Toggle ThirdPersonControlRotationToggle;
	public Widget_Labeled_Slider ThirdPersonCameraDistanceSlider;
	public Widget_Labeled_Slider ThirdPersonCameraSpringArmForceSlider;
	public Widget_Labeled_Incrementor ThirdPersonCameraOffsetXIncrementor;
	public Widget_Labeled_Incrementor ThirdPersonCameraOffsetYIncrementor;
	public Widget_Labeled_Incrementor ThirdPersonCameraOffsetZIncrementor;

	Player_Settings PlayerSettings;

	public Widget_Camera_Settings(){
		Name = "Camera Settings";
	}

	public void Awake(){
		ThirdPersonControlRotationToggle.ToggleName.text = "Control Rotation";
		ThirdPersonCameraDistanceSlider.SliderName.text = "Camera Distance";
		ThirdPersonCameraSpringArmForceSlider.SliderName.text = "Spring Arm Force";
		ThirdPersonCameraOffsetXIncrementor.IncrementorName.text = "X";
		ThirdPersonCameraOffsetYIncrementor.IncrementorName.text = "Y";
		ThirdPersonCameraOffsetZIncrementor.IncrementorName.text = "Z";
	}

	public override void OnDisable(){
		base.OnDisable();

		ThirdPersonControlRotationToggle.Toggle.onValueChanged.RemoveListener(OnThirdPersonControlRotationChanged);
		ThirdPersonCameraDistanceSlider.Slider.onValueChanged.RemoveListener(OnThirdPersonCameraDistanceChanged);
		ThirdPersonCameraSpringArmForceSlider.Slider.onValueChanged.RemoveListener(OnThirdPersonCameraSpringArmForceChanged);
		ThirdPersonCameraOffsetXIncrementor.ValueChanged.RemoveListener(OnThirdPersonCameraOffsetXChanged);
		ThirdPersonCameraOffsetYIncrementor.ValueChanged.RemoveListener(OnThirdPersonCameraOffsetYChanged);
		ThirdPersonCameraOffsetZIncrementor.ValueChanged.RemoveListener(OnThirdPersonCameraOffsetZChanged);
	}

	public override void OnEnable(){
		base.OnEnable();

		ThirdPersonControlRotationToggle.Toggle.onValueChanged.AddListener(OnThirdPersonControlRotationChanged);
		ThirdPersonCameraDistanceSlider.Slider.onValueChanged.AddListener(OnThirdPersonCameraDistanceChanged);
		ThirdPersonCameraSpringArmForceSlider.Slider.onValueChanged.AddListener(OnThirdPersonCameraSpringArmForceChanged);
		ThirdPersonCameraOffsetXIncrementor.ValueChanged.AddListener(OnThirdPersonCameraOffsetXChanged);
		ThirdPersonCameraOffsetYIncrementor.ValueChanged.AddListener(OnThirdPersonCameraOffsetYChanged);
		ThirdPersonCameraOffsetZIncrementor.ValueChanged.AddListener(OnThirdPersonCameraOffsetZChanged);
	}

	public override void Start(){
		base.Start();

		PlayerSettings = Entity_Player.GetLocalPlayer().PlayerSettings;

		ThirdPersonControlRotationToggle.Toggle.isOn = PlayerSettings.ThirdPersonControlRotation;

		ThirdPersonCameraDistanceSlider.Slider.value = PlayerSettings.ThirdPersonCameraDistance;
		ThirdPersonCameraDistanceSlider.Slider.minValue = 1.0f;
		ThirdPersonCameraDistanceSlider.Slider.maxValue = 4.0f;

		ThirdPersonCameraSpringArmForceSlider.Slider.value = PlayerSettings.ThirdPersonCameraSpringArmForce;
		ThirdPersonCameraSpringArmForceSlider.Slider.minValue = 1.0f;
		ThirdPersonCameraSpringArmForceSlider.Slider.maxValue = 20.0f;

		ThirdPersonCameraOffsetXIncrementor.SetMinimumValue(-2.0f);
		ThirdPersonCameraOffsetXIncrementor.SetMaximumValue(2.0f);
		ThirdPersonCameraOffsetXIncrementor.SetCurrentValue(PlayerSettings.ThirdPersonCameraOffset.x);
		ThirdPersonCameraOffsetXIncrementor.IncrementAmount = 0.1f;
		ThirdPersonCameraOffsetXIncrementor.DecrementAmount = 0.1f;

		ThirdPersonCameraOffsetYIncrementor.SetMinimumValue(-2.0f);
		ThirdPersonCameraOffsetYIncrementor.SetMaximumValue(2.0f);
		ThirdPersonCameraOffsetYIncrementor.SetCurrentValue(PlayerSettings.ThirdPersonCameraOffset.y);
		ThirdPersonCameraOffsetYIncrementor.IncrementAmount = 0.1f;
		ThirdPersonCameraOffsetYIncrementor.DecrementAmount = 0.1f;

		ThirdPersonCameraOffsetZIncrementor.SetMinimumValue(-2.0f);
		ThirdPersonCameraOffsetZIncrementor.SetMaximumValue(2.0f);
		ThirdPersonCameraOffsetZIncrementor.SetCurrentValue(PlayerSettings.ThirdPersonCameraOffset.z);
		ThirdPersonCameraOffsetZIncrementor.IncrementAmount = 0.1f;
		ThirdPersonCameraOffsetZIncrementor.DecrementAmount = 0.1f;
	}

	public void OnThirdPersonControlRotationChanged(bool Value){
		PlayerSettings.ThirdPersonControlRotation = Value;
	}

	public void OnThirdPersonCameraDistanceChanged(float Value){
		Value = (float)Math.Round(Value, 2);

		PlayerSettings.ThirdPersonCameraDistance = Value;
	}

	public void OnThirdPersonCameraSpringArmForceChanged(float Value){
		Value = (float)Math.Round(Value, 2);

		PlayerSettings.ThirdPersonCameraSpringArmForce = Value;
	}

	public void OnThirdPersonCameraOffsetXChanged(){
		PlayerSettings.ThirdPersonCameraOffset.x = ThirdPersonCameraOffsetXIncrementor.GetCurrentValue();
	}

	public void OnThirdPersonCameraOffsetYChanged(){
		PlayerSettings.ThirdPersonCameraOffset.y = ThirdPersonCameraOffsetYIncrementor.GetCurrentValue();
	}

	public void OnThirdPersonCameraOffsetZChanged(){
		PlayerSettings.ThirdPersonCameraOffset.z = ThirdPersonCameraOffsetZIncrementor.GetCurrentValue();
	}
}
