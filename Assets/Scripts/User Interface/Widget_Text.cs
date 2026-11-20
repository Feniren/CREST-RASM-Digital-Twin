using UnityEngine;

using TMPro;

public class Widget_Text : Widget_Parent{
	public Widget_Switcher WidgetSwitcher;

	public string QuestName;

	public Widget_Text(){
		Name = "Text";
	}

	public override void OnDisable(){
		base.OnDisable();
	}

	public override void OnEnable(){
		base.OnEnable();

		Debug.Log("Widget Switcher original size " + WidgetSwitcher.GetComponent<RectTransform>().rect.size);
	}

	public override void Start(){
		base.Start();

		WidgetSwitcher.AddChild(CreateWidget(QuestName, WidgetSwitcher.transform));

		Debug.Log("Widget Switcher size: " + WidgetSwitcher.GetComponent<RectTransform>().rect.size);
	}
}
