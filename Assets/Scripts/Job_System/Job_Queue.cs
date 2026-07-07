using System.Collections.Generic;
using UnityEngine;

public class Job_Queue : MonoBehaviour {
	public List<Machine_Job> Jobs = new List<Machine_Job>();
	public bool TestMode_AddMillJob = true;

	void Start(){
		if (TestMode_AddMillJob && !HasJob(Machine_Job_Type.Mill)){
			Jobs.Add(new Machine_Job(Machine_Job_Type.Mill));
		}
	}

	public bool HasJob(Machine_Job_Type type){
		for (int i = 0; i < Jobs.Count; i++){
			if (Jobs[i].JobType == type)
				return true;
		}
		return false;
	}

	public bool jobPeek(Machine_Job_Type type) {
		if (Jobs[0].JobType == type) {
			return true;
		}	
		return false;
    }

    public Machine_Job_Type jobPeek()
    {
        if (Jobs.Count > 0)
        {
            return Jobs[0].JobType;
        }
		throw new System.InvalidOperationException("Job queue is empty.");
    }

    public void jobPop()
    {
        Jobs.RemoveAt(0);        
    }

    public void RemoveJob(Machine_Job_Type type){
		for (int i = 0; i < Jobs.Count; i++){
			if (Jobs[i].JobType == type){
				Jobs.RemoveAt(i);
				return;
			}
		}
	}
}
