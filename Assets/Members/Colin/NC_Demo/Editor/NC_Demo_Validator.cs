using System.Collections.Generic;
using System.IO;
using ProMill8000;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Checks the open NC demo scene's wiring. Replaces the correctness-by-construction the deleted
/// scene builder provided: every reference below is dereferenced at Play, the axis mapping only
/// works if the three drives cover three different world axes, and a program that names a tool
/// the rack lacks alarms at run time. Findings are logged one per line.
/// </summary>
public static class NC_Demo_Validator
{
    [MenuItem("NC Demo/7 Validate Scene")]
    public static void Validate()
    {
        Scene scene = SceneManager.GetActiveScene();
        var errors = new List<string>();
        var warnings = new List<string>();

        var link = One<NC_Mill_Link>(errors);
        var runner = One<NC_Runner>(errors);
        var rack = One<NC_Tool_Rack>(errors);
        var table = One<NC_Output_Table>(errors);
        var job = One<NC_Job_Controller>(errors);
        var controls = One<NC_Demo_Controls>(errors);
        var readout = One<NC_Demo_Readout>(errors);

        if (job != null) Check_Job(job, rack, errors, warnings);
        if (link != null) Check_Link(link, job, errors);
        if (runner != null) Check_Refs(runner, new[] { "Link", "Rack" }, errors);
        if (rack != null) Check_Rack(rack, errors);
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

    static void Check_Job(NC_Job_Controller job, NC_Tool_Rack rack, List<string> errors, List<string> warnings)
    {
        Check_Refs(job, new[] { "Runner", "Link", "Rack", "Table", "Doors", "Machine", "Stock_Prefab", "Stock_Mount" }, errors);

        if (Ref(job, "Stock_Prefab") is Mill_Stock stock)
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
        var jobs = NC_Program_Library.Load_All(dir, settings);
        if (jobs.Count == 0) warnings.Add("no " + NC_Program_Library.Pattern + " files in '" + dir + "'.");
        foreach (var item in jobs)
        {
            if (item.Warning_Count > 0) warnings.Add(item.Name + " plans with " + item.Warning_Count + " warning(s)/alarm(s).");
            if (rack == null) continue;
            foreach (int t in item.Plan.Tools_Used)
                if (!rack.Has(t)) errors.Add(item.Name + " uses T" + t + ", which the rack does not have — the run would alarm.");
        }
    }
}
