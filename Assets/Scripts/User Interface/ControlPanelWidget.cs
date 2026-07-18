using UnityEngine;

public class ControlPanelWidget : MonoBehaviour{
	public GameObject MapSelectWidget;

	public ControlPanelWidget(){
	}

	public void CreateWidget(){
		GameObject NewWidget = Instantiate(MapSelectWidget, transform.position, transform.rotation);

		Destroy(gameObject);
	}

	public void ExitGame(){
		#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
		#else
			Application.Quit();
		#endif

	}
}
