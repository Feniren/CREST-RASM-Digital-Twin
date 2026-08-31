using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(RectTransform))]
public class Image_Rounded_Corners : MonoBehaviour, IMaterialModifier{
	[SerializeField]
	private Color BorderColor = Color.white;

	[SerializeField]
	private float BorderWidth = 0.0f;

	[SerializeField]
	private float CornerRadius = 24.0f;

	private Image SourceImage;
	private Material SourceMaterial;
	private RectTransform ImageRectTransform;
	private Material MaterialInstance;

	private static readonly int SizeProperty = Shader.PropertyToID("_Size");
	private static readonly int RadiusProperty = Shader.PropertyToID("_CornerRadius");
	private static readonly int BorderWidthProperty = Shader.PropertyToID("_BorderWidth");
	private static readonly int BorderColorProperty = Shader.PropertyToID("_BorderColor");

	private Vector2 LastSize;
	private float LastRadius;
	private Color LastBorderColor;
	private float LastBorderWidth;

	private void OnEnable(){
		SourceImage = GetComponent<Image>();
		ImageRectTransform = GetComponent<RectTransform>();

		SourceImage.SetMaterialDirty();
	}

	private void OnDisable(){
		DestroyInstance();

		if (SourceImage){
			SourceImage.SetMaterialDirty();
		}
	}

	private void Update(){
		Vector2 CurrentSize = ImageRectTransform.rect.size;

		if ((CurrentSize != LastSize) || (CornerRadius != LastRadius) || (BorderColor != LastBorderColor) || (BorderWidth != LastBorderWidth)){
			ApplyValues();
		}
	}

	private void OnValidate(){
		ApplyValues();
	}

	public Material GetModifiedMaterial(Material BaseMaterial){
		if (isActiveAndEnabled){
			if (SourceMaterial != BaseMaterial){
				DestroyInstance();
			}

			if (MaterialInstance == null){
				MaterialInstance = new Material(BaseMaterial);

				MaterialInstance.hideFlags = HideFlags.DontSave;
			}

			ApplyValues();

			return MaterialInstance;
		}
		else{
			return BaseMaterial;
		}
	}

	private void ApplyValues(){
		if (MaterialInstance){
			Vector2 Size = ImageRectTransform.rect.size;

			MaterialInstance.SetVector(SizeProperty, new Vector4(Size.x, Size.y, 0, 0));
			MaterialInstance.SetFloat(RadiusProperty, CornerRadius);
			MaterialInstance.SetColor(BorderColorProperty, BorderColor);
			MaterialInstance.SetFloat(BorderWidthProperty, BorderWidth);

			LastSize = Size;
			LastRadius = CornerRadius;
			LastBorderColor = BorderColor;
			LastBorderWidth = BorderWidth;
		}
	}

	private void DestroyInstance(){
		if (MaterialInstance){
			if (Application.isPlaying){
				Destroy(MaterialInstance);
			}
			else{
				DestroyImmediate(MaterialInstance);
			}

			MaterialInstance = null;
			SourceMaterial = null;
		}
	}

	public Color GetBorderColor(){
		return BorderColor;
	}

	public void SetBorderColor(Color Color){
		BorderColor = Color;

		ApplyValues();
	}


	public void SetBorderWidth(float Width){
		BorderWidth = Width;

		ApplyValues();
	}


	public void SetCornerRadius(float Radius){
		CornerRadius = Radius;

		ApplyValues();
	}
}

