using UnityEngine;
using UnityEngine.Events;

public class Job_Parent : MonoBehaviour{
	public string Name;
	public bool BlockExecution;

	public UnityEvent OnJobComplete;

	public Job_Parent(){
		Name = "";
		BlockExecution = true;
	}
	
	public virtual void ExecuteJob(){
	}
}
