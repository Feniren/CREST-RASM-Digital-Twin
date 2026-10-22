using UnityEditor;
using UnityEngine;

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
