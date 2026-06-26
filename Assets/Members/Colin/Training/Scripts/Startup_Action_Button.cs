using System;
using UnityEngine;
using UnityEngine.UI;

// A world-space UI button that reports an ordered startup action to the lesson.
// Both physical power buttons (on the props) and software-panel buttons use this;
// Action_Button_Registry wires Clicked to Lesson_Sequencer.Notify_Action, and the
// Startup_State_Controller toggles the guided highlight via Set_Highlight.
[RequireComponent(typeof(Button))]
public class Startup_Action_Button : MonoBehaviour{
    public string Action_Id;

    [SerializeField] private GameObject Highlight;

    public event Action<string> Clicked;

    private Button button;

    private void Awake(){
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClicked);
        Set_Highlight(false);
    }

    private void OnDestroy(){
        if (button != null)
            button.onClick.RemoveListener(OnClicked);
    }

    public void Set_Highlight(bool on){
        if (Highlight != null)
            Highlight.SetActive(on);
    }

    private void OnClicked(){
        Clicked?.Invoke(Action_Id);
    }
}
