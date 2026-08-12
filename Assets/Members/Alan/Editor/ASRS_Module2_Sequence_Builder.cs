using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Authors the Module 2 lesson content into whichever scene is open: fixes
// the InstructionCanvas "NextButton" wiring (as of this writing it isn't
// hooked up to SequenceManager.OnNextPressed anywhere, so pressing Next does
// nothing), wires up every existing ASRS_Power_Interactable in the scene to
// the SequenceManager, and populates SequenceManager.steps with the 10-step
// lesson: identify the two Emergency Stops, the pneumatic gripper, and the
// RFID sensor; power the laptop and the Controller-USB; home and test the
// arm via SCORBASE (the manual's Powering Up / Homing / Testing sections);
// then run one full conveyor-to-rack material-flow cycle.
//
// Steps 1-4 (identification) and step 5 (laptop) are authored with EMPTY
// targets — those props don't exist in the scene yet. Drag the real marker
// GameObject into each step's targets once it does. Step 10 (conveyor
// round-trip) has a requiredActionId ("conveyor_returned") but nothing calls
// it yet — that needs a future hook once the round-trip logic exists.
//
// Run this AFTER "ASRS/Build SCORBASE Panel In Current Scene" — it looks for
// that panel and cross-wires its Search Home / Go actions into the sequence.
// It does NOT create a power-switch placeholder — wire ASRS_Power_Interactable
// onto your own switch object(s) first (e.g. "PowerInteractable"); this just
// hooks whatever it finds up to the lesson.
public static class ASRS_Module2_Sequence_Builder
{
    // Scoped down for now — just wires the power switch(es) to the
    // SequenceManager. NextButton wiring and the step content itself
    // (BuildLessonContent below) come once the actual lesson steps are set.
    [MenuItem("ASRS/Wire Power Switches To Sequence Manager")]
    public static void WirePowerSwitchesOnly()
    {
        SequenceManager sequenceManager = Object.FindFirstObjectByType<SequenceManager>();

        if (sequenceManager == null)
        {
            Debug.LogError("ASRS_Module2_Sequence_Builder: no SequenceManager found in the open scene.");
            return;
        }

        int wiredSwitches = WireExistingPowerSwitches(sequenceManager);

        if (wiredSwitches == 0)
        {
            Debug.LogWarning("ASRS_Module2_Sequence_Builder: no ASRS_Power_Interactable found in the open scene — nothing to wire.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"ASRS_Module2_Sequence_Builder: wired {wiredSwitches} power switch(es) to the SequenceManager. Save the scene.");
    }

    [MenuItem("ASRS/Build Module 2 Lesson Content")]
    public static void BuildLessonContent()
    {
        SequenceManager sequenceManager = Object.FindFirstObjectByType<SequenceManager>();
        ASRS_Scorbase_Panel panel = Object.FindFirstObjectByType<ASRS_Scorbase_Panel>();

        if (sequenceManager == null)
        {
            Debug.LogError("ASRS_Module2_Sequence_Builder: no SequenceManager found in the open scene.");
            return;
        }

        if (panel == null)
        {
            Debug.LogError("ASRS_Module2_Sequence_Builder: no ASRS_Scorbase_Panel found. Run 'ASRS/Build SCORBASE Panel In Current Scene' first.");
            return;
        }

        WireNextButton(sequenceManager);
        int wiredSwitches = WireExistingPowerSwitches(sequenceManager);
        BuildSteps(sequenceManager);
        SetRef(panel, "sequenceManager", sequenceManager);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = sequenceManager.gameObject;

        if (wiredSwitches == 0)
            Debug.LogWarning("ASRS_Module2_Sequence_Builder: no ASRS_Power_Interactable found in the scene — the 'Powering Up' step has nothing wired to satisfy it yet.");

        Debug.Log($"ASRS_Module2_Sequence_Builder: Module 2 lesson content built ({wiredSwitches} power switch(es) wired). Save the scene.");
    }

    // The shared "InstructionCanvas" prefab's NextButton ships with an empty
    // Button.onClick and an empty Interactable_Select.OnInteractBegin — every
    // scene that uses it has to wire those itself. Module 1 does; Module 2
    // currently doesn't, which is why Next silently does nothing there today.
    private static void WireNextButton(SequenceManager sequenceManager)
    {
        GameObject nextButtonGO = GameObject.Find("NextButton");
        if (nextButtonGO == null)
        {
            Debug.LogWarning("ASRS_Module2_Sequence_Builder: no 'NextButton' object found in the scene — SequenceManager.nextButton left unset.");
            return;
        }

        SetRef(sequenceManager, "nextButton", nextButtonGO);

        Button button = nextButtonGO.GetComponent<Button>();
        if (button != null)
        {
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(button.onClick, i);
            UnityEventTools.AddVoidPersistentListener(button.onClick, sequenceManager.OnNextPressed);
        }

        Interactable_Select select = nextButtonGO.GetComponent<Interactable_Select>();
        if (select != null)
        {
            for (int i = select.OnInteractBegin.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(select.OnInteractBegin, i);
            UnityEventTools.AddVoidPersistentListener(select.OnInteractBegin, sequenceManager.OnNextPressed);
        }
    }

    // Wires every ASRS_Power_Interactable already in the scene (however many
    // there are, however they're named) to the SequenceManager, rather than
    // requiring one canonical object — any of them turning on satisfies the
    // "Powering Up" step equally.
    private static int WireExistingPowerSwitches(SequenceManager sequenceManager)
    {
        ASRS_Power_Interactable[] switches = Object.FindObjectsByType<ASRS_Power_Interactable>(FindObjectsSortMode.None);

        foreach (ASRS_Power_Interactable powerSwitch in switches)
            SetRef(powerSwitch, "Sequence_Manager", sequenceManager);

        return switches.Length;
    }

    private static void BuildSteps(SequenceManager sequenceManager)
    {
        SerializedObject so = new SerializedObject(sequenceManager);
        SerializedProperty steps = so.FindProperty("steps");
        steps.arraySize = 10;

        // Identification steps (1-5): pure Marker_Interactable-style steps.
        // targets are intentionally left empty — drag the real marker
        // GameObject in once each prop exists. No requiredActionId: these
        // aren't software actions.

        SetStep(steps.GetArrayElementAtIndex(0),
            "Every station has an Emergency Stop within reach of the operator. Near the Controller-USB there's an E-Stop that cuts power to the arm immediately if something goes wrong while you're at the controls.",
            "Point at and select the Emergency Stop control near the Controller-USB to continue.",
            "");

        SetStep(steps.GetArrayElementAtIndex(1),
            "The ASRS Storage Unit itself also has its own Emergency Stop, mounted on the rack housing — reachable even if you're standing at the rack instead of the controller. Real installations wire every E-Stop together so pressing any one of them halts the whole cell.",
            "Point at and select the Emergency Stop button on the ASRS Storage Unit to continue.",
            "");

        SetStep(steps.GetArrayElementAtIndex(2),
            "This is the pneumatic gripper — the \"hand\" on the end of the robotic arm. It's what actually grabs a template by its handle and carries it between the rack and the conveyor.",
            "Point at and select the pneumatic gripper on the robot arm to continue.",
            "");

        SetStep(steps.GetArrayElementAtIndex(3),
            "This is the RFID sensor. As a slotted table passes it, the sensor reads the table's tag and reports its identity — that's how the ASRS always knows which template is which without a person having to look.",
            "Point at and select the RFID sensor to continue.",
            "");

        // Step 5: an action, not just identification — hook ready for once a
        // laptop prop + power toggle exist.
        SetStep(steps.GetArrayElementAtIndex(4),
            "The SCORBASE software runs on the station laptop — it's the operator's window into the Controller-USB. Nothing else in this lesson works until it's powered on.",
            "Turn on the laptop to continue.",
            "laptop_power_on");

        // Steps 6-9: the SCORBASE manual walkthrough (already wired — the
        // power switch and ASRS_Scorbase_Panel report these action IDs).
        SetStep(steps.GetArrayElementAtIndex(5),
            "This is the ASRS Controller-USB. It runs the SCORBASE software and is what actually drives the ASRS robotic arm — SCORBASE talks to it over USB, and it talks to the arm's motors directly.",
            "Press the power switch on the Controller-USB to turn it on.",
            "controller_power_on");

        SetStep(steps.GetArrayElementAtIndex(6),
            "Before SCORBASE can trust any position, it needs to know exactly where every axis actually is. Search Home drives each axis to a known reference point, one at a time, so its position becomes trustworthy.",
            "On the SCORBASE panel, press Search Home and watch each axis check off, then Robot.",
            "scorbase_home");

        SetStep(steps.GetArrayElementAtIndex(7),
            "With the arm homed, you can now command it to any storage cell by its Table ID. This is the same kind of check a real installation runs before trusting the ASRS with real parts.",
            "On the SCORBASE panel, type a storage cell ID (e.g. 010001) and press Go.",
            "test_move");

        SetStep(steps.GetArrayElementAtIndex(8),
            "A full test also proves the arm can reach the far side of the rack, which requires it to rotate before moving — exactly like the real ASRS-36x2 switching sides.",
            "Now type a cell ID on the far side of the rack (row 7 or higher, e.g. 070001) and press Go again.",
            "test_move");

        // Step 10: the material-flow proof. No code calls this yet — needs a
        // future hook (likely on the RFID sensor or conveyor script) once the
        // round-trip logic exists.
        SetStep(steps.GetArrayElementAtIndex(9),
            "This is the real material flow: a slotted table rides the conveyor into the ASRS station, the RFID sensor identifies it, and the arm stores it in an empty cell. Watching it happen end to end is the real test of everything you've just set up.",
            "Load a slotted table/template onto the conveyor belt, and wait for the arm to take it from the conveyor and store it in the rack.",
            "conveyor_returned");

        so.FindProperty("completionText").stringValue =
            "Nice work — you've identified the ASRS's safety and handling components, brought SCORBASE online, proven the arm can reach the whole rack, and watched a template make it all the way from the conveyor into storage.";

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetStep(SerializedProperty step, string infoText, string instructionsText, string actionId)
    {
        step.FindPropertyRelative("targets").arraySize = 0;
        step.FindPropertyRelative("infoText").stringValue = infoText;
        step.FindPropertyRelative("instructionsText").stringValue = instructionsText;
        step.FindPropertyRelative("RequiredOccupiedTable").objectReferenceValue = null;
        step.FindPropertyRelative("requiredActionId").stringValue = actionId;
    }

    private static void SetRef(Component component, string property, Object value)
    {
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(property);

        if (prop == null)
        {
            Debug.LogError($"ASRS_Module2_Sequence_Builder: property '{property}' not found on {component.GetType().Name}");
            return;
        }

        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
