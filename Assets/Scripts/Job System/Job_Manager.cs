using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

public class Job_Manager : MonoBehaviour{
	public List<Job_Parent> JobQueue = new List<Job_Parent>();

	public UnityEvent OnJobQueueUpdate;

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

		OnJobQueueUpdate.Invoke();

		RunJob();
	}

	public void DequeueJob(Job_Parent Job){
		JobQueue.Remove(Job);

		Destroy(Job);

		JobActive = false;

		OnJobQueueUpdate.Invoke();

		RunJob();
	}

	public void DequeueJob(int JobIndex){
		Destroy(JobQueue[JobIndex]);

		JobQueue.RemoveAt(JobIndex);

		JobActive = false;

		OnJobQueueUpdate.Invoke();

		RunJob();
	}

	public void QueueJob(Job_Parent JobReference){
		JobQueue.Add(JobReference);

		OnJobQueueUpdate.Invoke();

		RunJob();
	}

	public void QueueJob(string JobName){
		GameObject Job = Instantiate(JobLibraryReference.GetJobFromName(JobName), Vector3.zero, Quaternion.identity);

		if (Job){
			JobQueue.Add(Job.GetComponent<Job_Parent>());

			OnJobQueueUpdate.Invoke();

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
