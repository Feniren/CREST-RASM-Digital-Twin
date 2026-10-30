using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class Widget_Labeled_Toggle : Widget_Parent{
	public Toggle Toggle;
	public TextMeshProUGUI ToggleName;

	public Widget_Labeled_Toggle(){
		Name = "Labeled Toggle";
	}
}
