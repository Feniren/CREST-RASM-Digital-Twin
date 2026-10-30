using UnityEngine;

public class Widget_Text : Widget_Parent{
	public Widget_List_Item NextButton;
	public Widget_List_Item PreviousButton;

	public Widget_Text(){
		Name = "Text";
	}

	public override void OnDisable(){
		base.OnDisable();

		NextButton.Button.onClick.RemoveListener(OnNextPressed);
		PreviousButton.Button.onClick.RemoveListener(OnPreviousPressed);
	}

	public override void OnEnable(){
		base.OnEnable();

		NextButton.Button.onClick.AddListener(OnNextPressed);
		PreviousButton.Button.onClick.AddListener(OnPreviousPressed);
	}

	void OnNextPressed(){
	}

	void OnPreviousPressed(){
	}
}
