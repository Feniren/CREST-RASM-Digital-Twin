using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Guided-tour demonstration for Module 1. The table copy of the component being taught
// glides out of its slot and into the part's real place on the mill, loops that trip
// while the trainee watches, and settles there for good when they press Continue — so
// the machine assembles itself as the tour runs.
//
// Practice is untouched: the quiz stays click-to-select, and by then every part is
// already on the mill, which is what the trainee is asked to point at.
public class Table_Part_Glide : MonoBehaviour, Step_Demonstrator{
    [SerializeField] private Lesson_Sequencer Sequencer;
    [SerializeField] private Transform PartsRoot;

    [Header("Timing (seconds)")]
    [SerializeField] private float GlideSeconds = 1.6f;
    [SerializeField] private float HoldAtMillSeconds = 1.2f;
    [SerializeField] private float HoldAtTableSeconds = 0.4f;

    private readonly Dictionary<string, Table_Part> parts = new Dictionary<string, Table_Part>();
    private Table_Part current;
    private Coroutine loop;

    private void Awake(){
        foreach (Table_Part part in PartsRoot.GetComponentsInChildren<Table_Part>(true))
            parts[part.Part_Id] = part;

        Sequencer.Step_Changed += OnStepChanged;
        Sequencer.Lesson_Completed += OnLessonCompleted;
    }

    // Tells the highlighters to leave this component alone — it announces itself by
    // flying into place. Components with no copy on the table return false and keep
    // their marker highlight.
    public bool Demonstrates(string markerId){
        return markerId != null
            && parts.TryGetValue(markerId, out Table_Part part)
            && part.Can_Glide;
    }

    private void OnStepChanged(Lesson_Step step, int index, int count){
        // Retry and the wrist reset call Sequencer.Begin() without reloading the scene,
        // so a restarted guided run has to put the parts back on the table itself.
        if (index == 0 && Sequencer.Mode == Lesson_Mode.Guided)
            Reset_All();

        Settle_Current();

        if (Sequencer.Mode != Lesson_Mode.Guided || step.Kind != Lesson_Step_Kind.Select_Component)
            return;

        if (!parts.TryGetValue(step.Target_Marker_Id, out Table_Part part) || !part.Can_Glide)
            return;

        current = part;
        current.Show_Mill_Source(false);
        loop = StartCoroutine(Loop(current));
    }

    private void OnLessonCompleted(Lesson_Result result){
        Settle_Current();
    }

    // The step advanced, so whatever was demonstrating flies home one last time and stays
    // there. Its mill source is left hidden — the clone is now standing in for it.
    private void Settle_Current(){
        if (loop != null){
            StopCoroutine(loop);
            loop = null;
        }

        if (current == null)
            return;

        Table_Part settling = current;
        current = null;
        StartCoroutine(Settle(settling));
    }

    private IEnumerator Settle(Table_Part part){
        yield return Glide(part, part.Glide_T, 1f);

        // Landed. Hand back to the real mill geometry and put the copy away: the copy is
        // parented to the parts table, so leaving it on show would strand it there while
        // the worktable and spindle animate the part it replaced.
        part.Show_Mill_Source(true);
        part.Show_Model(false);
    }

    private void Reset_All(){
        StopAllCoroutines();
        loop = null;
        current = null;

        foreach (Table_Part part in parts.Values){
            part.Show_Model(true);
            part.Set_Glide(0f);
            part.Show_Mill_Source(true);
        }
    }

    private IEnumerator Loop(Table_Part part){
        while (true){
            yield return Glide(part, 0f, 1f);
            yield return new WaitForSeconds(HoldAtMillSeconds);
            yield return Glide(part, 1f, 0f);
            yield return new WaitForSeconds(HoldAtTableSeconds);
        }
    }

    private IEnumerator Glide(Table_Part part, float from, float to){
        float duration = GlideSeconds * Mathf.Abs(to - from);

        if (duration <= 0f){
            part.Set_Glide(to);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration){
            elapsed += Time.deltaTime;
            part.Set_Glide(Mathf.SmoothStep(from, to, elapsed / duration));
            yield return null;
        }

        part.Set_Glide(to);
    }
}
