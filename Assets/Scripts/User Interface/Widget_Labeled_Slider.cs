using UnityEngine;
using UnityEngine.UI;

using System;

using TMPro;

public class Widget_Labeled_Slider : Widget_Parent{
	public Slider SliderReference;
	public TextMeshProUGUI SliderNameText;
	public TextMeshProUGUI SliderValueText;

	public Widget_Labeled_Slider(){
		Name = "Labaled Slider";
	}

	public override void OnDisable(){
		base.OnDisable();

		SliderReference.onValueChanged.RemoveListener(OnValueChanged);
	}

	public override void OnEnable(){
		base.OnEnable();

		SliderReference.onValueChanged.AddListener(OnValueChanged);
	}

	public void OnValueChanged(float Value){
		Value = (float)Math.Round(Value, 2);

		SliderReference.SetValueWithoutNotify(Value);

		SliderValueText.text = Value.ToString();
	}
    
}
