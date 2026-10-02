using System.Collections;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Slider_Drag_Handler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler{
	Slider SliderReference;
	Coroutine DragOperation;
	float MouseDelta;

	[SerializeField]
	float Sensitivity;

	public Slider_Drag_Handler(){
		MouseDelta = 0.0f;
	}

	void Awake(){
		SliderReference = GetComponent<Slider>();
	}

	public void OnDisable(){
		EndDrag();
	}

	public void OnEnable(){
	}

	public void OnPointerDown(PointerEventData EventData){
		if (DragOperation == null){
			DragOperation = StartCoroutine(Drag());
		}
	}

	public void OnPointerUp(PointerEventData EventData){
		EndDrag();
	}

	void EndDrag(){
		if (DragOperation != null){
			StopCoroutine(DragOperation);

			DragOperation = null;
		}
	}

	IEnumerator Drag(){
		while (true){
			if (Mouse.current != null){
				if (Mouse.current.leftButton.isPressed){
					MouseDelta = Mouse.current.delta.ReadValue().x;

					if (MouseDelta != 0.0f){
						SliderReference.value = Mathf.Clamp((SliderReference.value + (MouseDelta * Sensitivity)), SliderReference.minValue, SliderReference.maxValue);
					}
				}
			}

			yield return null;
		}
	}
}
