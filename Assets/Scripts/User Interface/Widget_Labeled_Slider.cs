using UnityEngine;
using UnityEngine.UI;

using System;

using TMPro;

public class Widget_Labeled_Slider : Widget_Parent{
	public Slider Slider;
	public TextMeshProUGUI SliderName;
	public TextMeshProUGUI SliderValue;

	public Widget_Labeled_Slider(){
		Name = "Labaled Slider";
	}

	public override void OnDisable(){
		base.OnDisable();

		Slider.onValueChanged.RemoveListener(OnValueChanged);
	}

	public override void OnEnable(){
		base.OnEnable();

		Slider.onValueChanged.AddListener(OnValueChanged);
	}

	public void OnValueChanged(float Value){
		Value = (float)Math.Round(Value, 2);

		Slider.SetValueWithoutNotify(Value);

		SliderValue.text = Value.ToString();
	}
    
}
