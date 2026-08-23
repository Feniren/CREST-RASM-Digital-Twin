using UnityEngine;

public class Widget_Parent : MonoBehaviour{
	[ReadOnly]
	public string Name;

	public Widget_Parent(){
		Name = "";
	}

	public void OnEnable(){
	}

	public GameObject CreateWidget(string WidgetName, bool SetNewActiveWidget = false, bool DestroyWidget = true){
		GameObject Widget = Instantiate(FindFirstObjectByType<Data_Loader>().WidgetLibrary.GetWidgetFromName(WidgetName), transform.position, transform.rotation);

		if (SetNewActiveWidget){
			FindFirstObjectByType<Player_Controller>().ActiveWidget = Widget;
		}

		if (DestroyWidget){
			Destroy(gameObject);
		}

		return Widget;
	}

	public GameObject CreateWidget(string WidgetName, Vector3 PositionOffset, Vector3 RotationOffset, bool SetNewActiveWidget = false, bool DestroyWidget = true){
		GameObject Widget = Instantiate(FindFirstObjectByType<Data_Loader>().WidgetLibrary.GetWidgetFromName(WidgetName), (transform.position + PositionOffset), transform.rotation);

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
