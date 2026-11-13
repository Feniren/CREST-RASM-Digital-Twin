using System.Collections.Generic;
using System.IO;
using ProMill8000;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Checks the open NC demo scene's wiring (NC_Mill_Demo or NC_Lathe_Demo; the machine is picked
/// by which link the scene has). Replaces the correctness-by-construction the deleted scene
/// builder provided: every reference below is dereferenced at Play, the axis mapping only works
/// if the drives cover different world axes, and a program that names a tool the rack or turret
/// lacks alarms at run time. Findings are logged one per line.
/// </summary>
public static class NC_Demo_Validator
{
    [MenuItem("NC Demo/7 Validate Scene")]
    public static void Validate()
    {
        Scene scene = SceneManager.GetActiveScene();
        var errors = new List<string>();
        var warnings = new List<string>();

        bool lathe = Object.FindObjectsByType<NC_Lathe_Link>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0;
        var runner = One<NC_Runner>(errors);
        var table = One<NC_Output_Table>(errors);
        var job = One<NC_Job_Controller>(errors);
        var controls = One<NC_Demo_Controls>(errors);
        var readout = One<NC_Demo_Readout>(errors);

        I_NC_Tool_Changer changer;
        if (lathe)
        {
            var link = One<NC_Lathe_Link>(errors);
            var turret = One<NC_Lathe_Turret>(errors);
            changer = turret;
            if (link != null) Check_Lathe_Link(link, job, errors);
            if (turret != null && link != null) Check_Turret(turret, link, errors);
        }
        else
        {
            var link = One<NC_Mill_Link>(errors);
            var rack = One<NC_Tool_Rack>(errors);
            changer = rack;
            if (link != null) Check_Link(link, job, errors);
            if (rack != null) Check_Rack(rack, errors);
        }
        if (job != null) Check_Job(job, changer, lathe, errors, warnings);
        if (runner != null) Check_Refs(runner, new[] { "Link", "Rack" }, errors);
        if (table != null) Check_Table(table, errors);
        if (controls != null)
        {
            Check_Refs(controls, new[] { "Job", "Runner", "View_Camera" }, errors);
            if (new SerializedObject(controls).FindProperty("Views").arraySize == 0) warnings.Add("NC_Demo_Controls.Views is empty — the 1/2/3 keys do nothing.");
        }
        if (readout != null) Check_Refs(readout, new[] { "Job", "Runner", "Link", "Rack", "Text" }, errors);

        foreach (string error in errors) Debug.LogError("NC_Demo_Validator: " + error);
        foreach (string warning in warnings) Debug.LogWarning("NC_Demo_Validator: " + warning);
        if (errors.Count == 0 && warnings.Count == 0) Debug.Log("NC_Demo_Validator: '" + scene.name + "' OK.");
        else Debug.Log("NC_Demo_Validator: '" + scene.name + "' — " + errors.Count + " error(s), " + warnings.Count + " warning(s).");
    }

    static T One<T>(List<string> errors) where T : Component
    {
        var found = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (found.Length == 1) return found[0];
        errors.Add("expected exactly one " + typeof(T).Name + ", found " + found.Length + ".");
        return found.Length > 0 ? found[0] : null;
    }

    static void Check_Refs(Component target, string[] fields, List<string> errors)
    {
        var so = new SerializedObject(target);
        foreach (string field in fields)
        {
            var prop = so.FindProperty(field);
            if (prop == null) errors.Add(target.GetType().Name + " has no field '" + field + "' — this validator is out of date with the runtime.");
            else if (prop.objectReferenceValue == null) errors.Add(target.GetType().Name + "." + field + " is empty.");
        }
    }

    static Object Ref(Component target, string field)
    {
        var prop = new SerializedObject(target).FindProperty(field);
        return prop != null ? prop.objectReferenceValue : null;
    }

