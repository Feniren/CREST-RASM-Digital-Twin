using UnityEngine;

[CreateAssetMenu(menuName = "Quest/Quest Step/Quest Step")]
public class Quest_Step_Parent : ScriptableObject{
	[SerializeField]
	protected GameObject QuestStepListener;

	[SerializeField]
	protected Text_Line QuestStepText;

	protected bool QuestStepComplete;

	public virtual GameObject ExecuteQuestStep(){
		GameObject Listener = Instantiate(QuestStepListener);

		Listener.GetComponent<Quest_Step_Listener_Parent>().SetQuestStepText(QuestStepText);

		return Listener;
	}
}
