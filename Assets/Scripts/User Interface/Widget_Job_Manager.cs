using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class Widget_Job_Manager : Widget_Parent{
	List<GameObject> ActiveJobs = new List<GameObject>();

	Job_Manager JobManager;

	[SerializeField]
	Transform ContentContainer;

	[SerializeField]
	TextMeshProUGUI JobManagerText;

	[SerializeField]
	ScrollRect ScrollBox;

	GameObject JobDetailsPanel;

	public Widget_Job_Manager(){
		Name = "Job Manager";
	}

	public override void Start(){
		base.Start();

		JobManager = FindFirstObjectByType<Job_Manager>();

		JobManager.OnJobQueueUpdate.AddListener(UpdateJobList);
		JobManager.OnJobQueueUpdate.AddListener(UpdateJobManagerText);

		UpdateJobList();
		UpdateJobManagerText();
	}

	void OnDestroy(){
		if (JobDetailsPanel){
			Destroy(JobDetailsPanel);
		}
	}

	public void UpdateJobList(){
		for (int Index = 0; Index < ActiveJobs.Count; Index++){
			Destroy(ActiveJobs[Index]);
		}

		ActiveJobs.Clear();

		for (int Index = 0; Index < JobManager.JobQueue.Count; Index++){
			GameObject ActiveJob = CreateWidget("List Item", ContentContainer);
			Widget_List_Item ListItem = ActiveJob.GetComponent<Widget_List_Item>();

			ActiveJob.GetComponentInChildren<TextMeshProUGUI>().text = JobManager.JobQueue[Index].Name;

			ListItem.Index = Index;

			ActiveJob.GetComponent<Button>().onClick.AddListener(() => OnListItemPressed(ListItem.Index));

			ActiveJobs.Add(ActiveJob);

			Canvas.ForceUpdateCanvases();

			ScrollBox.verticalNormalizedPosition = 0.0f;
		}
	}
	
	void UpdateJobManagerText(){
		JobManagerText.text = ("Active Jobs: " + JobManager.JobQueue.Count.ToString());
	}

	public void OnListItemPressed(int Index){
		Widget_Job_Details JobDetailsReference;

		if (JobDetailsPanel == null){
			JobDetailsPanel = CreateWidget("Job Details", new Vector3(0.42f, 0.0f, 1.41f), new Vector3(0.0f, 45.0f, 0.0f), false, false);
		}

		JobDetailsReference = JobDetailsPanel.GetComponent<Widget_Job_Details>();

		JobDetailsReference.SetJob(Index);
	}

	public void OnCancelPressed(){
		CreateWidget("Job Select");
	}
}
