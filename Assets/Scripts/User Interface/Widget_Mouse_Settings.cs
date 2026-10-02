using UnityEngine;
using UnityEngine.UI;

using System;

public class Widget_Mouse_Settings : Widget_Parent{
	Player_Settings PlayerSettings;

	public Widget_Labeled_Slider MouseXSlider;
	public Widget_Labeled_Slider MouseYSlider;

	public Widget_Mouse_Settings(){
		Name = "Mouse Settings";
	}

	public void Awake(){
		MouseXSlider.SliderNameText.text = "Mouse Sensitivity X";
		MouseYSlider.SliderNameText.text = "Mouse Sensitivity Y";
	}

	public override void OnDisable(){
		base.OnDisable();

		MouseXSlider.SliderReference.onValueChanged.RemoveListener(OnMouseXValueChanged);
		MouseYSlider.SliderReference.onValueChanged.RemoveListener(OnMouseYValueChanged);
	}

	public override void OnEnable(){
		base.OnEnable();

		MouseXSlider.SliderReference.onValueChanged.AddListener(OnMouseXValueChanged);
		MouseYSlider.SliderReference.onValueChanged.AddListener(OnMouseYValueChanged);
	}

	public void Start(){
		PlayerSettings = Entity_Player.GetLocalPlayer().PlayerSettings;

		MouseXSlider.SliderReference.value = PlayerSettings.LookSpeedX;
		MouseXSlider.SliderReference.minValue = 0.1f;
		MouseXSlider.SliderReference.maxValue = 1.0f;

		MouseYSlider.SliderReference.value = PlayerSettings.LookSpeedY;
		MouseYSlider.SliderReference.minValue = 0.1f;
		MouseYSlider.SliderReference.maxValue = 1.0f;
	}

	public void OnMouseXValueChanged(float Value){
		Value = (float)Math.Round(Value, 2);

		PlayerSettings.LookSpeedX = Value;
	}

	public void OnMouseYValueChanged(float Value){
		Value = (float)Math.Round(Value, 2);

		PlayerSettings.LookSpeedY = Value;
	}
}
