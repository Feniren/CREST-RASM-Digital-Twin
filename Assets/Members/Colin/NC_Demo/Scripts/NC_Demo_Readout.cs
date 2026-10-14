using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// CNCBase-style readout of the running job: file, current block, work position, feed and
/// speed, tool, stock statistics. Rebuilt into one reused StringBuilder a few times a second.
/// </summary>
public class NC_Demo_Readout : MonoBehaviour
{
    [SerializeField] private NC_Job_Controller Job;
    [SerializeField] private NC_Runner Runner;
    [SerializeField] private NC_Mill_Link Link;
    [SerializeField] private NC_Tool_Rack Rack;
    [SerializeField] private TMP_Text Text;
    [SerializeField] private float Refresh_Seconds = 0.1f;

    readonly StringBuilder sb = new StringBuilder();
    float next;

    void Awake()
    {
        if (Job == null || Runner == null || Link == null || Rack == null || Text == null)
        {
            Debug.LogWarning("NC_Demo_Readout: Job, Runner, Link, Rack and Text must be assigned.");
            enabled = false;
        }
    }

    void Update()
    {
        if (Time.unscaledTime < next) return;
        next = Time.unscaledTime + Refresh_Seconds;

        var job = Job.Current;
        sb.Clear();
        sb.Append("NC Mill Demo   ");
        if (job != null) sb.Append("file ").Append(Job.Job_Index + 1).Append('/').Append(Job.Jobs.Count).Append("  ").Append(job.Name).Append("  (").Append(job.Program.Category).Append(")   ");
        sb.Append(Job.Current_Phase).Append("   ").Append(Runner.Current_State).Append("   override ").Append(Mathf.RoundToInt(Runner.Feed_Override * 100f)).Append('%').AppendLine();

        var cursor = Runner.Cursor;
        var move = cursor != null ? cursor.Current : null;
        if (move != null && job != null)
            sb.Append('N').Append(move.Line).Append("  ").Append(Block_Text(job.Program, move.Line)).Append("      move ").Append(cursor.Index + 1).Append('/').Append(Runner.Plan.Moves.Count);
        sb.AppendLine();

        if (Link.Ready)
        {
            Vector3 w = Link.Work_Position_Mm;
            sb.Append("X ").Append(w.x.ToString("0.000").PadLeft(8)).Append("   Y ").Append(w.y.ToString("0.000").PadLeft(8)).Append("   Z ").Append(w.z.ToString("0.000").PadLeft(8));
        }
        else sb.Append("X    -.---   Y    -.---   Z    -.---");
        if (move != null)
            sb.Append("     F ").Append(move.Feed_Mm_Min.ToString("0")).Append("  S ").Append(move.Spindle_Rpm.ToString("0")).Append("  ").Append(Rack.Describe(Rack.Active)).Append("  spindle ").Append(Runner.Spindle_On ? "ON" : "off");
        sb.AppendLine();

        var stock = Job.Stock;
        if (stock != null && stock.Data != null)
            sb.Append("stock ").Append(stock.Data.Nx).Append('x').Append(stock.Data.Nz).Append(" cells, ").Append(stock.Vertex_Count).Append(" verts, remesh ").Append(stock.Last_Remesh_Ms.ToString("0.0")).Append(" ms");
        if (Runner.Alarm_Text != null) sb.Append("   ALARM: ").Append(Runner.Alarm_Text);
        Text.SetText(sb);
    }

    static string Block_Text(NC_Program program, int line)
    {
        var blocks = program.Blocks;
        for (int i = 0; i < blocks.Count; i++)
            if (blocks[i].Line == line) return blocks[i].Text;
        return "";
    }
}
