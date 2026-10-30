using UnityEngine;

[CreateAssetMenu(menuName = "Quest/Quest Step/Quest Step")]
public class Quest_Step_Parent : ScriptableObject{
	[SerializeField]
	protected GameObject QuestStepListener;

	protected bool QuestStepComplete;

	public virtual GameObject ExecuteQuestStep(){
		return Instantiate(QuestStepListener);
	}
}
