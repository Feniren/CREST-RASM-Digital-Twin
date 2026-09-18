using System.Collections;
using System.IO;
using UnityEngine;
using ProMill8000;

public class Mill_Demo_Controller : Training_Demo_Controller{
    [Header("Mill (machine-axis naming: X table, Y saddle, Z spindle)")]
    [SerializeField] private AxisMovement WorktableX;
    [SerializeField] private AxisMovement SaddleY;
    [SerializeField] private AxisMovement SpindleZ;
    [SerializeField] private Mill_Doors_Physics Doors;

    // The milling demo runs a real NC program through the NC demo's runtime (see
    // Assets/Members/Colin/NC_Demo/README.md): the stock is authored under Stock_Mount in
    // the vise, and the drives, tool holder and work origin are wired on NC_Mill_Link.
    [Header("NC milling demo")]
    [SerializeField] private NC_Runner Runner;
    [SerializeField] private NC_Mill_Link Link;
    [SerializeField] private NC_Machine_Profile Machine;
    [SerializeField] private Mill_Stock Stock;
    [SerializeField] private string Program_File = "01_pocket.nc";   // under StreamingAssets/NC

    [Header("Axis indicators")]
    [SerializeField] private GameObject XIndicator;
    [SerializeField] private GameObject YIndicator;
    [SerializeField] private GameObject ZIndicator;

    // Hand-authored on the Lesson_Manager in Module1_Overview.unity; the values
    // below are only defaults for a newly added controller.
    [Header("Travel (meters)")]
    [SerializeField] private float XTravel = 0.14f;
    [SerializeField] private float YTravel = 0.04f;
    [SerializeField] private float ZTravel = 0.27f;

    private void Awake(){
        if (Doors == null || Runner == null || Link == null || Machine == null || Stock == null){
            Debug.LogWarning("Mill_Demo_Controller: Doors and every NC milling demo reference must be assigned.");
            enabled = false;
        }
    }

    public override void Play(Lesson_Step step){
        if (!enabled)
            return;

        Stop();   // a re-entered demo must not fight one still in progress

        if (step.Kind == Lesson_Step_Kind.Axis_Demo)
            Play_Axis_Demo();
        else if (step.Kind == Lesson_Step_Kind.Milling_Demo)
            Play_Milling_Demo();
        else
            Debug.LogWarning($"Mill_Demo_Controller: no demo for step kind {step.Kind}");
    }

    // Also called by Lesson_Sequencer.Begin on every (re)start, e.g. a wrist reset mid-cut.
    public override void Stop(){
        if (!enabled)
            return;

        StopAllCoroutines();
        Runner.Unload();
        Link.Reset_Home();

        foreach (GameObject indicator in new[]{ XIndicator, YIndicator, ZIndicator })
            if (indicator != null)
                indicator.SetActive(false);
    }

    public void Play_Axis_Demo(){
        StartCoroutine(AxisDemo());
    }

    public void Play_Milling_Demo(){
        StartCoroutine(MillingDemo());
    }

    private IEnumerator AxisDemo(){
        yield return RunAxis(XIndicator, WorktableX, new[]{ XTravel, -XTravel, 0f });
        yield return RunAxis(YIndicator, SaddleY, new[]{ YTravel, -YTravel, 0f });
        yield return RunAxis(ZIndicator, SpindleZ, new[]{ -ZTravel, 0f });
        Raise_Demo_Finished();
    }

    // Guard closed, cut, guard open so the trainee can see the pocket. The runner homes the
    // machine itself before reporting Done, and every alarm/abort path ends there too.
    private IEnumerator MillingDemo(){
        yield return Set_Doors(false);
        Stock.Reset_Stock();   // solid again after a restart

        NC_Job job = NC_Program_Library.Load(
            Path.Combine(Application.streamingAssetsPath, "NC", Program_File), Machine.Settings);
        Runner.Load(job.Plan, Stock);   // mounts the program's first tool from the rack
        Runner.Play();

        while (Runner.Current_State != NC_Runner.State.Done)
            yield return null;

        yield return Set_Doors(true);
        Raise_Demo_Finished();
    }

    private IEnumerator Set_Doors(bool open){
        Doors.Set_Open(open);
        yield return new WaitUntil(() => !Doors.IsSliding);
    }

    private IEnumerator RunAxis(GameObject indicator, AxisMovement axis, float[] offsets){
        if (indicator != null)
            indicator.SetActive(true);

        foreach (float offset in offsets){
            axis.MoveToOffset(offset);

            while (axis.IsMoving)
                yield return null;
        }

        if (indicator != null)
            indicator.SetActive(false);
    }
}