    static void Check_Link(NC_Mill_Link link, NC_Job_Controller job, List<string> errors)
    {
        Check_Refs(link, new[] { "Table_X", "Saddle_Y", "Spindle_Z", "Tool_Holder", "Work_Origin" }, errors);

        var directions = new List<Vector3>();
        foreach (string field in new[] { "Table_X", "Saddle_Y", "Spindle_Z" })
            if (Ref(link, field) is AxisMovement drive) directions.Add(drive.WorldDirection);
        bool perpendicular = true;
        for (int i = 0; i < directions.Count; i++)
            for (int j = i + 1; j < directions.Count; j++)
                perpendicular &= Mathf.Abs(Vector3.Dot(directions[i], directions[j])) < 1e-3f;
        if (directions.Count < 3 || !perpendicular) errors.Add("NC_Mill_Link: the three drives must move along three mutually perpendicular directions (check AxisMovement.Axis on each drive).");

        if (job == null || !(Ref(link, "Work_Origin") is Transform origin)) return;
        var mount = Ref(job, "Stock_Mount") as Transform;
        if (mount != null && origin.parent != mount) errors.Add("NC_Mill_Link.Work_Origin must be a child of NC_Job_Controller.Stock_Mount so it rides the stock drives.");
        if (Ref(job, "Stock_Prefab") is Mill_Stock stock)
        {
            Vector3 size = new SerializedObject(stock).FindProperty("Size").vector3Value;
            var expected = new Vector3(0f, size.y, size.z);   // the stock's front-left-top corner with the stock at the mount's origin
            if (Vector3.Distance(origin.localPosition, expected) > 1e-4f)
                errors.Add("NC_Mill_Link.Work_Origin sits at " + origin.localPosition.ToString("F4") + " but the stock's front-left-top corner is " + expected.ToString("F4") + ".");
        }
    }

    static void Check_Lathe_Link(NC_Lathe_Link link, NC_Job_Controller job, List<string> errors)
    {
        Check_Refs(link, new[] { "Cross_X", "Carriage_Z", "Spindle", "Work_Origin" }, errors);
        var x = Ref(link, "Cross_X") as AxisMovement;
        var z = Ref(link, "Carriage_Z") as AxisMovement;
        var spindle = Ref(link, "Spindle") as Transform;
        var origin = Ref(link, "Work_Origin") as Transform;
        if (x == null || z == null || spindle == null || origin == null) return;

        if (Mathf.Abs(Vector3.Dot(x.WorldDirection, z.WorldDirection)) > 1e-3f) errors.Add("NC_Lathe_Link: Cross_X and Carriage_Z must move along perpendicular directions (check AxisMovement.Axis).");
        if (Vector3.Angle(z.WorldDirection, spindle.forward) > 0.1f && Vector3.Angle(-z.WorldDirection, spindle.forward) > 0.1f) errors.Add("NC_Lathe_Link: Carriage_Z must move parallel to the spindle axis (Spindle local Z).");
        if (Vector3.Angle(origin.forward, spindle.forward) > 0.1f) errors.Add("NC_Lathe_Link.Work_Origin local +Z must point along the spindle axis (Spindle local +Z, toward the tailstock).");
        if (Mathf.Abs(Vector3.Dot(origin.right, x.WorldDirection)) < 0.9999f) errors.Add("NC_Lathe_Link.Work_Origin local +X must lie along the Cross_X direction (program X is radial along the cross slide).");
        if (origin.IsChildOf(spindle)) errors.Add("NC_Lathe_Link.Work_Origin must not ride the Spindle — it would spin with it.");
        Vector3 off_axis = Vector3.ProjectOnPlane(origin.position - spindle.position, spindle.forward);
        if (off_axis.magnitude > 1e-4f) errors.Add("NC_Lathe_Link.Work_Origin is " + (off_axis.magnitude * 1000f).ToString("0.###") + " mm off the spindle axis.");

        if (job == null) return;
        var mount = Ref(job, "Stock_Mount") as Transform;
        if (mount != null && !mount.IsChildOf(spindle)) errors.Add("NC_Job_Controller.Stock_Mount must ride NC_Lathe_Link.Spindle so the bar spins with the chuck.");
        if (mount != null && Vector3.Angle(mount.forward, spindle.forward) > 0.1f) errors.Add("NC_Job_Controller.Stock_Mount local Z must be the spindle axis (the Lathe_Stock axis is its local +Z).");
        if (mount != null && Ref(job, "Stock_Prefab") is Lathe_Stock stock)
        {
            float length = new SerializedObject(stock).FindProperty("Length").floatValue;
            Vector3 face = mount.TransformPoint(new Vector3(0f, 0f, length));
            if (Vector3.Distance(face, origin.position) > 1e-4f)
                errors.Add("NC_Lathe_Link.Work_Origin should sit at the stock's free face " + face.ToString("F4") + " but is at " + origin.position.ToString("F4") + ".");
        }
    }

