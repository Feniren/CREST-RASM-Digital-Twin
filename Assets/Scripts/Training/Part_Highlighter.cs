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

        if (Sequencer.Mode != Lesson_Mode.Guided || step.Kind != Lesson_Step_Kind.Select_Component)
            return;

        // A component that flies into place demonstrates itself — glowing its marker as
        // well just competes with the animation.
        if (demonstrator != null && demonstrator.Demonstrates(step.Target_Marker_Id))
            return;

        currentTarget = Registry.Resolve(step.Target_Marker_Id);

        if (currentTarget != null){
            currentTarget.Set_Persistent_Glow(true, TargetGlow);
            currentTarget.Set_Label_Visible(true);
        }
    }

    private void OnLessonCompleted(Lesson_Result result){
        ClearTarget();
    }

    // Both modes: the quiz needs it to show what is about to be picked, and it costs
    // nothing in the guided tour. The persistent target glow stays Guided-only — that is
    // the one that would give the answer away.
    private void OnHoverChanged(Component_Marker marker, bool entered){
        if (entered)
            marker.Set_Hover_Tint(Hover);
        else
            marker.Clear_Hover_Tint();
    }

    private void ClearTarget(){
        if (currentTarget != null){
            currentTarget.Set_Persistent_Glow(false, TargetGlow);
            currentTarget.Set_Label_Visible(false);
        }

        currentTarget = null;
    }
}
