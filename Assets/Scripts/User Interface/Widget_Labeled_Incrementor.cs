using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

using System;

using TMPro;

public class Widget_Labeled_Incrementor : Widget_Parent{
	public Widget_Button DecrementButton;
	public Widget_Button IncrementButton;
	public TextMeshProUGUI IncrementorName;
	public TextMeshProUGUI IncrementorValue;

	public float DecrementAmount;
	public float IncrementAmount;

	public UnityEvent ValueChanged;

	float IncrementorCurrentValue;
	float IncrementorMaximumValue;
	float IncrementorMinimumValue;

	public Widget_Labeled_Incrementor(){
		Name = "Labeled Incrementor";
	}

	public override void OnDisable(){
		base.OnDisable();

		ValueChanged.RemoveListener(OnValueChanged);

		DecrementButton.onClick.RemoveListener(OnDecrementPressed);
		IncrementButton.onClick.RemoveListener(OnIncrementPressed);
	}

	public override void OnEnable(){
		base.OnEnable();

		ValueChanged.AddListener(OnValueChanged);

		DecrementButton.onClick.AddListener(OnDecrementPressed);
		IncrementButton.onClick.AddListener(OnIncrementPressed);
	}

	void OnDecrementPressed(){
		IncrementorCurrentValue = Mathf.Max((float)Math.Round((IncrementorCurrentValue - DecrementAmount), 2), IncrementorMinimumValue);

		ValueChanged.Invoke();
	}

	void OnIncrementPressed(){
		IncrementorCurrentValue = Mathf.Min((float)Math.Round((IncrementorCurrentValue + IncrementAmount), 2), IncrementorMaximumValue);

		ValueChanged.Invoke();
	}

	void OnValueChanged(){
		IncrementorValue.text = IncrementorCurrentValue.ToString();
	}

	public float GetCurrentValue(){
		return IncrementorCurrentValue;
	}

	public float GetMaximumValue(){
		return IncrementorMaximumValue;
	}

	public float GetMinimumValue(){
		return IncrementorMinimumValue;
	}

	public void SetCurrentValue(float Value){
		IncrementorCurrentValue = Mathf.Clamp(Value, IncrementorMinimumValue, IncrementorMaximumValue);

		ValueChanged.Invoke();
	}

	public void SetMaximumValue(float Value){
		IncrementorMaximumValue = Value;

		if (IncrementorCurrentValue > IncrementorMaximumValue){
			SetCurrentValue(IncrementorMaximumValue);
		}
	}

	public void SetMinimumValue(float Value){
		IncrementorMinimumValue = Value;

		if (IncrementorCurrentValue < IncrementorMinimumValue){
			SetCurrentValue(IncrementorMinimumValue);
		}
	}
}