    // Every station, once presented, must put its tip in the Work_Origin's XZ plane with the tool pointing radially out (+X).
    static void Check_Turret(NC_Lathe_Turret turret, NC_Lathe_Link link, List<string> errors)
    {
        Check_Refs(turret, new[] { "Turret" }, errors);
        var head = Ref(turret, "Turret") as Transform;
        var origin = Ref(link, "Work_Origin") as Transform;
        var tools = new SerializedObject(turret).FindProperty("Tools");
        if (tools.arraySize == 0) errors.Add("NC_Lathe_Turret.Tools is empty.");
        if (head == null || origin == null) return;
        Quaternion saved = head.localRotation;
        for (int i = 0; i < tools.arraySize; i++)
        {
            var tool = tools.GetArrayElementAtIndex(i).objectReferenceValue as Carving_Tool;
            if (tool == null) { errors.Add("NC_Lathe_Turret.Tools[" + i + "] (T" + (i + 1) + ") is empty."); continue; }
            if (Ref(tool, "Definition") == null) errors.Add(tool.name + ": Definition is empty.");
            if (Ref(tool, "Visual") == null) errors.Add(tool.name + ": Visual is empty.");
            if (!tool.transform.IsChildOf(head)) errors.Add(tool.name + " must sit under the turret so it indexes with it.");
            turret.Index(i + 1);
            Vector3 local = origin.InverseTransformPoint(tool.transform.position);
            if (Mathf.Abs(local.y) > 1e-4f) errors.Add(tool.name + ": presented tip is " + (local.y * 1000f).ToString("0.###") + " mm off the Work_Origin XZ plane (centre height) — it could never reach its targets.");
            if (Vector3.Angle(tool.transform.up, origin.right) > 0.5f) errors.Add(tool.name + ": presented tool axis is " + Vector3.Angle(tool.transform.up, origin.right).ToString("0.#") + "° from radial (+X); the carve assumes a radial tool.");
        }
        head.localRotation = saved;
    }

    static void Check_Rack(NC_Tool_Rack rack, List<string> errors)
    {
        var so = new SerializedObject(rack);
        var tools = so.FindProperty("Tools");
        var slots = so.FindProperty("Slots");
        if (tools.arraySize == 0) errors.Add("NC_Tool_Rack.Tools is empty.");
        if (tools.arraySize != slots.arraySize) errors.Add("NC_Tool_Rack.Tools and Slots must have the same length.");
        for (int i = 0; i < tools.arraySize; i++)
        {
            var tool = tools.GetArrayElementAtIndex(i).objectReferenceValue as Carving_Tool;
            var slot = i < slots.arraySize ? slots.GetArrayElementAtIndex(i).objectReferenceValue as Transform : null;
            if (tool == null) { errors.Add("NC_Tool_Rack.Tools[" + i + "] (T" + (i + 1) + ") is empty."); continue; }
            if (slot == null) { errors.Add("NC_Tool_Rack.Slots[" + i + "] is empty."); continue; }
            if (Ref(tool, "Definition") == null) errors.Add(tool.name + ": Definition is empty.");
            if (Ref(tool, "Visual") == null) errors.Add(tool.name + ": Visual is empty.");
            if (tool.transform.parent != slot) errors.Add(tool.name + " should start parked under " + slot.name + ".");
        }
    }

