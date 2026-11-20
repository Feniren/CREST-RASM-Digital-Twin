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
		MouseXSlider.SliderName.text = "Mouse Sensitivity X";
		MouseYSlider.SliderName.text = "Mouse Sensitivity Y";
	}

	public override void OnDisable(){
		base.OnDisable();

		MouseXSlider.Slider.onValueChanged.RemoveListener(OnMouseXValueChanged);
		MouseYSlider.Slider.onValueChanged.RemoveListener(OnMouseYValueChanged);
	}

	public override void OnEnable(){
		base.OnEnable();

		MouseXSlider.Slider.onValueChanged.AddListener(OnMouseXValueChanged);
		MouseYSlider.Slider.onValueChanged.AddListener(OnMouseYValueChanged);
	}

	public override void Start(){
		base.Start();

		PlayerSettings = Entity_Player.GetLocalPlayer().PlayerSettings;

		MouseXSlider.Slider.value = PlayerSettings.LookSpeedX;
		MouseXSlider.Slider.minValue = 0.1f;
		MouseXSlider.Slider.maxValue = 1.0f;

		MouseYSlider.Slider.value = PlayerSettings.LookSpeedY;
		MouseYSlider.Slider.minValue = 0.1f;
		MouseYSlider.Slider.maxValue = 1.0f;
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
