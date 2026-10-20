using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Read-only checks for Module 1 in the open scene — never modifies the
// scene, the rack inventory, or anything that moves. Every finding is one
// Console line under [ASRS][Module1][Validate]; click it to ping the object.
public static class ASRS_Module1_Validator
{
    private const string Tag = "[ASRS][Module1][Validate]";

    private sealed class Report
    {
        public int Errors, Warnings;
        public void Error(string message, Object context = null) { Errors++; Debug.LogError($"{Tag} {message}", context); }
        public void Warn(string message, Object context = null) { Warnings++; Debug.LogWarning($"{Tag} {message}", context); }
        public void Ok(string message, Object context = null) => Debug.Log($"{Tag} OK — {message}", context);
    }

    [MenuItem("ASRS/Module 1/Validate Scene")]
    public static void Validate()
    {
        Report r = new Report();

        SequenceManager[] managers = Object.FindObjectsByType<SequenceManager>(FindObjectsSortMode.None);
        if (managers.Length != 1)
        {
            r.Error($"Expected exactly one active SequenceManager, found {managers.Length}.");
            Finish(r);
            return;
        }

        SequenceManager sm = managers[0];
        SequenceManager.Step[] steps = sm.Steps ?? new SequenceManager.Step[0];

        CheckSequenceReferences(r, sm);
        Dictionary<string, int> ids = CheckSteps(r, sm, steps);
        CheckOrder(r, sm, ids);

        Item_ASRS rack = ASRS_Module1_Builder.FindLessonRack();
        CheckRack(r, rack);
        CheckSlotInsert(r, sm, rack, ids);

        CheckDiagram(r, ids);
        ASRS_Rack_Cell_Inspector inspector = CheckInspector(r, rack, ids);
        CheckQuiz(r, inspector, ids);
        CheckEmitters(r, steps);

        Finish(r);
    }

    private static void Finish(Report r)
    {
        string summary = $"{Tag} Finished — {r.Errors} error(s), {r.Warnings} warning(s).";
        if (r.Errors > 0) Debug.LogError(summary);
        else if (r.Warnings > 0) Debug.LogWarning(summary);
        else Debug.Log(summary);
    }

    // ------------------------------------------------------------ sequence

    private static void CheckSequenceReferences(Report r, SequenceManager sm)
    {
        Lesson_Panel lessonPanel = Object.FindFirstObjectByType<Lesson_Panel>();

        if (Get<Object>(sm, "infoPanel") == null)
            r.Error("SequenceManager has no Info Panel — no lesson text will show.", sm);

        if (Get<Object>(sm, "nextButton") == null)
            r.Error("SequenceManager has no Next Button — info steps can't advance.", sm);

        Station_Type_Card card = Get<Station_Type_Card>(sm, "stationCard") ?? (lessonPanel != null ? lessonPanel.MediaCard : null);
        if (card == null)
            r.Warn("No Station_Type_Card (SequenceManager.stationCard or Lesson_Panel.MediaCard) — station images and the carousel video won't show.", sm);
        else
        {
            if (Get<Object>(card, "videoPlayer") == null || Get<Object>(card, "videoSurface") == null)
                r.Warn("Station_Type_Card has no Video Player / Video Surface — the carousel video can't play.", card);
            if (Get<Object>(card, "videoControls") == null)
                r.Warn("Station_Type_Card has no video controls — run 'ASRS/Module 1/Add Video Controls To Lesson Panel Prefab' for Pause/Replay.", card);
        }

        if (Get<Object>(sm, "quizManager") != null)
            r.Warn("SequenceManager has a Quiz_Manager assigned. Module 1 uses ASRS_Module1_Quiz; the old quiz would wait forever on Module 1's labelled info steps.", sm);
    }

