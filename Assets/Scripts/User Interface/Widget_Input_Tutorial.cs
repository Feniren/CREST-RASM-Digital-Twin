using System;

using UnityEngine;

public class Widget_Input_Tutorial : Widget_Parent{
	public Widget_Navigation_Bar NavigationBar;
	public Widget_Labeled_Slider ProgressBar;
	public Widget_Labeled_Text Text;
	public Widget_Labeled_Text Title;

	public Quest_Parent Quest;

	GameObject QuestManager;
	Quest_Manager QuestInstance;
	Quest_Step_Listener_Input QuestStepInput;

	public Widget_Input_Tutorial(){
		Name = "Input Tutorial";
	}

	public override void OnDisable(){
		base.OnDisable();

		NavigationBar.NextButton.Button.onClick.RemoveListener(OnNextPressed);
		NavigationBar.PreviousButton.Button.onClick.RemoveListener(OnPreviousPressed);
	}

	public override void OnEnable(){
		base.OnEnable();

		NavigationBar.NextButton.Button.onClick.AddListener(OnNextPressed);
		NavigationBar.PreviousButton.Button.onClick.AddListener(OnPreviousPressed);
	}

	public override void Start(){
		base.Start();

		if (Quest){
			QuestManager = Quest.StartQuest();
			QuestInstance = QuestManager.GetComponent<Quest_Manager>();

			Title.Text.text = QuestInstance.GetQuestName();
			Text.Text.text = QuestInstance.GetTextLineManager().GetCurrentLine().Text;

			QuestInstance.QuestAdvanced.AddListener(OnQuestAdvanced);

			UpdateProgressBar();
		}

		ProgressBar.SetInteractable(false);
	}

	void OnElapsedInputHoldTimeChanged(float Value){
		Value = (float)Math.Round(Value, 2);

		ProgressBar.Slider.value = Value;
	}

	void OnNextPressed(){
		if (!QuestInstance.GetTextLineManager().GetCurrentLine().LockProgress){
			Text.Text.text = QuestInstance.GetTextLineManager().NextLine().Text;

			UpdateProgressBar();
		}
	}

	void OnPreviousPressed(){
		Text.Text.text = QuestInstance.GetTextLineManager().PreviousLine().Text;

		UpdateProgressBar();
	}

	void OnQuestAdvanced(){
		UpdateProgressBar();
	}

	void UpdateProgressBar(){
		if (QuestInstance.GetTextLineManager().GetCurrentLine().QuestStepIndex >= 0){
			if (QuestInstance.GetQuestStep() is Quest_Step_Listener_Input){
				QuestStepInput = (QuestInstance.GetQuestStep() as Quest_Step_Listener_Input);

				ProgressBar.SetActive(true);

				ProgressBar.Slider.value = QuestStepInput.GetElapsedInputHoldTime();
				ProgressBar.Slider.minValue = 0.0f;
				ProgressBar.Slider.maxValue = QuestStepInput.GetRequiredInputHoldTime();

				if (QuestInstance.GetQuestStep() == QuestInstance.GetActiveStep()){
					QuestStepInput.ElapsedInputHoldTimeChanged.AddListener(OnElapsedInputHoldTimeChanged);
				}
			}
		}
		else{
			ProgressBar.SetActive(false);
		}
	}
}
