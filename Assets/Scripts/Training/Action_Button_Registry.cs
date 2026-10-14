using System;
using System.Collections.Generic;
using UnityEngine;

// Mirror of Marker_Registry for Module 2's startup actions. Wires every action
// source under ButtonsRoot — world-space UI buttons (Startup_Action_Button) and
// 3D part interactables (Action_Interactable) alike — to Lesson_Sequencer.Notify_Action,
// and drives the guided highlight by action id.
public class Action_Button_Registry : MonoBehaviour{
    [SerializeField] private Transform ButtonsRoot;
    [SerializeField] private Lesson_Sequencer Sequencer;

    private readonly Dictionary<string, Action<bool>> highlights = new Dictionary<string, Action<bool>>();
    private Panel_Tab_Group[] tabGroups;

    private void Awake(){
        foreach (Startup_Action_Button button in ButtonsRoot.GetComponentsInChildren<Startup_Action_Button>(true)){
            button.Clicked += Sequencer.Notify_Action;
            highlights[button.Action_Id] = button.Set_Highlight;
        }

        foreach (Action_Interactable interactable in ButtonsRoot.GetComponentsInChildren<Action_Interactable>(true)){
            interactable.Clicked += Sequencer.Notify_Action;
            highlights[interactable.Action_Id] = interactable.Set_Highlight;
        }

        tabGroups = ButtonsRoot.GetComponentsInChildren<Panel_Tab_Group>(true);
    }

    public void Set_Highlight(string id){
        foreach (KeyValuePair<string, Action<bool>> entry in highlights)
            entry.Value(entry.Key == id);

        // Guided mode: surface the tab holding the highlighted step so it isn't hidden.
        foreach (Panel_Tab_Group group in tabGroups)
            group.Reveal(id);
    }

    public void Reset_Tabs(){
        if (tabGroups == null)
            return;

        foreach (Panel_Tab_Group group in tabGroups)
            group.Reset_To_First();
    }

    public void Clear_Highlights(){
        foreach (Action<bool> setter in highlights.Values)
            setter(false);
    }
}
