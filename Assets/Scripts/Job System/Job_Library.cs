using System;
using System.Collections.Generic;

using UnityEngine;

[CreateAssetMenu(menuName = "Job/Job Library")]
public class Job_Library : ScriptableObject{
	[SerializeField]
	private List<GameObject> Jobs = new List<GameObject>();

	private Dictionary<string, GameObject> JobDictionary;

	private void OnEnable(){
		BuildDictionary();
	}

	private void BuildDictionary(){
		JobDictionary = new Dictionary<string, GameObject>();

		for (int Index = 0; Index < Jobs.Count; Index++){
			Job_Parent JobReference = Jobs[Index].GetComponent<Job_Parent>();
			
			JobDictionary.Add(JobReference.Name, Jobs[Index]);
		}
	}

	public GameObject GetJobFromName(string Name){
		if (JobDictionary == null){
			BuildDictionary();
		}

		if (JobDictionary.TryGetValue(Name, out GameObject JobPrefab)){
			return JobPrefab;
		}
		else{
			Debug.LogError(Name + " not found as a Job");

			return null;
		}
	}
}
