using UnityEngine;
using UnityEngine.SceneManagement;

public class MapSelectWidget : MonoBehaviour{
	public GameObject PreviousWidget;

	public MapSelectWidget(){
	}

	public void LoadMap(string Name){
		SceneManager.LoadScene(Name);
	}

	public void LoadDigitalTwin(){
		LoadMap("DigitalTwin");
	}

	public void LoadMillModule(){
		LoadMap("Bootstrap");
	}

	public void LoadRobotArmModule(){
		LoadMap("Arm-Module");
	}

	public void LoadMainMenu(){
		LoadMap("Main Menu");
	}

	public void Cancel(){
		GameObject NewWidget = Instantiate(PreviousWidget, transform.position, transform.rotation);

		Destroy(gameObject);
	}
}
