using UnityEngine;

public class Widget_Parent : MonoBehaviour{
	[ReadOnly]
	public string Name;

	protected Data_Loader DataLoader;

	public Widget_Parent(){
		Name = "";
	}

	public virtual void OnDisable(){
	}

	public virtual void OnEnable(){
	}

	public virtual void Start(){
		DataLoader = Data_Loader.GetDataLoader();
	}

	public GameObject CreateWidget(string WidgetName, bool SetNewActiveWidget = false, bool DestroyWidget = true){
		GameObject Widget = Instantiate(DataLoader.WidgetLibrary.GetWidgetFromName(WidgetName), transform.position, transform.rotation);

		if (SetNewActiveWidget){
			FindFirstObjectByType<Player_Controller>().ActiveWidget = Widget;
		}

		if (DestroyWidget){
			Destroy(gameObject);
		}

		return Widget;
	}

	public GameObject CreateWidget(string WidgetName, Transform WidgetTransform){
		GameObject Widget = Instantiate(DataLoader.WidgetLibrary.GetWidgetFromName(WidgetName), WidgetTransform);

		return Widget;
	}

	public GameObject CreateWidget(string WidgetName, Vector3 PositionOffset, Vector3 RotationOffset, bool SetNewActiveWidget = false, bool DestroyWidget = true){
		GameObject Widget = Instantiate(DataLoader.WidgetLibrary.GetWidgetFromName(WidgetName), (transform.position + PositionOffset), transform.rotation);

		Widget.transform.Rotate(RotationOffset);

		if (SetNewActiveWidget){
			FindFirstObjectByType<Player_Controller>().ActiveWidget = Widget;
		}

		if (DestroyWidget){
			Destroy(gameObject);
		}

		return Widget;
	}

	protected void ExitGame(){
		#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
		#else
			Application.Quit();
		#endif
	}
}
