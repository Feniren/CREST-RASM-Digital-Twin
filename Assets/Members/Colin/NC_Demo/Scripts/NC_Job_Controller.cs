using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// The job loop the Digital Twin's mill station will reuse: for every program in the library
/// folder (file-name order) — open the doors, load a fresh stock into the vise, close, run,
/// home, open, and set the carved block down on the output table under its category.
/// </summary>
public class NC_Job_Controller : MonoBehaviour
{
    public enum Phase { Idle, Loading_Stock, Running, Unloading, Done, No_Programs }

    [Header("Machine")]
    [SerializeField] private NC_Runner Runner;
    [SerializeField] private NC_Mill_Link Link;
    [SerializeField] private NC_Tool_Rack Rack;
    [SerializeField] private NC_Output_Table Table;
    [SerializeField] private Mill_Doors_Physics Doors;
    [SerializeField] private NC_Machine_Profile Machine;

    [Header("Stock")]
    [SerializeField] private Mill_Stock Stock_Prefab;
    [Tooltip("Rides the stock drives; the Work_Origin the link uses is authored under it")]
    [SerializeField] private Transform Stock_Mount;

    [Header("Programs")]
    [Tooltip("Folder under StreamingAssets holding the .nc files")]
    [SerializeField] private string Folder = "NC";

    public IReadOnlyList<NC_Job> Jobs => jobs;
    public int Job_Index { get; private set; } = -1;
    public NC_Job Current => Job_Index >= 0 && Job_Index < jobs.Count ? jobs[Job_Index] : null;
    public Phase Current_Phase { get; private set; }
    public Mill_Stock Stock { get; private set; }
    public string Library_Path => Path.Combine(Application.streamingAssetsPath, Folder);

    List<NC_Job> jobs = new List<NC_Job>();
    Coroutine run;

    void Awake()
    {
        if (Runner == null || Link == null || Rack == null || Table == null || Doors == null || Machine == null || Stock_Prefab == null || Stock_Mount == null)
        {
            Debug.LogWarning("NC_Job_Controller: every reference in the Machine and Stock groups must be assigned.");
            enabled = false;
        }
    }

    void Start()
    {
        Restart();
    }

    /// <summary>Clears the table and the mount, homes the mill, reloads the programs (fresh plans) and runs them all.</summary>
    public void Restart()
    {
        if (run != null) StopCoroutine(run);
        Runner.Unload();
        Table.Clear();
        foreach (var old in Stock_Mount.GetComponentsInChildren<Mill_Stock>()) Destroy(old.gameObject);   // never the authored Work_Origin
        Stock = null;
        Link.Reset_Home();
        Job_Index = -1;
        jobs = NC_Program_Library.Load_All(Library_Path, Machine.Settings);
        run = StartCoroutine(Run_All());
    }

    public void Skip()
    {
        Runner.Abort("skipped");
    }

    IEnumerator Run_All()
    {
        if (jobs.Count == 0)
        {
            Current_Phase = Phase.No_Programs;
            Debug.LogWarning("NC_Job_Controller: no " + NC_Program_Library.Pattern + " files in " + Library_Path);
            yield break;
        }
        for (Job_Index = 0; Job_Index < jobs.Count; Job_Index++)
        {
            var job = jobs[Job_Index];
            foreach (var n in job.Program.Notices) Debug.LogWarning("NC_Job_Controller: " + job.Name + ": " + n);
            foreach (var n in job.Plan.Notices)
                if (n.Severity != NC_Notice.Level.Info) Debug.LogWarning("NC_Job_Controller: " + job.Name + ": " + n);
            Debug.Log("NC_Job_Controller: " + job.Name + " (" + job.Program.Category + "): " + job.Plan.Moves.Count + " moves, cut " + job.Plan.Cut_Mm.ToString("0")
                      + " mm, rapid " + job.Plan.Rapid_Mm.ToString("0") + " mm, est " + job.Plan.Estimated_Seconds.ToString("0") + " s, tools " + string.Join(" ", job.Plan.Tools_Used));

            Current_Phase = Phase.Loading_Stock;
            yield return Set_Doors(true);
            Stock = Instantiate(Stock_Prefab, Stock_Mount);            // the prefab's local pose is identity: block in the vise
            Stock.name = "Stock_" + Path.GetFileNameWithoutExtension(job.Name);
            yield return Set_Doors(false);

            Current_Phase = Phase.Running;
            Runner.Load(job.Plan, Stock);
            Runner.Play();
            yield return new WaitUntil(() => Runner.Current_State == NC_Runner.State.Done);

            Current_Phase = Phase.Unloading;
            yield return Set_Doors(true);
            Stock.enabled = false;                                     // the mesh stays; no more remesh checks
            Table.Place(Stock.gameObject, job.Program.Category, Label(job));
            Debug.Log("NC_Job_Controller: " + job.Name + " done, cut " + Runner.Cut_Seconds.ToString("0") + " s"
                      + (Runner.Alarm_Text != null ? ", ALARM " + Runner.Alarm_Text : "") + ", " + job.Warning_Count + " warnings");
            Stock = null;
        }
        Current_Phase = Phase.Done;
        run = null;
    }

    // Toggles the doors if needed and waits for the slide to finish, however long it is tuned to take.
    IEnumerator Set_Doors(bool open)
    {
        if (Doors.IsOpen != open) Doors.AlternateInteract(null);   // the door script does not use the player argument
        yield return new WaitUntil(() => !Doors.IsSliding);
    }

    string Label(NC_Job job)
    {
        var sb = new StringBuilder(job.Name).Append('\n');
        bool first = true;
        foreach (int t in job.Plan.Tools_Used)
        {
            if (!first) sb.Append("  ");
            sb.Append(Rack.Describe(t));
            first = false;
        }
        sb.Append('\n');
        int s = Mathf.RoundToInt(Runner.Cut_Seconds);
        sb.Append(s / 60).Append(':').Append((s % 60).ToString("00")).Append(" cut");
        if (Runner.Alarm_Text != null) sb.Append("  ALARM: ").Append(Runner.Alarm_Text);
        else if (job.Warning_Count > 0) sb.Append("  ").Append(job.Warning_Count).Append(job.Warning_Count == 1 ? " warning" : " warnings");
        return sb.ToString();
    }
}