    private static Dictionary<string, int> CheckSteps(Report r, SequenceManager sm, SequenceManager.Step[] steps)
    {
        Dictionary<string, int> ids = new Dictionary<string, int>();
        if (steps.Length == 0)
            r.Error("SequenceManager has no steps.", sm);

        int stationSteps = 0;
        for (int i = 0; i < steps.Length; i++)
        {
            SequenceManager.Step s = steps[i];
            string label = $"Step {i + 1}";

            bool hasTargets = s.targets != null && s.targets.Any(t => t != null);
            bool hasTable = s.RequiredOccupiedTable != null || s.RequiredLoadedSlottedTable != null;
            bool hasAction = !string.IsNullOrEmpty(s.requiredActionId);

            if (!hasTargets && !hasTable && !hasAction && !s.advanceOnNext)
                r.Error($"{label} has no targets, table, action id or Advance On Next — it can never complete.", sm);

            if (s.targets != null)
                foreach (GameObject t in s.targets)
                    if (t != null && t.GetComponent<Marker_Interactable>() == null)
                        r.Error($"{label} target '{t.name}' has no Marker_Interactable.", t);

            if (hasAction)
            {
                if (ids.ContainsKey(s.requiredActionId))
                    r.Error($"{label} repeats action id '{s.requiredActionId}' (also step {ids[s.requiredActionId] + 1}) — NotifyAction can't tell them apart.", sm);
                else
                    ids[s.requiredActionId] = i;
            }

            if (s.stationType != null)
            {
                stationSteps++;
                if (string.IsNullOrEmpty(s.stationType.displayName) || string.IsNullOrEmpty(s.stationType.summary))
                    r.Error($"{label}: station type '{s.stationType.name}' has no name or summary.", s.stationType);
                if (s.stationType.displayName == "Carousel" && s.stationType.video == null)
                    r.Warn($"{label}: the Carousel has no video assigned — restore Carousel_Overview.mp4 (the card shows its placeholder meanwhile).", s.stationType);
            }
        }

        if (stationSteps != 3)
            r.Warn($"Found {stationSteps} station-type step(s); Module 1 teaches three (ASRS-36x2u, ASRS-36x2, Carousel) — run 'ASRS/Module 1/Update Intro (3 Stations) And Step Media'.", sm);
        else
            r.Ok("three station-type steps present.");

        foreach (SequenceManager.Step s in steps)
            if (s.media != null && s.media.image == null && s.media.video == null)
                r.Warn($"Step media '{s.media.name}' has no picture yet — assign its Image to show it in the media box.", s.media);

        foreach (string required in new[]
                 {
                     ASRS_Module1_Builder.InspectCellsId,
                     ASRS_Module1_Builder.IdentifyMaterialId,
                     ASRS_Module1_Builder.LoadAsrsId, ASRS_Module1_Builder.QuizId,
                 })
        {
            if (!ids.ContainsKey(required))
                r.Error($"No step uses action id '{required}' — run 'ASRS/Module 1/Build Module 1 In Current Scene'.", sm);
        }

        for (int n = 1; n <= 6; n++)
            if (!ids.ContainsKey(ASRS_Module1_Builder.FlowPrefix + n))
                r.Warn($"Material-flow step '{ASRS_Module1_Builder.FlowPrefix}{n}' is missing.", sm);

        return ids;
    }

    private static void CheckOrder(Report r, SequenceManager sm, Dictionary<string, int> ids)
    {
        void Before(string a, string b, string why)
        {
            if (ids.TryGetValue(a, out int ia) && ids.TryGetValue(b, out int ib) && ia > ib)
                r.Error($"'{a}' (step {ia + 1}) comes after '{b}' (step {ib + 1}) — {why}", sm);
        }

        Before(ASRS_Module1_Builder.LoadSlottedTableId, ASRS_Module1_Builder.LoadAsrsId, "the template must be loaded before it goes into the rack.");
        Before(ASRS_Module1_Builder.InspectCellsId, ASRS_Module1_Builder.IdentifyMaterialId, "trainees should inspect cells before the identification exercise.");
        Before(ASRS_Module1_Builder.LoadAsrsId, ASRS_Module1_Builder.QuizId, "passing the quiz must not bypass the physical rack loading.");

        if (ids.TryGetValue(ASRS_Module1_Builder.QuizId, out int quiz) && quiz != sm.Steps.Length - 1)
            r.Warn($"The quiz is step {quiz + 1} of {sm.Steps.Length}, not the last step.", sm);
    }

    // ---------------------------------------------------------------- rack

