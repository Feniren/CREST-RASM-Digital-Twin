using UnityEngine;

public class Widget_System : Widget_Parent{
	public Widget_System(){
		Name = "System";
	}

	public void OnControlsPressed(){
		CreateWidget("Control", true);
	}

	public void OnLoadMapPressed(){
		CreateWidget("Map Select", true);
	}

	public void OnQuitGamePressed(){
		ExitGame();
	}

	public void OnCancelPressed(){
		Destroy(gameObject);
	}
}
