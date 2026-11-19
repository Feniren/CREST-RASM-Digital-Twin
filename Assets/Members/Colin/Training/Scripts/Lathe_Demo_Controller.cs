using System.Collections;
using System.IO;
using UnityEngine;

// Lathe Module 1's turning demo: a real NC program run through the NC demo's runtime (see
// Assets/Members/Colin/NC_Demo/README.md), mirroring Mill_Demo_Controller. The bar stock is
// authored under Lathe_NC/Spindle/Stock_Mount, and the drives, turret and work origin are wired
// on NC_Lathe_Link / NC_Lathe_Turret exactly as in NC_Lathe_Demo.unity.
public class Lathe_Demo_Controller : Training_Demo_Controller{
    [SerializeField] private Lathe_Doors Doors;

    [Header("NC turning demo")]
    [SerializeField] private NC_Runner Runner;
    [SerializeField] private NC_Lathe_Link Link;
    [SerializeField] private NC_Machine_Profile Machine;
    [SerializeField] private Lathe_Stock Stock;
    [SerializeField] private string Program_File = "04_part_off.nc";   // under StreamingAssets/NC_Lathe

    private void Awake(){
        if (Doors == null || Runner == null || Link == null || Machine == null || Stock == null){
            Debug.LogWarning("Lathe_Demo_Controller: Doors and every NC turning demo reference must be assigned.");
            enabled = false;
        }
    }

    public override void Play(Lesson_Step step){
        if (!enabled)
            return;

        Stop();   // a re-entered demo must not fight one still in progress

        if (step.Kind == Lesson_Step_Kind.Turning_Demo)
            StartCoroutine(TurningDemo());
        else
            Debug.LogWarning($"Lathe_Demo_Controller: no demo for step kind {step.Kind}");
    }

    // Also called by Lesson_Sequencer.Begin on every (re)start, e.g. a wrist reset mid-cut.
    public override void Stop(){
        if (!enabled)
            return;

        StopAllCoroutines();
        Runner.Unload();
        Link.Reset_Home();
        Stock.Reset_Stock();   // a restarted tour shows a full bar, not a half-cut one
        if (!Doors.IsOpen)
            Doors.Set_Open(true);
    }

    // Guard closed, turn, guard open so the trainee can see the finished part. The runner homes
    // the machine itself before reporting Done, and every alarm/abort path ends there too.
    private IEnumerator TurningDemo(){
        yield return Set_Doors(false);

        NC_Job job = NC_Program_Library.Load(
            Path.Combine(Application.streamingAssetsPath, "NC_Lathe", Program_File), Machine.Settings, NC_Lathe_Planner.Plan);
        Runner.Load(job.Plan, Stock);   // mounts the program's first tool on the turret
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
}