    private static void CheckRack(Report r, Item_ASRS rack)
    {
        int active = Object.FindObjectsByType<Item_ASRS>(FindObjectsSortMode.None).Length;
        if (rack == null)
        {
            r.Error("No active Item_ASRS in the scene.");
            return;
        }

        if (active > 1)
            r.Warn($"{active} active Item_ASRS components — only '{rack.name}' is used by Module 1; deactivate the others.", rack);

        if (rack.EpoxyBlockPrefab == null)
            r.Error("Item_ASRS has no EpoxyBlockPrefab — rows without a MaterialList entry will be empty.", rack);

        for (int i = 0; i < (rack.MaterialList?.Count ?? 0); i++)
            if (rack.MaterialList[i] == null)
                r.Warn($"Item_ASRS MaterialList[{i}] (row {i + 1}) is empty — that row falls back to EpoxyBlockPrefab.", rack);

        Item_Slotted_Table[] tables = rack.GetComponentsInChildren<Item_Slotted_Table>(true);
        HashSet<string> addresses = new HashSet<string>(tables.Select(t => t.TableID).Where(id => ASRS_Rack_Addressing.TryParse(id, out _, out _)));
        if (addresses.Count != ASRS_Rack_Addressing.CellCount)
            r.Warn($"Rack has {addresses.Count} distinct valid TableIDs (expected {ASRS_Rack_Addressing.CellCount}) — cells without a table have no anchor and can't be inspected.", rack);
        else
            r.Ok($"rack '{rack.name}' has all {ASRS_Rack_Addressing.CellCount} slot tables.", rack);

        if (!tables.Any(t => t.GetComponent<BoxCollider>() != null))
            r.Warn("No rack slotted table has a BoxCollider — cell targets will use the inspector's fallback size.", rack);
    }

    private static void CheckSlotInsert(Report r, SequenceManager sm, Item_ASRS rack, Dictionary<string, int> ids)
    {
        Item_ASRS_Manual_Slot_Insert[] inserts = Object.FindObjectsByType<Item_ASRS_Manual_Slot_Insert>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (inserts.Length == 0)
        {
            r.Error("No Item_ASRS_Manual_Slot_Insert in the scene — the 'LoadASRS' step can never complete. Run 'ASRS/Hook Up ASRS Variant 1 (Module 1)'.");
            return;
        }

        foreach (Item_ASRS_Manual_Slot_Insert insert in inserts)
        {
            if (insert.Rack == null)
                r.Error($"Slot insert on '{insert.name}' has no Rack.", insert);
            else if (rack != null && insert.Rack != rack)
                r.Error($"Slot insert on '{insert.name}' points at '{insert.Rack.name}', not the lesson rack '{rack.name}'.", insert);

            if (insert.LessonSequence == null)
                r.Error($"Slot insert on '{insert.name}' has no SequenceManager — inserting won't complete the step.", insert);

            if (!ids.ContainsKey(insert.NotifyActionId))
                r.Error($"Slot insert on '{insert.name}' notifies '{insert.NotifyActionId}', which no step waits for.", insert);

            Collider c = insert.GetComponent<Collider>();
            if (c != null && !c.isTrigger)
                r.Error($"Slot insert on '{insert.name}': its Collider must be a trigger.", insert);
        }

        // The lesson's carryable table must have an empty home slot.
        Item_Slotted_Table lessonTable = Object.FindObjectsByType<Item_Slotted_Table>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(t => t.Pickup && t.GetComponentInParent<Item_ASRS>(true) == null);
        if (lessonTable == null)
        {
            r.Error("No carryable (Pickup) slotted table outside the rack — there's nothing for the trainee to load into the ASRS.");
            return;
        }

        if (!ASRS_Rack_Addressing.TryParse(lessonTable.TableID, out int row, out _))
            r.Error($"Lesson table '{lessonTable.name}' has an invalid TableID '{lessonTable.TableID}'.", lessonTable);
        else if (row != ASRS_Rack_Addressing.VacantRow)
            r.Error($"Lesson table '{lessonTable.name}' has TableID {lessonTable.TableID}, but only row {ASRS_Rack_Addressing.VacantRow} starts empty — its home slot will already be occupied.", lessonTable);
        else if (!inserts.Any(i => i.TryGetComponent(out Item_Slotted_Table slot) && slot.TableID == lessonTable.TableID))
            r.Error($"No slot insert sits on slot {lessonTable.TableID} for lesson table '{lessonTable.name}'.", lessonTable);
        else
            r.Ok($"lesson table {lessonTable.TableID} has a wired, empty home slot.", lessonTable);
    }

    // ------------------------------------------------------------ features

    private static void CheckFeatureBasics(Report r, ASRS_Module1_Step_Feature feature)
    {
        GameObject root = Get<GameObject>(feature, "root");
        if (root == null)
            r.Error($"{feature.GetType().Name} has no Root panel.", feature);
        else if (root == feature.gameObject)
            r.Error($"{feature.GetType().Name}'s Root is its own GameObject — hiding it would stop it hearing step changes. Put the panel on a child.", feature);

        if (!feature.gameObject.activeInHierarchy)
            r.Error($"{feature.GetType().Name} host '{feature.name}' is inactive — it must stay active.", feature);

        if (Get<Object>(feature, "sequenceManager") == null)
            r.Warn($"{feature.GetType().Name} has no SequenceManager assigned (it will search for one at runtime).", feature);
    }

