using UnityEngine;
using UnityEngine.SceneManagement;

public class JobSelectWidget : MonoBehaviour{
	public GameObject PreviousWidget;

	public JobSelectWidget(){
	}

	public void LoadMap(string Name){
		SceneManager.LoadScene(Name);
	}

	public void QueuePenholderJob(){
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

		//Destroy(gameObject);
	}
}
