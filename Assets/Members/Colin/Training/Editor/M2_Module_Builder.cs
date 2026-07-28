using System.Collections.Generic;
using UnityEditor;
using static Training_Builder_Core;

// Module 2 — "System Startup & Program Execution" (docs/VR_Modules/03_Module2_Startup_Plan.md).
//
// Lesson content only. The scene is hand-authored and committed at
// Assets/Members/Colin/Scenes/Module2_Startup.unity — it is no longer generated; see
// docs/VR_Modules/07_Scene_Authoring_Migration.md. Open it and run "Training/7
// Validate Open Module Scene" after editing: every Panel_Action step below has to
// find an action button with a matching Action_Id under the Action_Button_Registry's
// ButtonsRoot, and Lesson_Sequencer matches those ids by string at runtime, so a
// mismatch is a step that can never be completed and reports nothing.
public class M2_Module_Builder : ITraining_Module_Builder{
    public int Order => 1;

    public void Build(){
        Lesson_Definition lesson = BuildM2LessonAsset();

        // Registration used to be the last line of the scene build. The registry
        // drives the Bootstrap menu, the build-settings scene list and the play
        // redirect, so it still has to run even though the scene does not.
        RegisterModule(lesson, Colin_Training_Paths.Module2ScenePath);
    }

    // ------------------------------------------------------------------
    // Lesson content (system startup)
    // ------------------------------------------------------------------

    private static Lesson_Definition BuildM2LessonAsset(){
        Lesson_Definition def = LoadOrCreateLesson(Colin_Training_Paths.M2LessonPath);
        def.Module_Id = "M2";
        def.Scene_Name = "Module2_Startup";
        def.Display_Name = "M2 — System Startup & Program Execution";
        def.Steps = BuildM2Steps();
        def.Quiz_Pass_Threshold = def.Steps.FindAll(s => s.Include_In_Quiz).Count;
        def.Practice_Shuffles_Quiz = false; // order IS the test — keep the canonical sequence
        EditorUtility.SetDirty(def);
        return def;
    }

    private static List<Lesson_Step> BuildM2Steps(){
        return new List<Lesson_Step>{
            Info("intro_cell", "System Startup — bring the whole cell up from cold.\n\nTwo stations: the ARM station (PC + black-box controller, running SCORBASE) and the MILL station (ProMill 8000, running CNCBase). The robot arm itself isn't shown here — its state is described as you go.\n\nThree phases: (1) power on every system, (2) launch SCORBASE and CNCBase, (3) bring each machine Active, Home it, then verify. Follow the highlighted control at each step.\n\nPress Continue."),
            // Phase 1 — power on every system
            M2Action("arm_pc_on", "Arm-station PC power button", "Phase 1 — power on everything. Each station has a dedicated PC; boot it first."),
            M2Action("arm_controller_on", "Robot controller power (the black box)", "The black box is the robot controller that drives the arm — SCORBASE talks to it. Power it on."),
            M2Action("mill_pc_on", "Mill-station PC power button", "The mill station needs its own PC running too."),
            M2Action("mill_power_on", "ProMill 8000 main power", "Switch on the ProMill 8000's main hardware power. Every system is now powered."),
            // Phase 2 — launch the control software
            M2Action("scorbase_launch", "Launch SCORBASE", "Phase 2 — launch the control software. SCORBASE runs the arm station."),
            M2Action("cncbase_launch", "Launch CNCBase", "CNCBase runs the mill station. Both control programs are now up."),
            // Phase 3 — bring each machine active, home, and verify
            M2Action("scorbase_online", "Control On (go Active)", "Phase 3 — bring each machine active, home it, and verify. In SCORBASE, Control On: only Active sends commands to real hardware."),
            M2Action("scorbase_home", "Search Home — All Axes", "Search Home drives each axis to its home switch, setting the encoder reference. Nothing is trustworthy before homing."),
            M2Action("scorbase_standalone", "Mode: Standalone", "Standalone = the station runs under its own software. CIM-managed (cell-wide) control is taught in Module 5."),
            M2Action("verify_arm", "Test Move (A1)", "Jog one axis to prove the arm responds — Active, homed and Standalone."),
            M2Action("cncbase_online", "Connect: Active", "Now the mill. In CNCBase, connect Active (the real machine), not Simulation."),
            M2Action("cncbase_home", "Machine Home", "Machine Home sets the mill's factory reference; all positioning is relative to it."),
            M2Action("run_start_fms", "start_fms.nc", "Run start_fms.nc — it puts the mill in a wait-loop in local control, ready for cell commands."),
            M2Action("verify_mill", "Confirm: Running", "Confirm the status reads Running: start_fms.nc — the mill is in local control."),
            Info("guided_done", "All systems active.\n\nThe arm is Active, Homed and Standalone; the mill is Homed and running start_fms.nc in local control. The arm has loaded the first workpiece onto the vise.\n\nNext: the practice run — no highlights. Perform the full cold-start in the correct order. Out-of-order actions are logged as errors.\n\nPress Continue.")
        };
    }

    private static Lesson_Step M2Action(string id, string label, string teach){
        return PanelAction(id, label, teach, "Practice: bring the cell up from cold, in the correct order.");
    }
}
