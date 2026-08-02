using UnityEngine;
using UnityEngine.SceneManagement;

using TMPro;

public class JobSelectWidget : MonoBehaviour{
	public GameObject JobManagerWidget;
	public GameObject PreviousWidget;

	[SerializeField]
	private TextMeshProUGUI JobManagerText;

	public Job_Manager JobManager;

	public JobSelectWidget(){
	}

	public void Start(){
		JobManager = FindFirstObjectByType<Job_Manager>();

		UpdateJobManagerText();

		JobManager.OnJobQueueUpdate.AddListener(UpdateJobManagerText);
	}

	public void UpdateJobManagerText(){
		JobManagerText.text = ("Active Jobs: " + JobManager.JobQueue.Count.ToString());
	}

	public void LoadMap(string Name){
		SceneManager.LoadScene(Name);
	}

	public void JobManagerDetails(){
		GameObject NewWidget = Instantiate(JobManagerWidget, transform.position, transform.rotation);

		Destroy(gameObject);
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
