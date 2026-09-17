using UnityEngine;
using UnityEngine.SceneManagement;

using TMPro;

public class Widget_Job_Select : Widget_Parent{
	public GameObject PreviousWidget;

	[SerializeField]
	private TextMeshProUGUI JobManagerText;

	public Job_Manager JobManager;

	public Widget_Job_Select(){
		Name = "Job Select";
	}

	public void Start(){
		JobManager = FindFirstObjectByType<Job_Manager>();

		UpdateJobManagerText();

		JobManager.OnJobQueueUpdate.AddListener(UpdateJobManagerText);
	}

	public void UpdateJobManagerText(){
		JobManagerText.text = ("Active Jobs: " + JobManager.JobQueue.Count.ToString());
	}

	public void OnJobManagerPressed(){
		CreateWidget("Job Manager");
	}

	public void OnPenholderPressed(){
		JobManager.QueueJob("Mill Epoxy Penholder");
	}

	public void OnPenTubePressed(){
		JobManager.QueueJob("Lathe Metal Penholder");
	}

	public void Cancel(){
		//GameObject NewWidget = Instantiate(PreviousWidget, transform.position, transform.rotation);

		//Destroy(gameObject);
	}
}
