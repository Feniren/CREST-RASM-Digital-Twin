using System.Collections.Generic;
using UnityEngine;

public class Job_Manager : MonoBehaviour{
	public List<Job_Parent> JobQueue = new List<Job_Parent>();

	public Job_Library JobLibraryReference;

	bool JobActive;

	public Job_Manager(){
		JobActive = false;
	}

	public void Start(){
	}

	public void DequeueJob(){
		Destroy(JobQueue[0]);

		JobQueue.RemoveAt(0);

		JobActive = false;

		RunJob();
	}

	public void QueueJob(Job_Parent JobReference){
		JobQueue.Add(JobReference);

		RunJob();
	}

	public void QueueJob(string JobName){
		GameObject Job = Instantiate(JobLibraryReference.GetJobFromName(JobName), Vector3.zero, Quaternion.identity);

		if (Job){
			JobQueue.Add(Job.GetComponent<Job_Parent>());

			RunJob();
		}
	}

	public void RunJob(){
		if (!JobActive){
			if (JobQueue.Count > 0){
				JobQueue[0].ExecuteJob();

				JobActive = true;

				if (JobQueue[0].BlockExecution){
					JobQueue[0].OnJobComplete.AddListener(OnJobComplete);
				}
				else{
					DequeueJob();
				}
			}
		}
	}

	public void OnJobComplete(){
		DequeueJob();
	}
}
