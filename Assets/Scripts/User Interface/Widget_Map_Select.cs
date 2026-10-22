using UnityEngine;
using UnityEngine.SceneManagement;

public class Widget_Map_Select : Widget_Parent{
	public Widget_Map_Select(){
		Name = "Map Select";
	}

	public void LoadMap(string Name){
		SceneManager.LoadScene(Name);
	}

	public void OnDigitalTwinPressed(){
		LoadMap("DigitalTwin");
	}

	public void OnMillModulePressed(){
		LoadMap("Bootstrap");
	}

	public void OnRobotArmModulePressed(){
		LoadMap("Arm-Module");
	}

	public void OnMainMenuPressed(){
		LoadMap("Main Menu");
	}

	public void OnCancelPressed(){
		CreateWidget("System", true);
	}
}
