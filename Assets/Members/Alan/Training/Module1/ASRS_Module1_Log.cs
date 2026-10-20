using UnityEngine;

// Consistent Console prefixes for Module 1, so one filter ("[ASRS][Module1]")
// shows the whole module and a narrower one isolates a single system.
public static class ASRS_Module1_Log
{
    public const string Root = "[ASRS][Module1]";
    public const string Rack = Root + "[Rack]";
    public const string Cells = Root + "[Cells]";
    public const string Quiz = Root + "[Quiz]";
    public const string Video = Root + "[Video]";
    public const string Flow = Root + "[Flow]";

    public static void Info(string tag, string message, Object context = null) => Debug.Log($"{tag} {message}", context);
    public static void Warn(string tag, string message, Object context = null) => Debug.LogWarning($"{tag} {message}", context);
    public static void Error(string tag, string message, Object context = null) => Debug.LogError($"{tag} {message}", context);
}
