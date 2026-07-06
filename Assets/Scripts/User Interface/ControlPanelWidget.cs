using UnityEngine;

public class ControlPanelWidget : MonoBehaviour{
	public ControlPanelWidget(){
	}

	public void ExitGame(){
		#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
		#else
			Application.Quit();
		#endif

	}
}
