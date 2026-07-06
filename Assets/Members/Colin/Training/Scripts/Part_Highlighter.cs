using UnityEngine;

public class Part_Highlighter : MonoBehaviour{
    [SerializeField] private Lesson_Sequencer Sequencer;
    [SerializeField] private Marker_Registry Registry;
    [SerializeField] private Color TargetGlow = new Color(1f, 0.85f, 0.2f, 0.4f);
    [SerializeField] private Color CorrectHover = new Color(0.2f, 1f, 0.3f, 0.45f);
    [SerializeField] private Color WrongHover = new Color(1f, 0.25f, 0.2f, 0.45f);

    private Component_Marker currentTarget;

    private void Awake(){
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

        currentTarget = Registry.Resolve(step.Target_Marker_Id);

        if (currentTarget != null){
            currentTarget.Set_Persistent_Glow(true, TargetGlow);
            currentTarget.Set_Label_Visible(true);
        }
    }

    private void OnLessonCompleted(Lesson_Result result){
        ClearTarget();
    }

    private void OnHoverChanged(Component_Marker marker, bool entered){
        if (Sequencer.Mode != Lesson_Mode.Guided)
            return;

        if (entered)
            marker.Set_Hover_Tint(marker == currentTarget ? CorrectHover : WrongHover);
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
