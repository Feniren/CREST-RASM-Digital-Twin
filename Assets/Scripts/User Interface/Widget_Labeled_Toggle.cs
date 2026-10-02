using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class Widget_Labeled_Toggle : Widget_Parent{
	public Toggle ToggleReference;
	public TextMeshProUGUI ToggleNameText;

	public Widget_Labeled_Toggle(){
		Name = "Labeled Toggle";
	}
}
