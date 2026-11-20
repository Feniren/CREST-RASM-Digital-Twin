using System;
using System.Collections.Generic;

using UnityEngine;

public class Text_Line_Manager : MonoBehaviour{
	int CurrentLine;
	bool FinalLine;
	List<Text_Data> TextLines;

	public Text_Line_Manager(){
		CurrentLine = 0;
		FinalLine = false;
	}

	public Text_Data GetCurrentLine(){
		if (CurrentLine < TextLines.Count){
			return TextLines[CurrentLine];
		}
		else{
			return new Text_Data();
		}
	}

	public int GetCurrentLineNumber(){
		return CurrentLine;
	}

	public Text_Data NextLine(){
		if (!FinalLine){
			CurrentLine = Mathf.Min((CurrentLine + 1), (TextLines.Count - 1));

			if (CurrentLine == (TextLines.Count - 1)){
				FinalLine = true;
			}
		}

		return TextLines[CurrentLine];
	}

	public Text_Data PreviousLine(){
		CurrentLine = Mathf.Max((CurrentLine - 1), 0);

		if (TextLines.Count > 1){
			FinalLine = false;
		}

		return TextLines[CurrentLine];
	}

	public void RemoveProgressLock(){
		for (int Index = 0; Index < TextLines.Count; Index++){
			if (TextLines[Index].LockProgress){
				TextLines[Index].LockProgress = false;

				break;
			}
		}
	}

	public void RemoveProgressBlock(int QuestStepIndex){
		for (int Index = 0; Index < TextLines.Count; Index++){
			if (TextLines[Index].QuestStepIndex == QuestStepIndex){
				if (TextLines[Index].LockProgress){
					TextLines[Index].LockProgress = false;
				}
			}
		}
	}

	public void SetTextLines(List<Text_Data> Lines){
		TextLines = new List<Text_Data>();

		foreach (Text_Data Text in Lines){
			TextLines.Add(new Text_Data(Text));
		}

		if (TextLines.Count <= 1){
			FinalLine = true;
		}
	}
}
