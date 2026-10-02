using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public class Widget_Switcher : Widget_Parent{
	public List<GameObject> Children = new List<GameObject>();
	public GameObject ActiveChild;
	public int ActiveChildIndex;

	public Widget_Switcher(){
		Name = "Widget Switcher";
	}

	public void AddChild(GameObject ChildWidget){
		ChildWidget.SetActive(false);

		Children.Add(ChildWidget);

		if (Children.Count == 1){
			SetActiveWidget(0);
		}
	}

	public void SetActiveWidget(int Index){
		if (Children.Count != 0){
			if ((Index >= 0) && (Index < Children.Count)){
				if (ActiveChild != null){
					ActiveChild.SetActive(false);
				}

				ActiveChild = Children[Index];
				ActiveChildIndex = Index;

				ActiveChild.SetActive(true);
				
				SetChildSize();
			}
		}
	}

	void SetChildSize(){
		Vector2 Pivot;
		RectTransform RectTransformReference;

		Pivot.x = 0.5f;
		Pivot.y = 0.5f;

		RectTransformReference = ActiveChild.GetComponent<RectTransform>();

		RectTransformReference.anchorMin = Vector2.zero;
		RectTransformReference.anchorMax = Vector2.one;
		RectTransformReference.offsetMin = Vector2.zero;
		RectTransformReference.offsetMax = Vector2.zero;
		RectTransformReference.pivot = Pivot;
	}
}
