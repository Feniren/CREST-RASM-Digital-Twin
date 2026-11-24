using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Gives Module 2 the same lesson architecture as Module 1, in whichever
// scene is open (ASRSModule2):
//
//   * The widget Lesson Panel (Lesson_Panel_Builder) replaces the
//     AboutCanvas info panel. The old canvas is switched off, not deleted.
//   * Module 1's quiz (ASRS_Module1_Quiz: a random draw from a question pool,
//     one question at a time with feedback, a pass mark, Retry and Continue)
//     is shown on a new last step, "M2_QuizPassed". It replaces the old
//     "replay the steps" Quiz_Manager, which is switched off and unhooked
//     from SequenceManager.
//   * Steps get panel titles (only blank ones are filled).
//
//   Module2_Features                 (always active, hosts the component)
//   └ Module2Quiz                    ASRS_Module1_Quiz
//     └ QuizPanel                    world-space canvas (shown on the quiz step)
//
// The SCORBASE panel, the numeric keypad, the power switches and the existing
// lesson steps are left exactly as they are. Re-running only re-wires; the
// quiz panel is never rebuilt (delete Module2Quiz to rebuild it), and its
// questions are only written when it's first built — use "Update Quiz
// Questions" to rewrite them.
public static class ASRS_Module2_Builder
{
    private const string LogTag = "[ASRS][Module2][Build]";
    private const string FeaturesName = "Module2_Features";
    private const string QuizHostName = "Module2Quiz";

    public const string QuizId = "M2_QuizPassed";
    private const string QuizTitle = "Module 2 Check";

    private const int QuizQuestionsPerAttempt = 4;
    private const int QuizPassMark = 3;

    private const string QuizStepText = "Answer the four questions on the quiz panel. They're picked at random from what this module covered: the safety and handling parts, bringing SCORBASE online, homing, and moving the arm. You need at least three correct to pass, and Retry asks a new set of questions.";

    private const string OldCompletionText = "Nice work — you've identified the ASRS's safety and handling components, brought SCORBASE online, proven the arm can reach the whole rack, and watched a template make it all the way from the conveyor into storage.";
    private const string NewCompletionText = "Module 2 complete — you passed the check. You can now find the ASRS's E-Stops, gripper and RFID sensor, power up the station, bring SCORBASE online, home the arm, move it by hand and by Pick and Place, and follow a template from the conveyor into storage.";

    // Panel headings for Module 2's steps. A step matches on its Required
    // Action Id, or (identification steps have none) on how its infoText
    // starts. The two "test_move" steps are told apart by order.
    private static readonly (string actionId, string infoTextStart, string title)[] KnownTitles =
    {
        ("", "Every station has an Emergency Stop", "Emergency Stop: Controller"),
        ("", "The ASRS Storage Unit itself also has its own Emergency Stop", "Emergency Stop: ASRS"),
        ("", "This is the pneumatic gripper", "Pneumatic Gripper"),
        ("", "This is the RFID sensor", "RFID Sensor"),
        ("laptop_power_on", "", "Power On the Laptop"),
        ("controller_power_on", "", "Power On the Controller-USB"),
        ("scorbase_online", "", "Go Online"),
        ("scorbase_home", "", "Search Home"),
        ("manual_movement_used", "", "Manual Movement"),
        ("test_move", "", "Pick and Place: This Side"),
        ("test_move_other_side", "", "Pick and Place: Far Side"),
        ("conveyor_returned", "", "Conveyor Round Trip"),
        (QuizId, "", QuizTitle),
    };

