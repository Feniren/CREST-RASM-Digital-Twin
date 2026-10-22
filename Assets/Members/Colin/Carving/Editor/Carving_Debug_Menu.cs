using UnityEditor;
using UnityEngine;

/// <summary>
/// Play-mode debug drivers for the sandbox, usable over MCP (which cannot press keys or move
/// transforms in Play mode). The demo cuts themselves are authored on Carving_Demo_Path.
/// </summary>
public static class Carving_Debug_Menu
{
    [MenuItem("Carving/8 Debug - Run Demo Cuts")]
    public static void Run_Demo_Cuts()
    {
        if (!Playing()) return;
        var path = Object.FindFirstObjectByType<Carving_Demo_Path>();
        if (path == null)
        {
            Debug.LogError("Carving: no Carving_Demo_Path in the open scene.");
            return;
        }
        path.Run();
    }

    [MenuItem("Carving/8 Debug - Reset Stocks")]
    public static void Reset_Stocks()
    {
        if (!Playing()) return;
        foreach (var s in Object.FindObjectsByType<Mill_Stock>(FindObjectsSortMode.None)) s.Reset_Stock();
        foreach (var s in Object.FindObjectsByType<Lathe_Stock>(FindObjectsSortMode.None)) s.Reset_Stock();
    }

    static bool Playing()
    {
        if (Application.isPlaying) return true;
        Debug.LogError("Carving: enter Play mode in Carving_Sandbox first.");
        return false;
    }
}