    static void Check_Table(NC_Output_Table table, List<string> errors)
    {
        var so = new SerializedObject(table);
        var rows = so.FindProperty("Rows");
        if (rows.arraySize == 0) errors.Add("NC_Output_Table.Rows is empty.");
        for (int r = 0; r < rows.arraySize; r++)
        {
            var row = rows.GetArrayElementAtIndex(r);
            if (row.FindPropertyRelative("Label").objectReferenceValue == null) errors.Add("NC_Output_Table.Rows[" + r + "].Label is empty.");
            var slots = row.FindPropertyRelative("Slots");
            if (slots.arraySize == 0) errors.Add("NC_Output_Table.Rows[" + r + "] has no slots.");
            for (int s = 0; s < slots.arraySize; s++)
                if (slots.GetArrayElementAtIndex(s).objectReferenceValue == null) errors.Add("NC_Output_Table.Rows[" + r + "].Slots[" + s + "] is empty.");
        }
        var label = so.FindProperty("Part_Label_Prefab").objectReferenceValue;
        if (label == null) errors.Add("NC_Output_Table.Part_Label_Prefab is empty.");
        else if (!PrefabUtility.IsPartOfPrefabAsset(label)) errors.Add("NC_Output_Table.Part_Label_Prefab must be a prefab asset, not a scene object.");
    }

    static void Check_Job(NC_Job_Controller job, I_NC_Tool_Changer rack, bool lathe, List<string> errors, List<string> warnings)
    {
        Check_Refs(job, lathe ? new[] { "Runner", "Link", "Rack", "Table", "Machine", "Stock_Prefab", "Stock_Mount" }
                              : new[] { "Runner", "Link", "Rack", "Table", "Doors", "Machine", "Stock_Prefab", "Stock_Mount" }, errors);
        var job_so = new SerializedObject(job);
        bool lathe_planner = job_so.FindProperty("Planner").enumValueIndex == (int)NC_Job_Controller.Planner_Kind.Lathe;
        if (lathe_planner != lathe) errors.Add("NC_Job_Controller.Planner must be " + (lathe ? "Lathe" : "Mill") + " for this machine.");
        var prefab = Ref(job, "Stock_Prefab");
        if (prefab != null && (lathe ? !(prefab is Lathe_Stock) : !(prefab is Mill_Stock))) errors.Add("NC_Job_Controller.Stock_Prefab must be a " + (lathe ? "Lathe_Stock" : "Mill_Stock") + ".");

        if (prefab is Behaviour stock)
        {
            if (stock.GetComponent<MeshFilter>() == null) errors.Add("Stock_Prefab has no MeshFilter.");
            var renderer = stock.GetComponent<MeshRenderer>();
            if (renderer == null || renderer.sharedMaterial == null) errors.Add("Stock_Prefab's MeshRenderer has no material.");
        }

        string folder = new SerializedObject(job).FindProperty("Folder").stringValue;
        string dir = Path.Combine(Application.streamingAssetsPath, folder);
        if (!Directory.Exists(dir))
        {
            errors.Add("program folder '" + dir + "' does not exist.");
            return;
        }
        var settings = Ref(job, "Machine") is NC_Machine_Profile machine ? machine.Settings : new NC_Machine_Settings();
        var jobs = lathe ? NC_Program_Library.Load_All(dir, settings, NC_Lathe_Planner.Plan) : NC_Program_Library.Load_All(dir, settings, NC_Planner.Plan);
        if (jobs.Count == 0) warnings.Add("no " + NC_Program_Library.Pattern + " files in '" + dir + "'.");
        foreach (var item in jobs)
        {
            if (item.Warning_Count > 0) warnings.Add(item.Name + " plans with " + item.Warning_Count + " warning(s)/alarm(s).");
            if (rack == null) continue;
            foreach (int t in item.Plan.Tools_Used)
                if (!rack.Has(t)) errors.Add(item.Name + " uses T" + t + ", which the " + (lathe ? "turret" : "rack") + " does not have — the run would alarm.");
        }
    }
}
