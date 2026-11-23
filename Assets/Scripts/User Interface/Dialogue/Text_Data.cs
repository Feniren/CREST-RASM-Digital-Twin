using UnityEngine;

[System.Serializable]
public class Text_Data{
	public bool LockProgress;
	public int QuestStepIndex;
	public string Speaker;

	[TextArea]
	public string Text;

	public Text_Data(){
		LockProgress = false;
		QuestStepIndex = -1;
		Speaker = "";
		Text = "";
	}

	public Text_Data(Text_Data Original){
		LockProgress = Original.LockProgress;
		QuestStepIndex = Original.QuestStepIndex;
		Speaker = Original.Speaker;
		Text = Original.Text;
	}

	public void UnlockProgress(){
		LockProgress = false;
	}
}
