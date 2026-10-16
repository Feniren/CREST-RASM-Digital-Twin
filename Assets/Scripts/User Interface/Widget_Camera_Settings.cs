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
		ThirdPersonControlRotationToggle.ToggleNameText.text = "Control Rotation";
		ThirdPersonCameraDistanceSlider.SliderNameText.text = "Camera Distance";
		ThirdPersonCameraSpringArmForceSlider.SliderNameText.text = "Spring Arm Force";
		ThirdPersonCameraOffsetXIncrementor.IncrementorName.text = "X";
		ThirdPersonCameraOffsetYIncrementor.IncrementorName.text = "Y";
		ThirdPersonCameraOffsetZIncrementor.IncrementorName.text = "Z";
	}

	public override void OnDisable(){
		base.OnDisable();

		ThirdPersonControlRotationToggle.ToggleReference.onValueChanged.RemoveListener(OnThirdPersonControlRotationChanged);
		ThirdPersonCameraDistanceSlider.SliderReference.onValueChanged.RemoveListener(OnThirdPersonCameraDistanceChanged);
		ThirdPersonCameraSpringArmForceSlider.SliderReference.onValueChanged.RemoveListener(OnThirdPersonCameraSpringArmForceChanged);
		ThirdPersonCameraOffsetXIncrementor.ValueChanged.RemoveListener(OnThirdPersonCameraOffsetXChanged);
		ThirdPersonCameraOffsetYIncrementor.ValueChanged.RemoveListener(OnThirdPersonCameraOffsetYChanged);
		ThirdPersonCameraOffsetZIncrementor.ValueChanged.RemoveListener(OnThirdPersonCameraOffsetZChanged);
	}

	public override void OnEnable(){
		base.OnEnable();

		ThirdPersonControlRotationToggle.ToggleReference.onValueChanged.AddListener(OnThirdPersonControlRotationChanged);
		ThirdPersonCameraDistanceSlider.SliderReference.onValueChanged.AddListener(OnThirdPersonCameraDistanceChanged);
		ThirdPersonCameraSpringArmForceSlider.SliderReference.onValueChanged.AddListener(OnThirdPersonCameraSpringArmForceChanged);
		ThirdPersonCameraOffsetXIncrementor.ValueChanged.AddListener(OnThirdPersonCameraOffsetXChanged);
		ThirdPersonCameraOffsetYIncrementor.ValueChanged.AddListener(OnThirdPersonCameraOffsetYChanged);
		ThirdPersonCameraOffsetZIncrementor.ValueChanged.AddListener(OnThirdPersonCameraOffsetZChanged);
	}

	public override void Start(){
		base.Start();

		PlayerSettings = Entity_Player.GetLocalPlayer().PlayerSettings;

		ThirdPersonControlRotationToggle.ToggleReference.isOn = PlayerSettings.ThirdPersonControlRotation;

		ThirdPersonCameraDistanceSlider.SliderReference.value = PlayerSettings.ThirdPersonCameraDistance;
		ThirdPersonCameraDistanceSlider.SliderReference.minValue = 1.0f;
		ThirdPersonCameraDistanceSlider.SliderReference.maxValue = 4.0f;

		ThirdPersonCameraSpringArmForceSlider.SliderReference.value = PlayerSettings.ThirdPersonCameraSpringArmForce;
		ThirdPersonCameraSpringArmForceSlider.SliderReference.minValue = 1.0f;
		ThirdPersonCameraSpringArmForceSlider.SliderReference.maxValue = 20.0f;

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
