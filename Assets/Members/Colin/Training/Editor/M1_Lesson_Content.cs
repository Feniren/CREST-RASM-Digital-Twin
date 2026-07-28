using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Training_Builder_Core;

// Module 1 — the lesson itself: the ordered steps and their prose. The Step_Id of a
// Select step doubles as its Target_Marker_Id, which is how a step reaches the scene
// marker it wants; the ids come from M1_Markers so the two cannot drift apart.
public partial class M1_Module_Builder{
    // Deliberately partial: the trainee passes on 4 of the 6 quiz components, per the
    // pass-gate in docs/VR_Modules/01_Module1_Plan.md. Module 2 derives its threshold
    // from the full step count instead, because there the order IS the test.
    private const int QuizPassThreshold = 4;

    private static void BuildLessonAsset(){
        Lesson_Definition def = LoadOrCreateLesson(Colin_Training_Paths.M1LessonPath);
        def.Module_Id = "M1";
        def.Scene_Name = "Module1_Overview";
        def.Display_Name = "M1 — CNC Milling: What & Why";
        def.Quiz_Pass_Threshold = QuizPassThreshold;
        def.Steps = BuildM1Steps();
        EditorUtility.SetDirty(def);
    }

    private static List<Lesson_Step> BuildM1Steps(){
        List<Lesson_Step> steps = new List<Lesson_Step>{
            Info("intro_role", "Welcome to the ProMill 8000 — a 3-axis CNC milling machine, and the machining station of the Intelitek SmartCIM 4.0 cell. Conveyor pallets deliver raw stock and the robot arm loads it into the mill for cutting.\n\nPress Continue."),
            Info("intro_operations", "A mill cuts by feeding a rotating tool into the workpiece. The five core operations are:\n\nFACE — flatten the top surface\nPOCKET — hollow out a recess\nCONTOUR — cut an outside profile\nDRILL — plunge holes\nSLOT — cut channels\n\nPress Continue."),
            Info("intro_axes", "The mill moves in three axes. Use the right-hand rule: thumb = +X (table left–right), index = +Y (table fore–aft), middle = +Z (spindle up–down).\n\nPress Continue."),
            Select(MarkerSpindleMotor, "Spindle Motor", "the motor on top of the head — it spins the spindle and cutting tool"),
            Select(MarkerSpindleHead, "Spindle Head", "it holds the rotating spindle and cutting tool, and moves up and down in Z"),
            Select(MarkerVice, "Vise", "it clamps the workpiece to the table during cutting"),
            Select(MarkerGuardDoor, "Guard Door", "the perspex shield that must be closed while the spindle is cutting — opened with the Door Open button in CNCBase"),
            Select(MarkerEmergencyStop, "Emergency Stop Button", "the red button on the front — press it to immediately stop the machine in an emergency"),
            Select(MarkerPowerOn, "Power Switch", "energizes the machine before any operation — always the first control you use at start-up", false),
            Select(MarkerDoorUnlock, "Door Unlock", "releases the guard-door interlock so the doors can slide open for loading and unloading", false),
            Select(MarkerElectronicsCabinet, "Electronics Cabinet", "the lower cabinet housing the machine's drive and control electronics — opened with the electrical panel keys"),
            Info("intro_accessories", "Not shown in this model: the machine also has a right-side connection panel (power, Ethernet, coolant, jog pendant ports) and optional accessories — a handheld jog pendant and a monitor stand.\n\nPress Continue."),
            new Lesson_Step{
                Step_Id = "axis_demo",
                Kind = Lesson_Step_Kind.Axis_Demo,
                // Travels interpolated from the constants that clamp the axes and drive
                // the demo, so the number on screen cannot drift from the motion.
                Prompt_Text = "Axis demo — watch the machine move:\n"
                    + $"X: table travels {XTravelMm:0} mm left–right\n"
                    + $"Y: table travels {YTravelMm:0} mm fore–aft\n"
                    + $"Z: spindle travels {ZTravelMm:0} mm up–down"
            },
            new Lesson_Step{
                Step_Id = "milling_demo",
                Kind = Lesson_Step_Kind.Milling_Demo,
                Prompt_Text = "Milling demo — the guard doors open, the spindle plunges into the perspex block, and the table cuts a square pocket: plunge → square pass → retract."
            }
        };

        // Counted rather than written out, so adding or dropping a Select step cannot
        // leave the closing prose claiming a total that no longer exists.
        int quizTotal = steps.FindAll(s => s.Include_In_Quiz).Count;
        steps.Add(Info("guided_done", "Guided tour complete!\n\nNext: the practice quiz. Labels and highlights are now off — identify each component from its name.\n\n"
            + $"You need at least {QuizPassThreshold} of {quizTotal} to pass. Press Continue."));

        return steps;
    }

    private static void BuildDemoBlockPrefab(){
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = Colin_Training_Paths.DemoBlockName;
        block.transform.localScale = new Vector3(0.1f, 0.05f, 0.1f);
        Material blueGlass = AssetDatabase.LoadAssetAtPath<Material>(Colin_Training_Paths.BlueGlassPath);

        if (blueGlass != null)
            block.GetComponent<MeshRenderer>().sharedMaterial = blueGlass;

        PrefabUtility.SaveAsPrefabAsset(block, Colin_Training_Paths.DemoBlockPrefabPath);
        Object.DestroyImmediate(block);
    }
}
