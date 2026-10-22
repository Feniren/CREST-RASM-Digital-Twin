using UnityEditor;
using UnityEngine;
using ProMill8000;

// Module 1 — play-mode debug commands and the model diagnostics dump. These invoke
// handlers on a running scene. Generic equivalents live in Training_Debug.
public static class M1_Debug_Menu{
    // Mirror the Target_Marker_Id strings in M1_Lesson.asset — Training/7 is what
    // checks the scene side of the contract.
    private const string MarkerGuardDoor = "guard_door";
    private const string MarkerDoorUnlock = "door_unlock";
    private const string MarkerPowerOn = "power_on";
    private const string MarkerEmergencyStop = "emergency_stop";
    // The mill root itself, not its wrapper — Find(name) locates it whether it sits
    // at the scene root or nested under IntellitekMill_Training.
    private const string MillRootName = "ClassifyIntellitekMill";
    private const string DiagnosticsPath = "Logs/M1_Mill_Diagnostics.md";

    // The right-side sliding panel rests raised on its gas struts, so its travel runs
    // negative — same convention as SpindleMotor. Matches the AxisMovement limits on
    // SlidingPanel_Moving in IntellitekMill_Training.prefab.
    private const string PanelName = "SlidingPanel_Moving";
    private const float PanelTravel = -0.6428f;

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

    [MenuItem("Training/8 Debug - Toggle Sliding Panel")]
    public static void DebugToggleSlidingPanel(){
        if (!Application.isPlaying){
            Debug.LogError("M1_Debug_Menu: the panel is driven by AxisMovement.Update — enter play mode first.");
            return;
        }

        GameObject panelObject = GameObject.Find(PanelName);

        if (panelObject == null){
            Debug.LogError($"M1_Debug_Menu: no {PanelName} in the loaded scenes.");
            return;
        }

        AxisMovement panel = panelObject.GetComponent<AxisMovement>();

        if (panel == null){
            Debug.LogError($"M1_Debug_Menu: {PanelName} has no AxisMovement.");
            return;
        }

        bool isDown = panel.OffsetFromOrigin < PanelTravel * 0.5f;
        panel.MoveToOffset(isDown ? 0f : PanelTravel);
        Debug.Log($"M1_Debug_Menu: sliding panel moving {(isDown ? "up" : "down")}.");
    }

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

    // World renderer bounds per group, written to a file: the console truncates long
    // multi-line messages, and these numbers are what the Component_Marker boxes are
    // sized and placed from.
    [MenuItem("Training/9 Dump Mill Diagnostics")]
    public static void DumpDiagnostics(){
        GameObject millObject = GameObject.Find(MillRootName);

        if (millObject == null){
            Debug.LogError($"M1_Debug_Menu: no {MillRootName} in open scene.");
            return;
        }

        Transform mill = millObject.transform;
        // Vice and Demo_Block hang off the training wrapper, not the mill, so look them up
        // from the wrapper when there is one.
        Transform searchRoot = mill.parent != null ? mill.parent : mill;
        var sb = new System.Text.StringBuilder();
        string[] nodes = {
            "IntellitekMillBody",
            "Worktable_Base", "WB_Static", "WB_Spindle", "WB_XAxis_Drive", "WB_YAxis_Drive", "WB_Hardware",
            // The CAD's own WB_Clamp is replaced by the DualAxisVice prefab, which rides the
            // drives through their dependents arrays rather than by parenting.
            "Vice", "Demo_Block",
            "SpindleBase", "SB_Static", "SpindleMotor", "SM_Static", "SM_Rotating", "SM_Hardware",
            "ToolChangeBody", "TC_Static", "TC_SwingArm", "TC_Carousel",
            "ElectronicsCabinet", "Cabinet_Hardware",
            "Enclosure", "Enclosure_Frame", "SlidingPanel", "SlidingPanel_Moving",
            "doors", "LeftMillDoor", "RightMillDoor",
            // The two switch renderers the Marker_State_Toggles drive.
            "kaig", @"\X2\59276025505C\X0  usemtl color_bfbfbf",
            "MillController"
        };

        Bounds whole = RendererBounds(mill);
        sb.AppendLine($"# {MillRootName} diagnostics");
        sb.AppendLine();
        sb.AppendLine($"root position {mill.position:F4}  rotation {mill.eulerAngles:F2}  scale {mill.lossyScale:F4}");
        sb.AppendLine($"whole mill: center ({whole.center.x:F4}, {whole.center.y:F4}, {whole.center.z:F4}) size ({whole.size.x:F4}, {whole.size.y:F4}, {whole.size.z:F4})");
        sb.AppendLine($"            min ({whole.min.x:F4}, {whole.min.y:F4}, {whole.min.z:F4}) max ({whole.max.x:F4}, {whole.max.y:F4}, {whole.max.z:F4})");
        sb.AppendLine();

        foreach (string name in nodes){
            Transform t = FindChild(searchRoot, name);

            if (t == null){
                sb.AppendLine($"{name}: NOT FOUND");
                continue;
            }

            Bounds b = RendererBounds(t);
            sb.AppendLine($"{name}: center ({b.center.x:F4}, {b.center.y:F4}, {b.center.z:F4}) size ({b.size.x:F4}, {b.size.y:F4}, {b.size.z:F4})");
        }

        // The milling demo's plungeDepth is calibrated against the lowest part of the
        // rotating spindle assembly, and the workpiece is placed on its axis — neither
        // matches SM_Rotating's bounding-box centre, so list the candidates explicitly.
        Transform rotating = FindChild(mill, "SM_Rotating");

        if (rotating != null){
            sb.AppendLine();
            sb.AppendLine("SM_Rotating children by lowest point (the first is the tool tip):");

            var byHeight = new System.Collections.Generic.List<Renderer>(rotating.GetComponentsInChildren<Renderer>());
            byHeight.Sort((a, b) => a.bounds.min.y.CompareTo(b.bounds.min.y));

            for (int i = 0; i < Mathf.Min(5, byHeight.Count); i++){
                Bounds b = byHeight[i].bounds;
                sb.AppendLine($"  {byHeight[i].name}: min.y {b.min.y:F4} axis ({b.center.x:F4}, {b.center.z:F4}) size ({b.size.x:F4}, {b.size.y:F4}, {b.size.z:F4})");
            }
        }

        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(DiagnosticsPath));
        System.IO.File.WriteAllText(DiagnosticsPath, sb.ToString());
        Debug.Log($"M1_Debug_Menu: diagnostics written to {DiagnosticsPath}");
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
