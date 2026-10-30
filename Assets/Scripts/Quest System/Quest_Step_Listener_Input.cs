using System;
using System.Collections;

using UnityEngine;
using UnityEngine.InputSystem;

public class Quest_Step_Listener_Input : Quest_Step_Listener_Parent{
	InputAction InputActionInstance;
	Coroutine InputHoldOperation;
	float ElapsedInputHoldTime;
	float RequiredInputHoldTime;

	public Quest_Step_Listener_Input(){
		ElapsedInputHoldTime = 0.0f;
		RequiredInputHoldTime = float.MaxValue;
	}

	protected override void OnDisable(){
		base.OnDisable();

		EndInputHold();

		if (InputActionInstance != null){
			InputActionInstance.started -= OnInputActionStarted;
			InputActionInstance.canceled -= OnInputActionCanceled;
		}
	}

	protected override void OnEnable(){
		base.OnEnable();
	}

	void OnInputActionStarted(InputAction.CallbackContext Context){
		InputHoldOperation = StartCoroutine(InputHold());
	}

	void OnInputActionCanceled(InputAction.CallbackContext Context){
		EndInputHold();
	}

	void EndInputHold(){
		if (InputHoldOperation != null){
			StopCoroutine(InputHoldOperation);

			InputHoldOperation = null;
		}
	}

	public float GetElapsedInputHoldTime(){
		return ElapsedInputHoldTime;
	}

	IEnumerator InputHold(){
		while (true){
			yield return new WaitForSeconds(0.1f);

			ElapsedInputHoldTime += 0.1f;

			if (ElapsedInputHoldTime >= RequiredInputHoldTime){
				QuestStepComplete.Invoke();

				yield break;
			}
		}
	}

	public void SetInputAction(InputActionReference InputActionReference){
		InputActionInstance = InputActionReference.action;

		InputActionInstance.started += OnInputActionStarted;
		InputActionInstance.canceled += OnInputActionCanceled;
	}

	public void SetRequiredInputHoldTime(float HoldTime){
		RequiredInputHoldTime = HoldTime;
	}
}
