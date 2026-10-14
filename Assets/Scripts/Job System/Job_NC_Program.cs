using System.Collections;
using System.IO;

using UnityEngine;
using UnityEngine.Events;

public class Job_NC_Program : Job_Parent{
	public string ProgramFile;

	public Item_Parent StockReference;

	public NC_Mill_Station StationReference;

	NC_Job Program;

	bool Armed;

	public Job_NC_Program(){
		Name = "NC Program";

		OnJobComplete = new UnityEvent();
	}

	public void OnValidate(){
		Name = "NC " + Path.GetFileNameWithoutExtension(ProgramFile);
	}

	public void OnEnable(){
		StationReference = FindFirstObjectByType<NC_Mill_Station>();
	}

	public void OnTargetFound(){
		StationReference.Mill.SensorReference.OnTargetFound.RemoveListener(OnTargetFound);

		Armed = false;

		StationReference.Run(Program, StationReference.Mill.SensorReference.SensedPlate, Complete);
	}

	public override void ExecuteJob(){
		Program = StationReference.Load(ProgramFile);

		if (Program == null){
			StartCoroutine(CompleteNextFrame());

			return;
		}

		StationReference.Mill.SensorReference.TargetItem = StockReference;

		StationReference.Mill.SensorReference.OnTargetFound.AddListener(OnTargetFound);

		Armed = true;
	}

	// Job_Manager subscribes to OnJobComplete only after ExecuteJob returns.
	IEnumerator CompleteNextFrame(){
		yield return null;

		OnJobComplete.Invoke();
	}

	void Complete(){
		// Cancelled mid-cut: Job_Manager already dequeued this job, invoking would dequeue the next one.
		if (this == null){
			return;
		}

		OnJobComplete.Invoke();
	}

	public void OnDestroy(){
		if (Armed){
			StationReference.Mill.SensorReference.OnTargetFound.RemoveListener(OnTargetFound);

			if (StationReference.Mill.SensorReference.TargetItem == StockReference){
				StationReference.Mill.SensorReference.TargetItem = null;
			}
		}
	}
}
