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
    [Tooltip("An I_NC_Link: NC_Mill_Link or NC_Lathe_Link")]
    [SerializeField] private MonoBehaviour Link;
    [Tooltip("An I_NC_Tool_Changer: NC_Tool_Rack or NC_Lathe_Turret")]
    [SerializeField] private MonoBehaviour Rack;
    [SerializeField] private TMP_Text Text;
    [SerializeField] private string Title = "NC Mill Demo";
    [SerializeField] private float Refresh_Seconds = 0.1f;

    readonly StringBuilder sb = new StringBuilder();
    float next;
    I_NC_Link link;
    I_NC_Tool_Changer rack;

    void Awake()
    {
        link = Link as I_NC_Link;
        rack = Rack as I_NC_Tool_Changer;
        if (Job == null || Runner == null || link == null || rack == null || Text == null)
        {
            Debug.LogWarning("NC_Demo_Readout: Job, Runner, Link (I_NC_Link), Rack (I_NC_Tool_Changer) and Text must be assigned.");
            enabled = false;
        }
    }

    void Update()
    {
        if (Time.unscaledTime < next) return;
        next = Time.unscaledTime + Refresh_Seconds;

        var job = Job.Current;
        sb.Clear();
        sb.Append(Title).Append("   ");
        if (job != null) sb.Append("file ").Append(Job.Job_Index + 1).Append('/').Append(Job.Jobs.Count).Append("  ").Append(job.Name).Append("  (").Append(job.Program.Category).Append(")   ");
        sb.Append(Job.Current_Phase).Append("   ").Append(Runner.Current_State).Append("   override ").Append(Mathf.RoundToInt(Runner.Feed_Override * 100f)).Append('%').AppendLine();

        var cursor = Runner.Cursor;
        var move = cursor != null ? cursor.Current : null;
        if (move != null && job != null)
            sb.Append('N').Append(move.Line).Append("  ").Append(Block_Text(job.Program, move.Line)).Append("      move ").Append(cursor.Index + 1).Append('/').Append(Runner.Plan.Moves.Count);
        sb.AppendLine();

        if (link.Ready) sb.Append(link.Describe_Position());
        else sb.Append("position -.---");
        if (move != null)
            sb.Append("     F ").Append(move.Feed_Mm_Min.ToString("0")).Append("  S ").Append(move.Spindle_Rpm.ToString("0")).Append("  ").Append(rack.Describe(rack.Active)).Append("  spindle ").Append(Runner.Spindle_On ? "ON" : "off");
        sb.AppendLine();

        if (Job.Stock is Mill_Stock mill && mill.Data != null)
            sb.Append("stock ").Append(mill.Data.Nx).Append('x').Append(mill.Data.Nz).Append(" cells, ").Append(mill.Vertex_Count).Append(" verts, remesh ").Append(mill.Last_Remesh_Ms.ToString("0.0")).Append(" ms");
        else if (Job.Stock is Lathe_Stock lathe && lathe.Data != null)
            sb.Append("stock Ø").Append((lathe.Data.Radius * 2000f).ToString("0.#")).Append(" x ").Append((lathe.Data.Length * 1000f).ToString("0.#")).Append(" mm, ").Append(lathe.Vertex_Count).Append(" verts, remesh ").Append(lathe.Last_Remesh_Ms.ToString("0.0")).Append(" ms");
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