    [MenuItem("ASRS/Module 2/Build Module 2 In Current Scene")]
    public static void BuildInScene()
    {
        SequenceManager sequenceManager = Object.FindFirstObjectByType<SequenceManager>();
        if (sequenceManager == null)
        {
            Debug.LogError($"{LogTag} No SequenceManager in the open scene — open ASRSModule2 first.");
            return;
        }

        if (Object.FindFirstObjectByType<ASRS_Scorbase_Panel>(FindObjectsInactive.Include) == null)
            Debug.LogWarning($"{LogTag} No SCORBASE panel in this scene — is ASRSModule2 the open scene?", sequenceManager);

        Undo.RegisterCompleteObjectUndo(sequenceManager, "Build Module 2");

        bool addedQuizStep = EnsureQuizStep(sequenceManager);
        int pickTexts = UpdatePickAndPlaceSteps(sequenceManager);
        int titled = FillStepTitles(sequenceManager);
        int realParts = UseRealParts(sequenceManager);

        // The Module 1 Lesson Panel in place of the AboutCanvas.
        Lesson_Panel_Builder.BuildInScene();
        Lesson_Panel lessonPanel = Object.FindFirstObjectByType<Lesson_Panel>();

        RetireOldQuiz(sequenceManager);

        ASRS_Module1_Quiz quiz = GetOrBuildQuiz(sequenceManager, out bool builtQuiz);
        if (quiz == null)
            return;

        if (builtQuiz && lessonPanel != null)
        {
            // Beside the Lesson Panel, on the reader's right (the canvas
            // faces away from the reader, so +right is their right).
            Transform panel = new SerializedObject(quiz).FindProperty("root").objectReferenceValue is GameObject root ? root.transform : null;
            if (panel != null)
                panel.SetPositionAndRotation(lessonPanel.transform.position + lessonPanel.transform.right * 1.45f, lessonPanel.transform.rotation);
        }

        // Switched back on in case it was unticked while testing.
        GameObject features = FindFeaturesRoot();
        if (features != null && !features.activeSelf)
        {
            Undo.RecordObject(features, "Build Module 2");
            features.SetActive(true);
        }

        ConfigureSequence(sequenceManager);

        EditorSceneManager.MarkSceneDirty(sequenceManager.gameObject.scene);
        // Not the quiz: leaving it selected keeps its large question list in
        // the Inspector going into Play mode.
        Selection.activeGameObject = sequenceManager.gameObject;
        Debug.Log($"{LogTag} Done — Lesson Panel wired, quiz step {(addedQuizStep ? "added" : "already there")}, quiz panel {(builtQuiz ? "built" : "re-wired")}, {titled} step title(s) filled, {realParts} step(s) switched to real parts, {pickTexts} Pick and Place step(s) updated. Save the scene.", sequenceManager);
    }

    // ------------------------------------------------------------ real parts

    // The gripper and RFID sensor steps are picked on the real part (no
    // marker highlight) — the step's Hint Target, which is already that
    // part. The marker is switched off, not deleted. The Emergency Stop
    // steps keep their markers.
    private static readonly string[] RealPartStepStarts =
    {
        "This is the pneumatic gripper",
        "This is the RFID sensor",
    };

    private static int UseRealParts(SequenceManager sequenceManager)
    {
        SerializedObject so = new SerializedObject(sequenceManager);
        SerializedProperty steps = so.FindProperty("steps");
        int switched = 0;

        for (int i = 0; i < steps.arraySize; i++)
        {
            SerializedProperty step = steps.GetArrayElementAtIndex(i);
            string infoText = step.FindPropertyRelative("infoText").stringValue;
            if (!RealPartStepStarts.Any(infoText.StartsWith))
                continue;

            Transform part = step.FindPropertyRelative("hintTarget").objectReferenceValue as Transform;
            if (part == null)
            {
                Debug.LogWarning($"{LogTag} Step {i + 1} has no Hint Target to use as its real part — left on its marker.", sequenceManager);
                continue;
            }

            SerializedProperty targets = step.FindPropertyRelative("targets");
            bool alreadyReal = targets.arraySize == 1 && targets.GetArrayElementAtIndex(0).objectReferenceValue == part.gameObject;
            if (alreadyReal)
                continue;

            for (int t = 0; t < targets.arraySize; t++)
            {
                GameObject old = targets.GetArrayElementAtIndex(t).objectReferenceValue as GameObject;
                if (old != null && old.GetComponent<Marker_Interactable>() != null && old.activeSelf)
                {
                    Undo.RecordObject(old, "Build Module 2");
                    old.SetActive(false);
                }
            }

            targets.arraySize = 1;
            targets.GetArrayElementAtIndex(0).objectReferenceValue = part.gameObject;
            switched++;
            Debug.Log($"{LogTag} Step {i + 1} now picks the real '{part.name}' (marker switched off).", part);
        }

        so.ApplyModifiedProperties();
        return switched;
    }

