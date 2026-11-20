using System;
using System.Collections.Generic;

using UnityEngine;

[CreateAssetMenu(menuName = "Quest/Quest Parent")]
public class Quest_Parent : ScriptableObject{
	[SerializeField]
	string Name;

	[SerializeField]
	[TextArea]
	string Description;

	[SerializeField]
	GameObject QuestManager;

	[SerializeField]
	List<Quest_Step_Parent> QuestSteps;

	[SerializeField]
	Text_Line QuestText;

	public string GetName(){
		return Name;
	}

	public string GetDescription(){
		return Description;
	}

	public List<Quest_Step_Parent> GetQuestSteps(){
		return QuestSteps;
	}

	public Text_Line GetQuestText(){
		return QuestText;
	}

	public GameObject StartQuest(){
		GameObject QuestManagerInstance = Instantiate(QuestManager);

		QuestManagerInstance.GetComponent<Quest_Manager>().SetQuestData(this);

		return QuestManagerInstance;
	}
}
