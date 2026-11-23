using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

public class Quest_Manager : MonoBehaviour{
	Quest_Parent QuestData;

	public UnityEvent QuestAdvanced;

	Quest_Step_Listener_Parent ActiveStep;
	int ActiveStepIndex;
	List<Quest_Step_Listener_Parent> QuestSteps;
	GameObject TextLine;
	Text_Line_Manager TextLineManager;

	public Quest_Manager(){
		ActiveStepIndex = 0;
		QuestSteps = new List<Quest_Step_Listener_Parent>();
	}

	public Text_Line_Manager GetTextLineManager(){
		return TextLineManager;
	}

	public void OnQuestStepComplete(){
		TextLineManager.RemoveProgressBlock(ActiveStepIndex);

		ActiveStep.QuestStepComplete.RemoveListener(OnQuestStepComplete);

		AdvanceQuest();
	}

	void AdvanceQuest(){
		ActiveStepIndex++;

		if (ActiveStepIndex < QuestData.GetQuestSteps().Count){
			ActiveStep = QuestData.GetQuestSteps()[ActiveStepIndex].ExecuteQuestStep().GetComponent<Quest_Step_Listener_Parent>();

			QuestSteps.Add(ActiveStep);

			ActiveStep.QuestStepComplete.AddListener(OnQuestStepComplete);

			QuestAdvanced.Invoke();
		}
	}

	public Quest_Step_Listener_Parent GetActiveStep(){
		return ActiveStep;
	}

	public string GetQuestName(){
		return QuestData.GetName();
	}

	public Quest_Step_Listener_Parent GetQuestStep(){
		if (TextLineManager.GetCurrentLine().QuestStepIndex >= 0){
			return QuestSteps[TextLineManager.GetCurrentLine().QuestStepIndex];
		}
		else{
			return QuestSteps[0];
		}
	}

	public Quest_Step_Listener_Parent GetQuestStep(int Index){
		return QuestSteps[Index];
	}

	public void SetQuestData(Quest_Parent Data){
		QuestData = Data;

		StartQuest();
	}

	public void StartQuest(){
		if (QuestData.GetQuestSteps().Count > 0){
			ActiveStep = QuestData.GetQuestSteps()[ActiveStepIndex].ExecuteQuestStep().GetComponent<Quest_Step_Listener_Parent>();
			TextLine = QuestData.GetQuestText().CreateTextLineManager();
			TextLineManager = TextLine.GetComponent<Text_Line_Manager>();

			QuestSteps.Add(ActiveStep);

			ActiveStep.QuestStepComplete.AddListener(OnQuestStepComplete);
		}
	}
}
