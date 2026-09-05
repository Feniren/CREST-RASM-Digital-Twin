using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The job loop: for every .nc in StreamingAssets/NC (sorted) — open the doors, teleport a
/// fresh stock into the vise, close, run the program, home, open, and set the carved block down
/// on the output table under its category. Keyboard: Space pause, N skip file, R restart,
/// [ ] feed override, 1/2/3 camera views. OnGUI shows the CNCBase-style readout.
/// </summary>
public class NC_Demo_Controller : MonoBehaviour
{
    public NC_Runner Runner;
    public NC_Mill_Link Link;
    public NC_Tool_Rack Rack;
    public NC_Output_Table Table;
    public Mill_Doors_Physics Doors;
    public GameObject Stock_Prefab;
    public Transform Stock_Mount;
    public Transform[] Views;
    public string Folder = "NC";
    public float Door_Seconds = 0.9f;

    string[] files = new string[0];
    int file_index = -1;
    NC_Program program;
    NC_Plan plan;
    Mill_Stock stock;
    string status = "";
    int view;
    Coroutine run;
    readonly StringBuilder text = new StringBuilder();
    GUIStyle style;

    void Start()
    {
        Application.runInBackground = true;   // an unattended demo must not stall when the window loses focus
        Restart();
    }

    public void Restart()
    {
        if (run != null) StopCoroutine(run);
        Runner.Unload();
        Table.Clear();
        for (int i = Stock_Mount.childCount - 1; i >= 0; i--) Destroy(Stock_Mount.GetChild(i).gameObject);
        stock = null;
        Link.Reset_Home();
        file_index = -1;
        program = null;
        plan = null;
        run = StartCoroutine(Run_All());
    }

    public void Skip()
    {
        Runner.Abort("skipped");
    }

    public void Next_View()
    {
        if (Views == null || Views.Length == 0) return;
        Set_View((view + 1) % Views.Length);
    }

    public void Set_View(int i)
    {
        if (Views == null || i < 0 || i >= Views.Length || Views[i] == null) return;
        view = i;
        var cam = Camera.main;
        if (cam != null) cam.transform.SetPositionAndRotation(Views[i].position, Views[i].rotation);
    }

    IEnumerator Run_All()
    {
        string dir = Path.Combine(Application.streamingAssetsPath, Folder);
        files = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.nc").OrderBy(f => f).ToArray() : new string[0];
        if (files.Length == 0)
        {
            status = "no .nc files in " + dir;
            Debug.LogWarning("NC: " + status);
            yield break;
        }

        for (file_index = 0; file_index < files.Length; file_index++)
        {
            string name = Path.GetFileName(files[file_index]);
            program = NC_Parser.Parse(name, File.ReadAllText(files[file_index]));
            plan = NC_Planner.Plan(program);
            foreach (var n in program.Notices) Debug.LogWarning("NC " + name + ": " + n);
            foreach (var n in plan.Notices) Debug.LogWarning("NC " + name + ": " + n);
            Debug.Log("NC: " + name + " (" + program.Category + "): " + plan.Moves.Count + " moves, cut " + plan.Cut_Mm.ToString("0") + " mm, rapid "
                      + plan.Rapid_Mm.ToString("0") + " mm, est " + plan.Estimated_Seconds.ToString("0") + " s, tools " + string.Join(" ", plan.Tools_Used));

            status = "loading stock";
            yield return Set_Doors(true);
            var go = Instantiate(Stock_Prefab, Stock_Mount);
            go.name = "Stock_" + Path.GetFileNameWithoutExtension(name);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            stock = go.GetComponent<Mill_Stock>();
            Link.Work_Origin = stock.transform.Find("Work_Origin");
            yield return Set_Doors(false);

            status = "running";
            Runner.Load(plan, stock);
            Runner.Play();
            yield return new WaitUntil(() => Runner.Current_State == NC_Runner.State.Done);

            status = "unloading";
            yield return Set_Doors(true);
            stock.enabled = false;                       // meshes stay, no more remesh checks
            int warnings = program.Notices.Count + plan.Notices.Count;
            Table.Place(go, program.Category, Label(name, plan, warnings));
            Debug.Log("NC: " + name + " done, cut " + Runner.Cut_Seconds.ToString("0") + " s" + (Runner.Alarm_Text != null ? ", ALARM " + Runner.Alarm_Text : "") + ", " + warnings + " warnings");
            stock = null;
        }
        status = "done";
    }