    // ------------------------------------------------------------ pick and place

    public const string OtherSideActionId = "test_move_other_side";

    private const string PickNearInfo = "Per the manual's Testing the ASRS procedure (p.16): with SCORBASE online and the arm homed, Pick and Place moves a stored table from one place to another. Each place is a location (the dropdown) plus a slot number from 1 to 72 (the Index box). Slots 1–36 are rows 1–6 (010001–060006), the side the arm faces after Search Home; slots 37–72 are rows 7–12 (070001–120006), on the far side. Row 5 (slots 25–30) starts empty.";
    private const string PickNearInstructions = "In Pick and Place, leave Source and Target on ASRS Slot. Click Source Index and enter 1 (table 010001, on the side the arm faces), click Target Index and enter 25 (empty slot 050001), then press OK. In VR, click a box and type with the keypad — its Enter presses OK. Watch the arm fetch the table and store it in slot 25.";
    private const string PickFarInfo = "A full test also proves the arm can reach the far side of the rack. SCORBASE turns the arm by itself whenever the slot it needs is on the other side — exactly like the real ASRS-36x2 switching sides.";
    private const string PickFarInstructions = "Now take a table from the far side: Source Index 37 (table 070001), Target Index 26 (empty slot 050002), then press OK. The arm turns to the far side to grab the table, then turns back to store it.";

    // Steps 10 and 11 told the trainee to type a Table ID into Source ID
    // (a read-out) and to rotate the arm by hand; SCORBASE actually reads
    // the slot numbers in Source/Target Index and turns the arm itself.
    // Rewrites only that old wording, and makes the second pick step wait
    // for a pick from the other side of the rack.
    private static int UpdatePickAndPlaceSteps(SequenceManager sequenceManager)
    {
        SerializedObject so = new SerializedObject(sequenceManager);
        SerializedProperty steps = so.FindProperty("steps");
        int pickStep = 0, updated = 0;

        for (int i = 0; i < steps.arraySize; i++)
        {
            SerializedProperty step = steps.GetArrayElementAtIndex(i);
            SerializedProperty actionId = step.FindPropertyRelative("requiredActionId");
            if (actionId.stringValue != "test_move" && actionId.stringValue != OtherSideActionId)
                continue;

            pickStep++;
            bool far = pickStep == 2;
            if (far && actionId.stringValue != OtherSideActionId)
            {
                actionId.stringValue = OtherSideActionId;
                updated++;
            }

            SerializedProperty info = step.FindPropertyRelative("infoText");
            SerializedProperty instructions = step.FindPropertyRelative("instructionsText");
            if (IsOldPickText(instructions.stringValue))
            {
                info.stringValue = far ? PickFarInfo : PickNearInfo;
                instructions.stringValue = far ? PickFarInstructions : PickNearInstructions;
                updated++;
            }
        }

        so.ApplyModifiedProperties();
        return updated;
    }

    private static bool IsOldPickText(string text) =>
        string.IsNullOrWhiteSpace(text) || text.Contains("Source ID and press OK") || text.Contains("as the Source") || text.Contains("Rotate the Side");

