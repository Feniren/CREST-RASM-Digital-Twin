using UnityEngine;
using UnityEngine.Events;

public class Quest_Step_Listener_Parent : MonoBehaviour{
	Text_Line QuestStepText;

	public bool QuestStepCompleted;
	public UnityEvent QuestStepComplete;

	public Quest_Step_Listener_Parent(){
		QuestStepCompleted = false;
	}

	protected virtual void OnDisable(){
	}

	protected virtual void OnEnable(){
	}

	public Text_Line GetQuestStepText(){
		return QuestStepText;
	}

	public void SetQuestStepText(Text_Line Text){
		QuestStepText = Text;
	}
}