    IEnumerator Set_Doors(bool open)
    {
        if (Doors == null || Doors.IsOpen == open) yield break;
        Doors.AlternateInteract(null);
        yield return new WaitForSeconds(Door_Seconds);
    }

    string Label(string name, NC_Plan p, int warnings)
    {
        var sb = new StringBuilder(name).Append('\n');
        sb.Append(string.Join("  ", p.Tools_Used.Select(t => Rack.Describe(t)))).Append('\n');
        int s = Mathf.RoundToInt(Runner.Cut_Seconds);
        sb.Append(s / 60).Append(':').Append((s % 60).ToString("00")).Append(" cut");
        if (Runner.Alarm_Text != null) sb.Append("  ALARM: ").Append(Runner.Alarm_Text);
        else if (warnings > 0) sb.Append("  ").Append(warnings).Append(warnings == 1 ? " warning" : " warnings");
        return sb.ToString();
    }

    void Update()
    {
        if (run == null && status != "done" && status != "") status = "stopped by a script reload; press R to restart";
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.spaceKey.wasPressedThisFrame) Runner.Toggle_Pause();
        if (kb.nKey.wasPressedThisFrame) Skip();
        if (kb.rKey.wasPressedThisFrame) Restart();
        if (kb.leftBracketKey.wasPressedThisFrame) Runner.Feed_Override = Mathf.Max(0.25f, Runner.Feed_Override * 0.5f);
        if (kb.rightBracketKey.wasPressedThisFrame) Runner.Feed_Override = Mathf.Min(10f, Runner.Feed_Override * 2f);
        if (kb.digit1Key.wasPressedThisFrame) Set_View(0);
        if (kb.digit2Key.wasPressedThisFrame) Set_View(1);
        if (kb.digit3Key.wasPressedThisFrame) Set_View(2);
    }

    void OnGUI()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 13, richText = false };
            style.normal.textColor = Color.white;
        }
        text.Clear();
        text.Append("NC Mill Demo   ");
        if (program != null) text.Append("file ").Append(file_index + 1).Append('/').Append(files.Length).Append("  ").Append(program.Name).Append("  (").Append(program.Category).Append(")   ");
        text.Append(status).Append("   ").Append(Runner.Current_State).Append("   override ").Append(Mathf.RoundToInt(Runner.Feed_Override * 100f)).Append('%').AppendLine();

        var cursor = Runner.Cursor;
        var move = cursor != null ? cursor.Current : null;
        if (move != null && program != null)
        {
            text.Append("N").Append(move.Line).Append("  ").Append(Block_Text(move.Line)).Append("      move ").Append(cursor.Index + 1).Append('/').Append(Runner.Plan.Moves.Count).AppendLine();
        }
        else text.AppendLine();

        if (Link.Ready)
        {
            Vector3 w = Link.Work_Position_Mm;
            text.Append("X ").Append(w.x.ToString("0.000").PadLeft(8)).Append("   Y ").Append(w.y.ToString("0.000").PadLeft(8)).Append("   Z ").Append(w.z.ToString("0.000").PadLeft(8));
        }
        else text.Append("X    -.---   Y    -.---   Z    -.---");
        if (move != null)
            text.Append("     F ").Append(move.Feed_Mm_Min.ToString("0")).Append("  S ").Append(move.Spindle_Rpm.ToString("0")).Append("  ").Append(Rack.Describe(Rack.Active)).Append("  spindle ").Append(Runner.Spindle_On ? "ON" : "off");
        text.AppendLine();

        if (stock != null && stock.Data != null)
            text.Append("stock ").Append(stock.Data.Nx).Append('x').Append(stock.Data.Nz).Append(" cells, ").Append(stock.Vertex_Count).Append(" verts, remesh ").Append(stock.Last_Remesh_Ms.ToString("0.0")).Append(" ms");
        if (Runner.Alarm_Text != null) text.Append("   ALARM: ").Append(Runner.Alarm_Text);
        text.AppendLine();
        text.Append("Space pause   N skip file   R restart   [ ] feed override   1/2/3 views   (click the Game view so keys register)");
        GUI.Label(new Rect(10f, 10f, 900f, 100f), text.ToString(), style);
    }

    string Block_Text(int line)
    {
        var blocks = program.Blocks;
        for (int i = 0; i < blocks.Count; i++)
            if (blocks[i].Line == line) return blocks[i].Text;
        return "";
    }
}
