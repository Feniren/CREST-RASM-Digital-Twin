using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Play-mode debug drivers for the NC demo, usable over MCP (which cannot press keys or move
/// transforms in Play mode). They call the same methods the keyboard controls do.
/// </summary>
public static class NC_Demo_Debug_Menu
{
    [MenuItem("NC Demo/8 Debug - Feed Override 1000%")]
    static void Override_1000()
    {
        var r = Find<NC_Runner>();
        if (r != null) r.Feed_Override = 10f;
    }

    [MenuItem("NC Demo/8 Debug - Feed Override 100%")]
    static void Override_100()
    {
        var r = Find<NC_Runner>();
        if (r != null) r.Feed_Override = 1f;
    }

    [MenuItem("NC Demo/8 Debug - Skip File")]
    static void Skip_File()
    {
        var j = Find<NC_Job_Controller>();
        if (j != null) j.Skip();
    }

    [MenuItem("NC Demo/8 Debug - Restart")]
    static void Restart()
    {
        var j = Find<NC_Job_Controller>();
        if (j != null) j.Restart();
    }

    [MenuItem("NC Demo/8 Debug - Next View")]
    static void Next_View()
    {
        var c = Find<NC_Demo_Controls>();
        if (c != null) c.Next_View();
    }

    static int next_program;

    // DigitalTwin: stands in for the job prefabs and Job Select buttons that do not exist yet.
    [MenuItem("NC Demo/8 Debug - Queue Mill Job (Next Program)")]
    static void Queue_Next_Program()
    {
        string[] files = Directory.GetFiles(Path.Combine(Application.streamingAssetsPath, "NC"), NC_Program_Library.Pattern);
        Array.Sort(files, StringComparer.Ordinal);
        Queue_Mill_Job(Path.GetFileName(files[next_program++ % files.Length]));
    }

    [MenuItem("NC Demo/8 Debug - Queue Mill Job (Missing File)")]
    static void Queue_Missing_File()
    {
        Queue_Mill_Job("missing.nc");
    }

    static void Queue_Mill_Job(string file)
    {
        var manager = Find<Job_Manager>();
        if (manager == null) return;
        var job = new GameObject("Job_NC_" + file).AddComponent<Job_NC_Program>();
        job.ProgramFile = file;
        job.Name = "NC " + Path.GetFileNameWithoutExtension(file);
        job.StockReference = AssetDatabase.LoadAssetAtPath<Item_Epoxy_Block>("Assets/Game_Objects/Item/Item_Epoxy_Block.prefab");
        Debug.Log("NC Demo: queued " + job.Name);
        manager.QueueJob(job);
    }

    static T Find<T>() where T : Object
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("NC Demo: enter Play mode in NC_Mill_Demo first.");
            return null;
        }
        var found = Object.FindFirstObjectByType<T>();
        if (found == null) Debug.LogError("NC Demo: no " + typeof(T).Name + " in the open scene.");
        return found;
    }
}
