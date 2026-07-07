using UnityEngine;

// Toggles a simple visible state cue on a mill part when its Component_Marker is
// selected, with a Debug.Log. Gated like Door_Click_Toggle so it never swallows
// quiz/guided selections. Used for the Power On and Emergency Stop buttons.
public class Marker_State_Toggle : MonoBehaviour{
    [SerializeField] private Component_Marker Marker;
    [SerializeField] private Renderer TargetRenderer;
    [SerializeField] private Lesson_Sequencer Sequencer;
    [SerializeField] private string ActionLabel = "Action";
    [SerializeField] private Color OnColor = new Color(0.2f, 1f, 0.3f, 1f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private MaterialPropertyBlock block;
    private Color offColor;
    private bool captured;
    private bool isOn;

    private void OnEnable(){
        if (Marker != null)
            Marker.Selected += On_Selected;
    }

    private void OnDisable(){
        if (Marker != null)
            Marker.Selected -= On_Selected;
    }

    // Act only when no selection step is pending, mirroring Door_Click_Toggle, so
    // a button press never consumes a quiz or guided answer.
    public void On_Selected(Component_Marker marker){
        Lesson_Step step = Sequencer != null ? Sequencer.Current_Step : null;

        if (!(step == null || step.Kind == Lesson_Step_Kind.Info))
            return;

        if (TargetRenderer == null)
            return;

        if (block == null)
            block = new MaterialPropertyBlock();

        if (!captured){
            offColor = TargetRenderer.sharedMaterial != null && TargetRenderer.sharedMaterial.HasProperty(BaseColorId)
                ? TargetRenderer.sharedMaterial.GetColor(BaseColorId)
                : Color.white;
            captured = true;
        }

        isOn = !isOn;
        TargetRenderer.GetPropertyBlock(block);
        block.SetColor(BaseColorId, isOn ? OnColor : offColor);
        TargetRenderer.SetPropertyBlock(block);

        Debug.Log($"Marker_State_Toggle: {ActionLabel} {(isOn ? "ON" : "OFF")}");
    }
}