    private static void CheckDiagram(Report r, Dictionary<string, int> ids)
    {
        ASRS_Pallet_Circulation_Diagram diagram = Object.FindFirstObjectByType<ASRS_Pallet_Circulation_Diagram>();
        if (diagram == null)
        {
            r.Error("No ASRS_Pallet_Circulation_Diagram in the scene.");
            return;
        }

        CheckFeatureBasics(r, diagram);

        if (diagram.StageCount != 6)
            r.Error($"Pallet diagram has {diagram.StageCount} stages; the material flow has 6.", diagram);
        else if (diagram.Stages.Any(s => s.node == null || string.IsNullOrEmpty(s.title)))
            r.Error("A pallet diagram stage is missing its node or title.", diagram);

        if (diagram.Pallet == null)
            r.Error("Pallet diagram has no Pallet icon.", diagram);

        foreach (string field in new[] { "playPauseButton", "replayButton", "previousButton", "nextButton", "stageDescriptionText" })
            if (Get<Object>(diagram, field) == null)
                r.Warn($"Pallet diagram field '{field}' is unassigned.", diagram);

        if (ids.ContainsKey(diagram.AnimationActionId))
            r.Warn($"Step '{diagram.AnimationActionId}' (auto-play lap) is still in the lesson — run 'ASRS/Module 1/Update Intro (3 Stations) And Step Media' to remove it.", diagram);
        else
            r.Ok("pallet diagram wired to the material-flow steps.", diagram);
    }

    private static ASRS_Rack_Cell_Inspector CheckInspector(Report r, Item_ASRS rack, Dictionary<string, int> ids)
    {
        ASRS_Rack_Cell_Inspector inspector = Object.FindFirstObjectByType<ASRS_Rack_Cell_Inspector>();
        if (inspector == null)
        {
            r.Error("No ASRS_Rack_Cell_Inspector in the scene.");
            return null;
        }

        CheckFeatureBasics(r, inspector);

        if (inspector.Rack == null)
            r.Error("Cell inspector has no Rack.", inspector);
        else if (rack != null && inspector.Rack != rack)
            r.Error($"Cell inspector points at '{inspector.Rack.name}', not the lesson rack '{rack.name}'.", inspector);

        if (Get<Object>(inspector, "highlightMaterial") == null)
            r.Warn("Cell inspector has no highlight material — the selected cell will use Unity's default material.", inspector);

        foreach (string field in new[] { "promptText", "cellText", "feedbackText" })
            if (Get<Object>(inspector, field) == null)
                r.Error($"Cell inspector field '{field}' is unassigned.", inspector);

        if (!ids.ContainsKey(inspector.InspectActionId) || !ids.ContainsKey(inspector.IdentifyActionId))
            r.Error("Cell inspector's action ids aren't both used by steps.", inspector);

        string predicted = PredictMaterial(inspector.Rack ?? rack, inspector.IdentifyReferenceTableId, r, "Identification reference", inspector);
        if (predicted != null && (inspector.MaterialNotes == null || !inspector.MaterialNotes.Any(n => n != null && n.materialName == predicted && !string.IsNullOrEmpty(n.description))))
            r.Warn($"No description for '{predicted}' in the inspector's Material Notes.", inspector);

        return inspector;
    }

    private static void CheckQuiz(Report r, ASRS_Rack_Cell_Inspector inspector, Dictionary<string, int> ids)
    {
        ASRS_Module1_Quiz quiz = Object.FindFirstObjectByType<ASRS_Module1_Quiz>();
        if (quiz == null)
        {
            r.Error("No ASRS_Module1_Quiz in the scene.");
            return;
        }

        CheckFeatureBasics(r, quiz);

        ASRS_Module1_Quiz.Question[] questions = quiz.Questions ?? new ASRS_Module1_Quiz.Question[0];
        if (questions.Length != 3)
            r.Error($"Quiz has {questions.Length} question(s); Module 1 requires exactly 3.", quiz);
        if (quiz.PassMark != 2)
            r.Warn($"Quiz pass mark is {quiz.PassMark}; the specification is 2 of 3.", quiz);

        int maxOptions = 0;
        for (int i = 0; i < questions.Length; i++)
        {
            ASRS_Module1_Quiz.Question q = questions[i];
            string label = $"Quiz Q{i + 1}";
            if (string.IsNullOrWhiteSpace(q.prompt))
                r.Error($"{label} has no prompt.", quiz);

            if (q.rackCellQuestion)
            {
                if (quiz.CellInspector == null)
                    r.Error($"{label} is a rack-cell question but the quiz has no Cell Inspector.", quiz);
                PredictMaterial(inspector != null ? inspector.Rack : null, q.cellTableId, r, label, quiz);
            }
            else
            {
                int count = q.options?.Count(o => !string.IsNullOrWhiteSpace(o)) ?? 0;
                maxOptions = Mathf.Max(maxOptions, count);
                if (count < 2)
                    r.Error($"{label} has fewer than two answer options.", quiz);
                if (q.correctOption < 0 || q.correctOption >= count)
                    r.Error($"{label}'s correct option ({q.correctOption}) is out of range.", quiz);
            }
        }

        Button_Count(r, quiz, maxOptions);

        foreach (string field in new[] { "questionText", "feedbackText", "nextButton", "retryButton", "continueButton" })
            if (Get<Object>(quiz, field) == null)
                r.Error($"Quiz field '{field}' is unassigned.", quiz);

        if (!ids.ContainsKey(quiz.QuizActionId))
            r.Error($"Quiz action '{quiz.QuizActionId}' isn't used by any step.", quiz);
        else
            r.Ok("three-question quiz configured.", quiz);
    }

