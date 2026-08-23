using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class Widget_Job_Manager : Widget_Parent{
	private List<GameObject> ActiveJobs = new List<GameObject>();

	private Data_Loader DataLoader;
	private Job_Manager JobManager;

	[SerializeField]
	private Transform ContentContainer;

	[SerializeField]
	private TextMeshProUGUI JobManagerText;

	[SerializeField]
	private ScrollRect ScrollBox;

	private GameObject JobDetailsPanel;

	public Widget_Job_Manager(){
		Name = "Job Manager";
	}

	public void Start(){
		DataLoader = FindFirstObjectByType<Data_Loader>();
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
			GameObject ActiveJob = Instantiate(DataLoader.WidgetLibrary.GetWidgetFromName("List Item"), ContentContainer);
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
			JobDetailsPanel = CreateWidget("Job Details", new Vector3(0.6f, 0.0f, 2.1f), new Vector3(0.0f, 45.0f, 0.0f), false, false);
		}

		JobDetailsReference = JobDetailsPanel.GetComponent<Widget_Job_Details>();

		JobDetailsReference.SetJob(Index);
	}

	public void OnCancelPressed(){
		CreateWidget("Job Select");
	}
}
