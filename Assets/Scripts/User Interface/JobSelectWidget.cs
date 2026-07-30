using UnityEngine;
using UnityEngine.SceneManagement;

public class JobSelectWidget : MonoBehaviour{
	public GameObject PreviousWidget;

	public Job_Manager JobManager;

	public JobSelectWidget(){
	}

	public void Start(){
		JobManager = FindFirstObjectByType<Job_Manager>();
	}

	public void LoadMap(string Name){
		SceneManager.LoadScene(Name);
	}

	public void QueuePenholderJob(){
		JobManager.QueueJob("Mill Epoxy Penholder");
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