    // Rewrites the quiz's question pool and settings (overwrites hand-edited
    // questions — Ctrl+Z undoes it).
    [MenuItem("ASRS/Module 2/Update Quiz Questions")]
    public static void UpdateQuiz()
    {
        ASRS_Module1_Quiz quiz = FindModule2Quiz();
        if (quiz == null)
        {
            Debug.LogError($"{LogTag} No Module 2 quiz in the open scene — run 'ASRS/Module 2/Build Module 2 In Current Scene' first.");
            return;
        }

        SerializedObject so = new SerializedObject(quiz);
        WriteQuestionPool(so);
        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(quiz.gameObject.scene);
        Debug.Log($"{LogTag} Quiz updated: {QuizQuestionsPerAttempt} questions asked at random per attempt, pass mark {QuizPassMark}. Save the scene.", quiz);
    }

    // ------------------------------------------------------------ steps

    // Appends the quiz step unless a step already waits on QuizId.
    private static bool EnsureQuizStep(SequenceManager sequenceManager)
    {
        SerializedObject so = new SerializedObject(sequenceManager);
        SerializedProperty steps = so.FindProperty("steps");

        for (int i = 0; i < steps.arraySize; i++)
            if (steps.GetArrayElementAtIndex(i).FindPropertyRelative("requiredActionId").stringValue == QuizId)
                return false;

        steps.arraySize++;
        SerializedProperty step = steps.GetArrayElementAtIndex(steps.arraySize - 1);

        // A new array element copies the one before it, so clear every field.
        step.FindPropertyRelative("title").stringValue = QuizTitle;
        step.FindPropertyRelative("targets").arraySize = 0;
        step.FindPropertyRelative("anyTargetCompletes").boolValue = false;
        step.FindPropertyRelative("infoText").stringValue = QuizStepText;
        step.FindPropertyRelative("instructionsText").stringValue = "";
        step.FindPropertyRelative("RequiredOccupiedTable").objectReferenceValue = null;
        step.FindPropertyRelative("RequiredLoadedSlottedTable").objectReferenceValue = null;
        step.FindPropertyRelative("requiredActionId").stringValue = QuizId;
        step.FindPropertyRelative("passiveAction").boolValue = false;
        step.FindPropertyRelative("hintTarget").objectReferenceValue = null;
        step.FindPropertyRelative("advanceOnNext").boolValue = false;
        step.FindPropertyRelative("stationType").objectReferenceValue = null;
        step.FindPropertyRelative("narration").objectReferenceValue = null;
        step.FindPropertyRelative("media").objectReferenceValue = null;
        step.FindPropertyRelative("largeText").boolValue = false;

        so.ApplyModifiedProperties();
        Debug.Log($"{LogTag} Added step {steps.arraySize} '{QuizTitle}' ({QuizId}).", sequenceManager);
        return true;
    }

    private static int FillStepTitles(SequenceManager sequenceManager)
    {
        SerializedObject so = new SerializedObject(sequenceManager);
        SerializedProperty steps = so.FindProperty("steps");
        bool[] used = new bool[KnownTitles.Length];
        int filled = 0;

        for (int i = 0; i < steps.arraySize; i++)
        {
            SerializedProperty step = steps.GetArrayElementAtIndex(i);
            string actionId = step.FindPropertyRelative("requiredActionId").stringValue;
            string infoText = step.FindPropertyRelative("infoText").stringValue;

            for (int k = 0; k < KnownTitles.Length; k++)
            {
                (string knownId, string infoStart, string title) = KnownTitles[k];
                bool matches = !string.IsNullOrEmpty(knownId)
                    ? knownId == actionId
                    : string.IsNullOrEmpty(actionId) && infoText.StartsWith(infoStart);
                if (used[k] || !matches)
                    continue;

                used[k] = true; // each heading once, so the second test_move gets the next one
                SerializedProperty titleProp = step.FindPropertyRelative("title");
                if (string.IsNullOrEmpty(titleProp.stringValue))
                {
                    titleProp.stringValue = title;
                    filled++;
                }
                break;
            }
        }

        so.ApplyModifiedProperties();
        return filled;
    }

