using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image_Rounded_Corners))]
public class Widget_Button : Button{
	[Header("Border Color")]
	[SerializeField]
	[ReadOnly]
	private Color DefaultBorderColor;

	[SerializeField]
	private Color HighlightedBorderColor;

	[Header("Background Opacity")]
	[SerializeField]
	[ReadOnly]
	private float DefaultBackgroundOpacity;

	[SerializeField]
	[Range(0.0f, 1.0f)]
	private float HighlightedBackgroundOpacity;

	private Image BackgroundImage;
	private Image_Rounded_Corners RoundedCornerImage;

	protected override void Awake(){
		base.Awake();

		BackgroundImage = targetGraphic as Image;

		RoundedCornerImage = GetComponent<Image_Rounded_Corners>();

		DefaultBorderColor = RoundedCornerImage.GetBorderColor();

		if (BackgroundImage){
			DefaultBackgroundOpacity = BackgroundImage.color.a;
		}
	}

	protected override void DoStateTransition(SelectionState State, bool Instant){
		base.DoStateTransition(State, Instant);

		switch (State){
			case SelectionState.Normal:
				RoundedCornerImage.SetBorderColor(DefaultBorderColor);

				if (BackgroundImage){
					DefaultBackgroundOpacity = BackgroundImage.color.a;
				}

				break;
			case SelectionState.Highlighted:
				RoundedCornerImage.SetBorderColor(HighlightedBorderColor);

				if (BackgroundImage){
					BackgroundImage.color = new Color(1.0f, 1.0f, 1.0f, HighlightedBackgroundOpacity);
				}

				break;
			default:
				RoundedCornerImage.SetBorderColor(DefaultBorderColor);

				if (BackgroundImage){
					BackgroundImage.color = new Color(1.0f, 1.0f, 1.0f, DefaultBackgroundOpacity);
				}

				break;
		}
	}
}
