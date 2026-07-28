using System.Collections.Generic;
using UnityEngine;

// Glows the table clone matching the current lesson step, mirroring how
// Part_Highlighter glows the mill marker. Guided-only, so during the practice
// quiz the parts stay visible but un-highlighted (no answer leak). The
// electronics cabinet has no clone, so its step simply glows nothing here.
public class Table_Part_Display : MonoBehaviour{
    [SerializeField] private Lesson_Sequencer Sequencer;
    [SerializeField] private Transform PartsRoot;
    [SerializeField] private Color TargetGlow = new Color(1f, 0.85f, 0.2f, 0.4f);

    private readonly Dictionary<string, Table_Part> parts = new Dictionary<string, Table_Part>();
    private Table_Part current;

    private void Awake(){
        foreach (Table_Part part in PartsRoot.GetComponentsInChildren<Table_Part>(true))
            parts[part.Part_Id] = part;

        Sequencer.Step_Changed += OnStepChanged;
        Sequencer.Lesson_Completed += OnLessonCompleted;
    }

    private void OnStepChanged(Lesson_Step step, int index, int count){
        ClearCurrent();

        if (Sequencer.Mode != Lesson_Mode.Guided || step.Kind != Lesson_Step_Kind.Select_Component)
            return;

        if (parts.TryGetValue(step.Target_Marker_Id, out current))
            current.Set_Glow(true, TargetGlow);
    }

    private void OnLessonCompleted(Lesson_Result result){
        ClearCurrent();
    }

    private void ClearCurrent(){
        if (current != null)
            current.Set_Glow(false, TargetGlow);

        current = null;
    }
}