    private static void ConfigureSequence(SequenceManager sequenceManager)
    {
        SerializedObject so = new SerializedObject(sequenceManager);

        SerializedProperty logTag = so.FindProperty("logTag");
        if (logTag != null && (string.IsNullOrEmpty(logTag.stringValue) || logTag.stringValue == "[ASRS][Sequence]"))
            logTag.stringValue = "[ASRS][Module2]";

        // Only replace the stock text — a custom message is kept.
        SerializedProperty completion = so.FindProperty("completionText");
        if (completion != null && completion.stringValue == OldCompletionText)
            completion.stringValue = NewCompletionText;

        so.ApplyModifiedProperties();
    }

    // ------------------------------------------------------------ quiz

    // The old Quiz_Manager reloads the scene and replays the steps; Module 1's
    // quiz replaces it. Unhooked and switched off, not deleted.
    private static void RetireOldQuiz(SequenceManager sequenceManager)
    {
        SerializedObject so = new SerializedObject(sequenceManager);
        SerializedProperty oldQuiz = so.FindProperty("quizManager");
        Quiz_Manager manager = oldQuiz != null ? oldQuiz.objectReferenceValue as Quiz_Manager : null;

        if (oldQuiz != null && oldQuiz.objectReferenceValue != null)
        {
            oldQuiz.objectReferenceValue = null;
            so.ApplyModifiedProperties();
            Debug.Log($"{LogTag} Unhooked the old Quiz_Manager from SequenceManager.", sequenceManager);
        }

        if (manager == null)
            manager = Object.FindFirstObjectByType<Quiz_Manager>(FindObjectsInactive.Include);

        if (manager != null && manager.gameObject.activeSelf)
        {
            Undo.RecordObject(manager.gameObject, "Build Module 2");
            manager.gameObject.SetActive(false);
            Debug.Log($"{LogTag} Switched off the old quiz '{manager.name}' (kept in the scene).", manager);
        }
    }

    private static ASRS_Module1_Quiz FindModule2Quiz()
    {
        GameObject features = FindFeaturesRoot();
        return features != null ? features.GetComponentInChildren<ASRS_Module1_Quiz>(true) : null;
    }

    private static GameObject FindFeaturesRoot()
    {
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
        return scene.GetRootGameObjects().FirstOrDefault(g => g.name == FeaturesName);
    }

    private static ASRS_Module1_Quiz GetOrBuildQuiz(SequenceManager sequenceManager, out bool built)
    {
        built = false;
        ASRS_Module1_Quiz quiz = FindModule2Quiz();

        if (quiz == null)
        {
            GameObject features = FindFeaturesRoot();
            if (features == null)
            {
                features = new GameObject(FeaturesName);
                if (features.scene != sequenceManager.gameObject.scene)
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(features, sequenceManager.gameObject.scene);
                Undo.RegisterCreatedObjectUndo(features, "Build Module 2");
            }

            GameObject host = new GameObject(QuizHostName);
            host.transform.SetParent(features.transform, false);
            Undo.RegisterCreatedObjectUndo(host, "Build Module 2");

            quiz = ASRS_Module1_Builder.BuildQuizPanel(host, QuizTitle);
            if (quiz == null)
                return null;

            built = true;
        }

        SerializedObject so = new SerializedObject(quiz);
        so.FindProperty("sequenceManager").objectReferenceValue = sequenceManager;
        so.FindProperty("quizActionId").stringValue = QuizId;
        so.FindProperty("moduleName").stringValue = "Module 2";
        so.FindProperty("cellInspector").objectReferenceValue = null;
        if (built)
            WriteQuestionPool(so);
        so.ApplyModifiedPropertiesWithoutUndo();
        return quiz;
    }

