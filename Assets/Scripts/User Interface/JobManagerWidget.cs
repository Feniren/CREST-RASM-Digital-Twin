using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class JobManagerWidget : MonoBehaviour{
	public GameObject ListItem;
	public GameObject PreviousWidget;

	private List<GameObject> ActiveJobs = new List<GameObject>();

	[SerializeField]
	private Transform ContentContainer;

	[SerializeField]
	private TextMeshProUGUI JobManagerText;

	[SerializeField]
	private ScrollRect ScrollBox;

	public Job_Manager JobManager;

	public JobManagerWidget(){
	}

	public void Start(){
		JobManager = FindFirstObjectByType<Job_Manager>();

		JobManager.OnJobQueueUpdate.AddListener(UpdateJobList);
		JobManager.OnJobQueueUpdate.AddListener(UpdateJobManagerText);

		UpdateJobList();
		UpdateJobManagerText();
	}

	public void UpdateJobList(){
		for (int Index = 0; Index < ActiveJobs.Count; Index++){
			Destroy(ActiveJobs[Index]);
		}

		ActiveJobs.Clear();

		for (int Index = 0; Index < JobManager.JobQueue.Count; Index++){
			GameObject ActiveJob = Instantiate(ListItem, ContentContainer);

			ActiveJob.GetComponent<TextMeshProUGUI>().text = JobManager.JobQueue[Index].Name;

			ActiveJobs.Add(ActiveJob);

			Canvas.ForceUpdateCanvases();

			ScrollBox.verticalNormalizedPosition = 0.0f;
		}
	}

	public void UpdateJobManagerText(){
		JobManagerText.text = ("Active Jobs: " + JobManager.JobQueue.Count.ToString());
	}

	public void Cancel(){
		GameObject NewWidget = Instantiate(PreviousWidget, transform.position, transform.rotation);

		Destroy(gameObject);
	}
}
