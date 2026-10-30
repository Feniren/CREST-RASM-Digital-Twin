using UnityEngine;
using UnityEngine.Events;

public class Quest_Step_Listener_Parent : MonoBehaviour{
	public UnityEvent QuestStepComplete;

	public Quest_Step_Listener_Parent(){
	}

	protected virtual void OnDisable(){
	}

	protected virtual void OnEnable(){
	}
}