    // Eight multiple-choice questions on what the Module 2 steps teach; each
    // attempt asks four at random, answers shuffled (so every correct answer
    // is written first here). No rack-cell questions.
    private static void WriteQuestionPool(SerializedObject quizSo)
    {
        (string prompt, string[] options, string explanation)[] pool =
        {
            ("What must you do in SCORBASE before it will accept jog, Search Home or Pick and Place commands?",
                new[] { "Go Online", "Run Search Home", "Rotate the arm to the far side", "Turn off the Controller-USB" },
                "SCORBASE only talks to the Controller-USB once it's online — until then nothing you press moves the robot."),

            ("Why do you run Search Home before commanding the arm to a slot?",
                new[] { "It drives each axis to a known reference, so the arm's position can be trusted", "It unlocks the rack doors", "It reads the RFID tag of every stored table", "It switches SCORBASE into simulation mode" },
                "Until every axis has been homed, SCORBASE can't know where the arm really is, so no commanded position is trustworthy."),

            ("What happens when you press an Emergency Stop?",
                new[] { "Power to the arm is cut immediately and the cell halts", "The arm drives back to its Home position", "Only the conveyor belt pauses", "SCORBASE goes offline but the arm keeps moving" },
                "Every E-Stop is wired together, so pressing any one of them — at the controller or on the ASRS — halts the whole cell."),

            ("Which part actually grabs a slotted table and carries it between the rack and the conveyor?",
                new[] { "The pneumatic gripper on the arm", "The RFID sensor", "The Controller-USB", "The conveyor belt" },
                "The pneumatic gripper is the arm's \"hand\" — it grips the template by its handle."),

            ("How does the ASRS know which slotted table has arrived on the conveyor?",
                new[] { "The RFID sensor reads the table's tag", "The operator types its ID into SCORBASE", "The gripper weighs the table", "SCORBASE assumes tables arrive in slot order" },
                "Each slotted table carries an RFID tag; the sensor reads it as the table passes, so no one has to look."),

            ("In this lesson, what order do you bring the ASRS up in?",
                new[] { "Laptop on → Controller-USB on → Go Online → Search Home", "Search Home → Go Online → Controller-USB on → Laptop on", "Controller-USB on → Search Home → Laptop on → Go Online", "Go Online → Laptop on → Search Home → Controller-USB on" },
                "Both machines are powered first, then SCORBASE goes online, and only then can it home the arm."),

            ("Why does the arm rotate before it reaches a slot in rows 1–6 (e.g. 010001)?",
                new[] { "Those slots are on the other side of the rack from where the arm is facing", "Rotating homes the arm", "Rows 1–6 are on the conveyor, not the rack", "Rotating unlocks the gripper" },
                "The ASRS-36x2 has two racks facing each other; the arm turns to face whichever side holds the slot."),

            ("What is SCORBASE's Manual Movement used for?",
                new[] { "Jogging the robot by hand, one axis at a time", "Running a saved program automatically", "Powering the Controller-USB on and off", "Reading a table's RFID tag" },
                "Manual Movement (View > Manual Movement) positions or tests the robot by hand instead of sending it to a saved position."),
        };

        SerializedProperty questions = quizSo.FindProperty("questions");
        questions.arraySize = pool.Length;
        for (int i = 0; i < pool.Length; i++)
            ASRS_Module1_Builder.WriteQuestion(questions.GetArrayElementAtIndex(i), pool[i].prompt, pool[i].options, 0, pool[i].explanation, false, "");

        quizSo.FindProperty("passMark").intValue = QuizPassMark;
        quizSo.FindProperty("multipleChoicePerAttempt").intValue = QuizQuestionsPerAttempt;
        quizSo.FindProperty("cellQuestionsPerAttempt").intValue = 0;
        quizSo.FindProperty("shuffleOptions").boolValue = true;
        quizSo.FindProperty("randomCellMaterial").boolValue = false;
    }
}
