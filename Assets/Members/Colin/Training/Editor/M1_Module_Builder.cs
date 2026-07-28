using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Training_Builder_Core;

// Module 1 — "CNC Milling: What & Why" (docs/VR_Modules/01_Module1_Plan.md).
// Content + mill-specific scene wiring only; all generic scaffolding comes from
// Training_Builder_Core. Re-run "Training/3 Build Module1 Scene" or "Training/5
// Reposition Markers" after the ProMill8000 model is replaced — markers are
// re-derived from node bounds.
//
// Split across sibling files, each a part of this class:
//   M1_Lesson_Content.cs  the lesson steps and their prose
//   M1_Mill_Rig.cs        mill placement, motion parameters, demo playback wiring
//   M1_Markers.cs         the component marker table and its placement (Training/5)
//   M1_Parts_Table.cs     the reference parts table
//   M1_Debug_Menu.cs      play-mode debug commands (Training/8, Training/9)
public partial class M1_Module_Builder : ITraining_Module_Builder{
    // Panel placement, hand-tuned in-editor against the mill.
    private static readonly Vector3 PromptPanelPos = new Vector3(-1.1f, 1.45f, 1.2f);
    private static readonly Vector3 ResultsPanelPos = new Vector3(0f, 1.5f, 1.0f);

    public int Order => 0;

    public void Build(){
        BuildLessonAsset();
        // Before the scene build — SeatWorkholding instantiates this prefab.
        BuildDemoBlockPrefab();
        BuildModule1Scene();
    }

    [MenuItem("Training/3 Build Module1 Scene")]
    public static void BuildModule1SceneMenu(){
        new M1_Module_Builder().Build();
    }

    // Phase order is load-bearing. The rig prefab is saved before the markers,
    // indicators, scaffold and parts table exist, so Module 2 instantiates a bare
    // mill; the markers exist before the lesson wiring, which resolves them by id.
    private static void BuildModule1Scene(){
        var scene = NewModuleScene();

        M1_Mill_Rig rig = BuildMillRig();
        if (rig == null) return;

        SaveMillRigPrefab(rig);

        GameObject markerGroup = new GameObject(Colin_Training_Paths.MarkerGroupName);
        BuildMarkers(markerGroup.transform, rig.Mill, rig.Vice);

        M1_Axis_Indicators indicators = BuildAxisIndicators(rig.MillBounds);

        Module_Scaffold scaffold = BuildLessonScaffold(PromptPanelPos, Quaternion.identity, ResultsPanelPos);
        if (scaffold == null) return;

        WireLessonManager(scaffold, markerGroup, rig, indicators);
        BuildPartsTable(rig.Mill, rig.Vice, scaffold.Sequencer, scaffold.Manager);

        EditorSceneManager.SaveScene(scene, Colin_Training_Paths.Module1ScenePath);
        RegisterModule(AssetDatabase.LoadAssetAtPath<Lesson_Definition>(Colin_Training_Paths.M1LessonPath), Colin_Training_Paths.Module1ScenePath);
        Debug.Log($"M1_Module_Builder: Module1 scene built. Mill bounds: center {rig.MillBounds.center}, size {rig.MillBounds.size}");
    }

    // The registries, highlighter, demo controller and marker-driven reactions that
    // sit on the Lesson_Manager. AddComponent order here is the component order in the
    // saved scene — keep it stable so rebuilds stay diffable.
    private static void WireLessonManager(Module_Scaffold scaffold, GameObject markerGroup, M1_Mill_Rig rig, M1_Axis_Indicators indicators){
        GameObject manager = scaffold.Manager;
        Lesson_Sequencer sequencer = scaffold.Sequencer;

        Marker_Registry registry = manager.AddComponent<Marker_Registry>();
        Part_Highlighter highlighter = manager.AddComponent<Part_Highlighter>();
        Mill_Demo_Controller demoController = manager.AddComponent<Mill_Demo_Controller>();

        SetRef(registry, "MarkersRoot", markerGroup.transform);
        SetRef(registry, "Sequencer", sequencer);

        SetRef(highlighter, "Sequencer", sequencer);
        SetRef(highlighter, "Registry", registry);

        WireMillDemo(demoController, rig, indicators);

        // The guard door and the door-unlock button both release the same interlock,
        // so each gets its own toggle on the same doors.
        WireDoorToggle(manager, markerGroup, sequencer, rig.Doors, MarkerGuardDoor);
        WireDoorToggle(manager, markerGroup, sequencer, rig.Doors, MarkerDoorUnlock);

        // Power On + Emergency Stop: a visible state cue on the real mill part. Node
        // paths come from the Markers table rather than being retyped here.
        WireStateToggle(manager, markerGroup, rig.Mill, sequencer, MarkerPowerOn,
            MarkerNodePath(MarkerPowerOn), "Power On", new Color(0.2f, 1f, 0.3f, 1f));
        WireStateToggle(manager, markerGroup, rig.Mill, sequencer, MarkerEmergencyStop,
            MarkerNodePath(MarkerEmergencyStop), "Emergency Stop", new Color(1f, 0.15f, 0.15f, 1f));

        SetRef(sequencer, "Registry", registry);
        SetRef(sequencer, "DemoController", demoController);
    }

    private static void WireDoorToggle(GameObject manager, GameObject markerGroup, Lesson_Sequencer sequencer, Item_Mill_Doors doors, string markerId){
        Door_Click_Toggle toggle = manager.AddComponent<Door_Click_Toggle>();
        SetRef(toggle, "DoorMarker", FindMarker(markerGroup, markerId));
        SetRef(toggle, "Doors", doors);
        SetRef(toggle, "Sequencer", sequencer);
    }
}
