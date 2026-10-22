using UnityEngine;
using UnityEngine.Events;

public class Job_Parent : MonoBehaviour{
	[ReadOnly]
	public string Name;

	public bool BlockExecution;
	
	public Text_Line Description;

	public UnityEvent OnJobComplete;

	public Job_Parent(){
		Name = "";
		BlockExecution = true;
	}
	
	public virtual void ExecuteJob(){
	}
}
