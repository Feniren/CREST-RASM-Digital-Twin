using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class Widget_Job_Details : Widget_Parent{
	[SerializeField]
	private TextMeshProUGUI JobName;

	[SerializeField]
	private TextMeshProUGUI JobDescription;
	
	public Job_Manager JobManagerReference;

	private Job_Parent Job;

	public Widget_Job_Details(){
		Name = "Job Details";
	}

	public void Awake(){
		JobManagerReference = FindFirstObjectByType<Job_Manager>();
	}

	public void SetJob(int JobIndex){
		Job = JobManagerReference.JobQueue[JobIndex];

		JobName.text = Job.Name;
		JobDescription.text = Job.Description.GetTextLine();
		Job.OnJobComplete.AddListener(OnCancelPressed);
	}

	public void OnCancelJobPressed(){
		JobManagerReference.DequeueJob(Job);

		Destroy(gameObject);
	}

	public void OnCancelPressed(){
		Destroy(gameObject);
	}
}