    private static void Button_Count(Report r, ASRS_Module1_Quiz quiz, int maxOptions)
    {
        int buttons = quiz.OptionButtons?.Count(b => b != null) ?? 0;
        if (buttons < maxOptions)
            r.Error($"Quiz has {buttons} option button(s) but a question needs {maxOptions}.", quiz);
    }

    // Every gated action id should have something in the scene that raises it.
    private static void CheckEmitters(Report r, SequenceManager.Step[] steps)
    {
        HashSet<string> emitted = new HashSet<string>();
        foreach (ASRS_Module1_Step_Feature f in Object.FindObjectsByType<ASRS_Module1_Step_Feature>(FindObjectsSortMode.None))
            foreach (SequenceManager.Step s in steps)
                if (!string.IsNullOrEmpty(s.requiredActionId) && f.HandlesAction(s.requiredActionId))
                    emitted.Add(s.requiredActionId);

        foreach (Item_ASRS_Manual_Slot_Insert insert in Object.FindObjectsByType<Item_ASRS_Manual_Slot_Insert>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            emitted.Add(insert.NotifyActionId);

        for (int i = 0; i < steps.Length; i++)
        {
            SequenceManager.Step s = steps[i];
            bool gatedOnActionOnly = !string.IsNullOrEmpty(s.requiredActionId) && !s.advanceOnNext
                                     && (s.targets == null || !s.targets.Any(t => t != null))
                                     && s.RequiredOccupiedTable == null && s.RequiredLoadedSlottedTable == null;

            if (gatedOnActionOnly && !emitted.Contains(s.requiredActionId))
                r.Warn($"Step {i + 1} waits for '{s.requiredActionId}', but no Module 1 component in the scene raises it (fine only if a UnityEvent calls NotifyAction with it).");
        }
    }

    // What a cell will hold at runtime, predicted from Item_ASRS's load
    // rules (row 5 empty; row N -> MaterialList[N-1] or EpoxyBlockPrefab).
    private static string PredictMaterial(Item_ASRS rack, string tableId, Report r, string label, Object context)
    {
        if (!ASRS_Rack_Addressing.TryParse(tableId, out int row, out _))
        {
            r.Error($"{label}: '{tableId}' isn't a valid rack address (row 01-12, column 1-6).", context);
            return null;
        }

        if (row == ASRS_Rack_Addressing.VacantRow)
        {
            r.Warn($"{label}: cell {tableId} is in the vacant row {ASRS_Rack_Addressing.VacantRow} — the first occupied cell will be used instead.", context);
            return null;
        }

        if (rack == null)
            return null;

        GameObject prefab = row - 1 < (rack.MaterialList?.Count ?? 0) && rack.MaterialList[row - 1] != null ? rack.MaterialList[row - 1] : rack.EpoxyBlockPrefab;
        if (prefab == null)
            return null;

        Item_Parent part = prefab.GetComponent<Item_Parent>();
        string name = part != null && !string.IsNullOrWhiteSpace(part.Name) ? part.Name : prefab.name;
        r.Ok($"{label}: cell {tableId} should hold '{name}'.", context);
        return name;
    }

    private static T Get<T>(Object component, string property) where T : Object
    {
        SerializedProperty prop = new SerializedObject(component).FindProperty(property);
        return prop != null ? prop.objectReferenceValue as T : null;
    }
}
