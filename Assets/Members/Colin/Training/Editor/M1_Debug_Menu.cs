using UnityEditor;
using UnityEngine;

// Module 1 — play-mode debug commands and the model diagnostics dump. These invoke
// handlers on a running scene. Generic equivalents live in Training_Debug.
public static class M1_Debug_Menu{
    // Mirror the Target_Marker_Id strings in M1_Lesson.asset — Training/7 is what
    // checks the scene side of the contract.
    private const string MarkerGuardDoor = "guard_door";
    private const string MarkerDoorUnlock = "door_unlock";
    private const string MarkerPowerOn = "power_on";
    private const string MarkerEmergencyStop = "emergency_stop";
    private const string MillWrapperName = "PM8000_Training";

    [MenuItem("Training/8 Debug - Begin Practice Directly")]
    public static void DebugBeginPractice(){
        if (!Application.isPlaying) return;
        Lesson_Sequencer sequencer = Object.FindFirstObjectByType<Lesson_Sequencer>();
        if (sequencer == null){ Debug.LogError("M1_Debug_Menu: no Lesson_Sequencer (load a module first)."); return; }
        if (sequencer.Lesson == null){ Debug.LogError("M1_Debug_Menu: Lesson_Sequencer.Lesson is not assigned in this scene."); return; }
        sequencer.Begin(sequencer.Lesson, Lesson_Mode.Practice);
    }

    [MenuItem("Training/8 Debug - Click Guard Door")]
    public static void DebugClickGuardDoor(){ ClickDoorToggle(MarkerGuardDoor); }

    [MenuItem("Training/8 Debug - Click Door Unlock")]
    public static void DebugClickDoorUnlock(){ ClickDoorToggle(MarkerDoorUnlock); }

    [MenuItem("Training/8 Debug - Click Power On")]
    public static void DebugClickPowerOn(){ ClickStateToggle(MarkerPowerOn); }

    [MenuItem("Training/8 Debug - Click Emergency Stop")]
    public static void DebugClickEmergencyStop(){ ClickStateToggle(MarkerEmergencyStop); }

    private static void ClickDoorToggle(string markerId){
        Door_Click_Toggle toggle = FindToggleFor<Door_Click_Toggle>(markerId, "DoorMarker", out Component_Marker marker);
        if (toggle == null) return;
        toggle.On_Door_Selected(marker);
        Debug.Log($"M1_Debug_Menu: {markerId} click simulated.");
    }

    private static void ClickStateToggle(string markerId){
        Marker_State_Toggle toggle = FindToggleFor<Marker_State_Toggle>(markerId, "Marker", out Component_Marker marker);
        if (toggle == null) return;
        toggle.On_Selected(marker);
        Debug.Log($"M1_Debug_Menu: {markerId} click simulated.");
    }

    // The Lesson_Manager carries two Door_Click_Toggles (guard door and door unlock)
    // and two Marker_State_Toggles (power on and emergency stop), so FindFirstObjectByType
    // returns an arbitrary one of each pair — the serialized marker reference is what
    // identifies which is which.
    private static T FindToggleFor<T>(string markerId, string markerField, out Component_Marker marker) where T : Component{
        marker = null;
        if (!Application.isPlaying) return null;

        Marker_Registry registry = Object.FindFirstObjectByType<Marker_Registry>();
        if (registry == null){ Debug.LogError("M1_Debug_Menu: no Marker_Registry (load Module 1 first)."); return null; }

        marker = registry.Resolve(markerId);
        if (marker == null){ Debug.LogError($"M1_Debug_Menu: registry has no marker '{markerId}'."); return null; }

        foreach (T candidate in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
            if (new SerializedObject(candidate).FindProperty(markerField).objectReferenceValue == (Object)marker)
                return candidate;

        Debug.LogError($"M1_Debug_Menu: no {typeof(T).Name} bound to marker '{markerId}'.");
        return null;
    }

    [MenuItem("Training/9 Dump Mill Diagnostics")]
    public static void DumpDiagnostics(){
        GameObject wrapper = GameObject.Find(MillWrapperName);

        if (wrapper == null){
            Debug.LogError($"M1_Debug_Menu: no {MillWrapperName} in open scene.");
            return;
        }

        Transform mill = wrapper.transform.GetChild(0);
        var sb = new System.Text.StringBuilder();
        string[] nodes = {
            "Worktable_Base", "WB_XAxis_Drive", "WB_YAxis_Drive", "WB_Clamp", "WB_Static", "WB_Spindle", "WB_Hardware",
            "PB_XY_Saddle", "PB_Saddle_Moving", "PB_Saddle_Static", "PB_Knee_Table",
            "SpindleBase", "SpindleMotor", "SB_Static", "doors", "PB_Column_Structure", "PB_Column_Static",
            "ToolChangeBody", "MillController"
        };

        foreach (string name in nodes){
            Transform t = FindChild(mill, name);

            if (t == null){
                sb.AppendLine($"{name}: NOT FOUND");
                continue;
            }

            Bounds b = RendererBounds(t);
            sb.AppendLine($"{name}: center ({b.center.x:F2}, {b.center.y:F2}, {b.center.z:F2}) size ({b.size.x:F2}, {b.size.y:F2}, {b.size.z:F2})");
        }

        Debug.Log("M1_Debug_Menu diagnostics:\n" + sb);
    }

    private static Transform FindChild(Transform root, string name){
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name)
                return t;

        return null;
    }

    private static Bounds RendererBounds(Transform root){
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return new Bounds(root.position, Vector3.one * 0.1f);

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }
}
