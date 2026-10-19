using System.Collections.Generic;
using UnityEngine;

public class Part_Highlighter : MonoBehaviour{
    [SerializeField] private Lesson_Sequencer Sequencer;
    [SerializeField] private Marker_Registry Registry;
    [SerializeField] private Color TargetGlow = new Color(1f, 0.85f, 0.2f, 0.4f);
    // Deliberately one neutral colour rather than correct/wrong tints. In the quiz a
    // green-on-correct hover would let the trainee sweep the machine until it lit up and
    // never actually answer; in the guided tour there is nothing to click any more.
    [SerializeField] private Color Hover = new Color(0.6f, 0.85f, 1f, 0.45f);

    private Component_Marker currentTarget;
    private Step_Demonstrator demonstrator;
    private readonly HashSet<Component_Marker> hovered = new HashSet<Component_Marker>();

    private void Awake(){
        demonstrator = GetComponent<Step_Demonstrator>();
        Sequencer.Step_Changed += OnStepChanged;
        Sequencer.Lesson_Completed += OnLessonCompleted;
    }

    private void Start(){
        foreach (Component_Marker marker in Registry.All)
            marker.Hover_Changed += OnHoverChanged;
    }

    private void OnStepChanged(Lesson_Step step, int index, int count){
        ClearTarget();
        Refresh_Hover();

        if (Sequencer.Mode != Lesson_Mode.Guided || step.Kind != Lesson_Step_Kind.Select_Component)
            return;

        // A component that flies into place demonstrates itself — glowing its marker as
        // well just competes with the animation.
        if (demonstrator != null && demonstrator.Demonstrates(step.Target_Marker_Id))
            return;

        currentTarget = Registry.Resolve(step.Target_Marker_Id);

        if (currentTarget != null)
            currentTarget.Set_Persistent_Glow(true, TargetGlow);
    }

    private void OnLessonCompleted(Lesson_Result result){
        ClearTarget();
        Refresh_Hover();
    }

    // The hover tint shows what is about to be picked, so it only appears while a quiz
    // question is up — nothing is pickable in the tour or around it. The persistent target
    // glow stays Guided-only; that is the one that would give the answer away.
    private void OnHoverChanged(Component_Marker marker, bool entered){
        if (entered)
            hovered.Add(marker);
        else
            hovered.Remove(marker);

        Set_Hover(marker, entered && Quiz_Active());
    }

    // A pointer already resting on a part when the quiz starts or ends raises no hover
    // event, so step changes re-apply the tint to whatever is hovered.
    private void Refresh_Hover(){
        bool on = Quiz_Active();

        foreach (Component_Marker marker in hovered)
            Set_Hover(marker, on);
    }

    private void Set_Hover(Component_Marker marker, bool on){
        if (on)
            marker.Set_Hover_Tint(Hover);
        else
            marker.Clear_Hover_Tint();
    }

    private bool Quiz_Active(){
        Lesson_Step step = Sequencer.Current_Step;
        return Sequencer.Mode == Lesson_Mode.Practice && step != null && step.Kind == Lesson_Step_Kind.Select_Component;
    }

    private void ClearTarget(){
        if (currentTarget != null)
            currentTarget.Set_Persistent_Glow(false, TargetGlow);

        currentTarget = null;
    }
}
