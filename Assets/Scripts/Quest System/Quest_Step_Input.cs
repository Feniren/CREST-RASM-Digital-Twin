using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "Quest/Quest Step/Input")]
public class Quest_Step_Input : Quest_Step_Parent{
	[SerializeField]
	InputActionReference InputAction;

	[SerializeField]
	float RequiredInputHoldTime;

	public override GameObject ExecuteQuestStep(){
		GameObject QuestStepListenerInput = base.ExecuteQuestStep();
		Quest_Step_Listener_Input InputListener = QuestStepListenerInput.GetComponent<Quest_Step_Listener_Input>();

		InputListener.SetRequiredInputHoldTime(RequiredInputHoldTime);
		InputListener.SetInputAction(InputAction);

		return QuestStepListenerInput;
	}
}
