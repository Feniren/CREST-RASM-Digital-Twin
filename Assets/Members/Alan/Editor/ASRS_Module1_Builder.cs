using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

// Builds the parts of Module 1 that don't exist yet in the open scene and
// wires them up, without touching anything that does:
//
//   Module1_Features                      (always active — hosts the components)
//   ├ PalletCirculationDiagram            ASRS_Pallet_Circulation_Diagram
//   │ └ DiagramPanel                      world-space canvas (shown on its steps)
//   ├ RackCellInspector                   ASRS_Rack_Cell_Inspector
//   │ └ CellInfoPanel                     world-space canvas (follows the selected cell)
//   └ Module1Quiz                         ASRS_Module1_Quiz
//     └ QuizPanel                         world-space canvas (shown on the quiz step)
//
// and inserts the Module 1 steps that are missing from SequenceManager —
// matched by Required Action Id, so re-running never duplicates a step and
// never edits one that's already there (Inspector edits survive).
//
// Panels already built are only re-wired, never rebuilt; delete a panel's
// GameObject to have it rebuilt. Everything is logged; run the validator
// afterwards ("ASRS/Module 1/Validate Scene" — it runs automatically here).
public static class ASRS_Module1_Builder
{
    private const string FeaturesName = "Module1_Features";
    private const string BackgroundMaterialPath = "Assets/Materials/UIBackground.mat";
    private const string GradientSpritePath = "Assets/Materials/Texture/UIGradient.png";
    private const string ListItemButtonPath = "Assets/Game_Objects/UserInterface/ListItemButton.prefab";
    private const string HighlightMaterialPath = "Assets/Materials/Green_Glass.mat";
    private const string LessonPanelPrefabPath = "Assets/Members/Alan/Training/UI/LessonPanel.prefab";

    private const float PanelScale = 0.01f; // canvas units are centimetres

    private static readonly Color Cyan = new Color(0.3764706f, 0.8509804f, 1f, 1f);
    private static readonly Color BodyColor = new Color(0.88f, 0.93f, 1f, 1f);
    private static readonly Color MutedColor = new Color(0.62f, 0.7f, 0.8f, 1f);
    private static readonly Color NodeColor = new Color(0.12f, 0.2f, 0.3f, 0.95f);
    private static readonly Color PalletColor = new Color(1f, 0.62f, 0.2f, 1f);

    // Module 1 action ids. Info steps carry one too, purely as a label (they
    // still advance on Next) so features and this builder can find them.
    public const string WelcomeId = "M1_Welcome";
    public const string StorageRolesId = "M1_StorageRoles";
    public const string FlowPrefix = "M1_Flow_";
    public const string PalletCirculationId = "M1_PalletCirculation";
    public const string InspectCellsId = "M1_InspectCells";
    public const string IdentifyMaterialId = "M1_IdentifyMaterial";
    public const string LoadSlottedTableId = "LoadSlottedTable";
    public const string LoadAsrsId = "LoadASRS";
    public const string QuizId = "M1_QuizPassed";

    private const string OldCompletionText = "Great job — you've identified every part. Let's test what you've learned.";
    private const string NewCompletionText = "Module 1 complete — you passed the check. You can now describe what an ASRS does, name the three storage station types, follow a pallet around the SmartCIM cell and find parts by their cell address.";

    private struct StepDef
    {
        public string Id, Title, Info, Instructions;
        public bool AdvanceOnNext;

        public StepDef(string id, string title, string info, string instructions, bool advanceOnNext)
        {
            Id = id; Title = title; Info = info; Instructions = instructions; AdvanceOnNext = advanceOnNext;
        }
    }

    private static readonly (string title, string description, int node)[] FlowStages =
    {
        ("Raw material into storage", "Raw materials are loaded into templates, and the loaded templates are stored in addressable ASRS cells.", 0),
        ("Robot retrieval", "When a job needs a part, the ASRS robot retrieves the requested template from its cell.", 1),
        ("Conveyor & PLC routing", "The template rides a pallet onto the conveyor. PLC-controlled logic routes the pallet to the station that needs it.", 2),
        ("Processing stations", "CNC and assembly stations machine and assemble the parts. (Shown as a concept — those stations aren't modelled in this scene.)", 3),
        ("Return conveyor", "Processed parts go back onto the conveyor, which carries the pallet back toward the ASRS.", 4),
        ("Finished goods stored", "The completed product is stored back in the ASRS as a finished good, ready for the next request.", 0),
    };

    private static readonly (string label, Vector2 position)[] DiagramNodes =
    {
        ("ASRS Storage", new Vector2(-40f, 9f)),
        ("Robot Retrieval", new Vector2(-14f, 21f)),
        ("Conveyor / PLC", new Vector2(22f, 21f)),
        ("CNC & Assembly", new Vector2(42f, 7f)),
        ("Return Conveyor", new Vector2(2f, -5f)),
    };

    private static readonly (string name, string description)[] KnownMaterials =
    {
        ("Epoxy Block", "Raw epoxy stock — the standard part loaded onto templates in this cell."),
        ("Epoxy Penholder", "A machined epoxy penholder — the finished product of the mill's epoxy penholder job."),
        ("Tube", "Metal tube stock — raw material for the lathe's metal penholder job."),
    };

    [MenuItem("ASRS/Module 1/Build Module 1 In Current Scene")]
    public static void BuildInScene()
    {
        SequenceManager sequenceManager = Object.FindFirstObjectByType<SequenceManager>();
        if (sequenceManager == null)
        {
            Debug.LogError("[ASRS][Module1][Build] No SequenceManager in the open scene — open ASRSModule1 first.");
            return;
        }

        Material background = AssetDatabase.LoadAssetAtPath<Material>(BackgroundMaterialPath);
        Sprite gradient = AssetDatabase.LoadAssetAtPath<Sprite>(GradientSpritePath);
        GameObject listItemButton = AssetDatabase.LoadAssetAtPath<GameObject>(ListItemButtonPath);
        if (background == null || listItemButton == null)
        {
            Debug.LogError($"[ASRS][Module1][Build] Missing widget assets — need {BackgroundMaterialPath} and {ListItemButtonPath}.");
            return;
        }

        Item_ASRS rack = FindLessonRack();
        if (rack == null)
            Debug.LogWarning("[ASRS][Module1][Build] No active Item_ASRS found — the cell inspector will need its Rack assigned by hand.");

        Transform reference = FindPanelReference(sequenceManager);
        GameObject features = GetOrCreateRoot(sequenceManager);
        UI ui = new UI(background, gradient, listItemButton);

        ASRS_Pallet_Circulation_Diagram diagram = GetOrBuild(features, "PalletCirculationDiagram", host => BuildDiagram(host, ui));
        ASRS_Rack_Cell_Inspector inspector = GetOrBuild(features, "RackCellInspector", host => BuildCellInspector(host, ui));
        ASRS_Module1_Quiz quiz = GetOrBuild(features, "Module1Quiz", host => BuildQuiz(host, ui));

        PlaceNewPanels(reference, diagram, inspector, quiz);

        SetRef(diagram, "sequenceManager", sequenceManager);
        SetRef(inspector, "sequenceManager", sequenceManager);
        SetRef(quiz, "sequenceManager", sequenceManager);
        SetRef(quiz, "cellInspector", inspector);
        if (rack != null)
        {
            SetRef(inspector, "rack", rack);
            FillMaterialNotes(inspector, rack);
        }

        AssignLessonTable(inspector, rack);

        SetRefIfEmpty(inspector, "highlightMaterial", AssetDatabase.LoadAssetAtPath<Material>(HighlightMaterialPath));

        int added = InsertMissingSteps(sequenceManager);
        ConfigureSequence(sequenceManager);
        ApplyIntroAndMedia(sequenceManager, quiz);

        EditorSceneManager.MarkSceneDirty(sequenceManager.gameObject.scene);
        Selection.activeGameObject = features;
        Debug.Log($"[ASRS][Module1][Build] Done — {added} step(s) added. Save the scene. Running the validator…", features);

        ASRS_Module1_Validator.Validate();
    }

    // Puts ASRS_Module1_Debug_Shortcuts on the SequenceManager's GameObject
    // (once). Remove the component — or leave it; it switches itself off in
    // release builds — when you're done testing.
    [MenuItem("ASRS/Module 1/Add Debug Step Shortcuts")]
    public static void AddDebugShortcuts()
    {
        SequenceManager sequenceManager = Object.FindFirstObjectByType<SequenceManager>();
        if (sequenceManager == null)
        {
            Debug.LogError("[ASRS][Module1][Build] No SequenceManager in the open scene — open ASRSModule1 first.");
            return;
        }

        if (!sequenceManager.TryGetComponent(out ASRS_Module1_Debug_Shortcuts shortcuts))
        {
            shortcuts = Undo.AddComponent<ASRS_Module1_Debug_Shortcuts>(sequenceManager.gameObject);
            Debug.Log("[ASRS][Module1][Build] Debug step shortcuts added. Save the scene. In Play mode: ] / [ next / previous, Home restart, Shift+1..7 jump to a section, \\ toggles the help box; on Quest click both thumbsticks in to skip.", shortcuts);
        }
        else
        {
            Debug.Log("[ASRS][Module1][Build] Debug step shortcuts are already on the SequenceManager.", shortcuts);
        }

        SetRef(shortcuts, "sequenceManager", sequenceManager);
        EditorSceneManager.MarkSceneDirty(sequenceManager.gameObject.scene);
        Selection.activeGameObject = sequenceManager.gameObject;
    }

    // ------------------------------------------- three-station intro + media

    private const string StationTypesFolder = "Assets/Members/Alan/Training/StationTypes/";

    // The intro teaches these, in this order. The Rack step is removed (its
    // asset is kept, just no longer used by a step).
    private static readonly string[] IntroStationOrder = { "StationType_ASRS36u", "StationType_ASRS36", "StationType_Carousel" };
    private const string RemovedStation = "StationType_Rack";

    private const string OldIntroInstructionsStart = "There are four storage station types";
    private const string LoadInstructions = "Point at the loaded slotted table and pull the trigger to put it in your inventory. Then point at its home cell (outlined in the rack) and pull the trigger to load it — you can only load it while a slotted table is in your inventory. Carrying it in by hand works too.";
    private const string OldInspectInstructions = "Point at any storage cell in the rack and press the trigger to inspect it.";
    private const string InspectInstructions = "Aim your hand's ray at any storage cell in the rack — it turns solid white over a cell — and pull the index-finger trigger (left hand recommended; on desktop, aim with the screen centre and left-click).";
    private const string NewIntroInstructions = "There are three storage station types: the ASRS-36x2u, the ASRS-36x2 and the Carousel. Press Next to review each one.";

    // Display-only pictures for part-identification steps, matched on the
    // step's title or how its text starts. Only filled where Media is empty.
    private static readonly (string asset, string title, string infoTextStart)[] StepMedia =
    {
        ("StationType_SmartCIM", "Storage Stations", "Before working with the station"),
        ("StationType_Template", "Slotted Table Buffer", "This is the slotted table buffer"),
        ("StationType_RFIDTag", "RFID Tag", "This is the RFID tag"),
        ("StationType_EpoxyBlock", "Epoxy Block", "This is the epoxy block"),
        ("StationType_ASRS", "The ASRS Unit", "This is the ASRS-36"),
        ("StationType_DoorHandle", "Rack Door", "Please walk over to the rack"),
    };

    private const string StationImagesFolder = "Assets/Members/Alan/Training/StationTypes/Images";

    // Pictures dropped into StationTypes/Images import as plain textures,
    // which the Image field of an ASRS_Station_Type won't accept. This sets
    // every one of them to a single Sprite (no mipmaps, full quality).
    [MenuItem("ASRS/Module 1/Make Station Type Images Into Sprites")]
    public static void MakeStationImagesSprites()
    {
        int changed = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { StationImagesFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
                continue;

            if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            changed++;
            Debug.Log($"[ASRS][Module1][Build] '{path}' is now a Sprite.", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
        }

        Debug.Log($"[ASRS][Module1][Build] {changed} image(s) in {StationImagesFolder} converted to Sprites — drag them onto each StationType asset's Image field.");
    }

    private static readonly string[] QuizStationOptions = { "Carousel", "ASRS-36x2", "ASRS-36x2u" };
    private const string QuizStationExplanation = "The carousel turns to bring stock to one fixed access point. The ASRS-36x2 and ASRS-36x2u use a Cartesian robot that travels to an addressed cell instead.";

    [MenuItem("ASRS/Module 1/Update Intro (3 Stations) And Step Media")]
    public static void ApplyIntroAndMediaInScene()
    {
        SequenceManager sequenceManager = Object.FindFirstObjectByType<SequenceManager>();
        if (sequenceManager == null)
        {
            Debug.LogError("[ASRS][Module1][Build] No SequenceManager in the open scene.");
            return;
        }

        ApplyIntroAndMedia(sequenceManager, Object.FindFirstObjectByType<ASRS_Module1_Quiz>());
        ASRS_Rack_Cell_Inspector inspector = Object.FindFirstObjectByType<ASRS_Rack_Cell_Inspector>();
        if (inspector != null)
            AssignLessonTable(inspector, inspector.Rack != null ? inspector.Rack : FindLessonRack());
        EditorSceneManager.MarkSceneDirty(sequenceManager.gameObject.scene);
        Debug.Log("[ASRS][Module1][Build] Intro and step media updated. Save the scene.", sequenceManager);
        ASRS_Module1_Validator.Validate();
    }

    // Safe to re-run: every change checks whether it's already been made.
    private static void ApplyIntroAndMedia(SequenceManager sequenceManager, ASRS_Module1_Quiz quiz)
    {
        SerializedObject so = new SerializedObject(sequenceManager);
        SerializedProperty steps = so.FindProperty("steps");

        // 0. Drop the auto-play Pallet Circulation step — the six flow steps
        //    already walk through the diagram stage by stage.
        int palletStep = IndexOfAction(steps, PalletCirculationId);
        if (palletStep >= 0)
        {
            steps.DeleteArrayElementAtIndex(palletStep);
            Debug.Log($"[ASRS][Module1][Build] Removed the Pallet Circulation step (was step {palletStep + 1}).", sequenceManager);
        }

        // 1. Drop the Rack station step.
        ASRS_Station_Type rack = LoadStation(RemovedStation);
        for (int i = steps.arraySize - 1; i >= 0 && rack != null; i--)
        {
            if (steps.GetArrayElementAtIndex(i).FindPropertyRelative("stationType").objectReferenceValue != rack)
                continue;

            steps.DeleteArrayElementAtIndex(i);
            Debug.Log($"[ASRS][Module1][Build] Removed the Rack station step (was step {i + 1}).", sequenceManager);
        }

        // 2. Station steps in the requested order, in the slots they already occupy.
        List<ASRS_Station_Type> order = IntroStationOrder.Select(LoadStation).Where(s => s != null).ToList();
        List<int> slots = new List<int>();
        for (int i = 0; i < steps.arraySize; i++)
            if (order.Contains(steps.GetArrayElementAtIndex(i).FindPropertyRelative("stationType").objectReferenceValue as ASRS_Station_Type))
                slots.Add(i);

        for (int k = 0, slot = 0; k < order.Count && slot < slots.Count; k++)
        {
            int current = IndexOfStation(steps, order[k]);
            if (current < 0)
                continue;

            if (current != slots[slot])
            {
                steps.MoveArrayElement(current, slots[slot]);
                Debug.Log($"[ASRS][Module1][Build] Moved '{order[k].displayName}' to step {slots[slot] + 1}.", order[k]);
            }
            slot++;
        }

        // 3. Text that still says "four" station types.
        for (int i = 0; i < steps.arraySize; i++)
        {
            SerializedProperty step = steps.GetArrayElementAtIndex(i);
            foreach (string field in new[] { "infoText", "instructionsText" })
            {
                SerializedProperty text = step.FindPropertyRelative(field);
                if (text.stringValue.StartsWith(OldIntroInstructionsStart))
                    text.stringValue = NewIntroInstructions;
                else if (text.stringValue.Contains("four storage station types"))
                    text.stringValue = text.stringValue.Replace("four storage station types", "three storage station types");
            }
        }

        // 4. Template / ASRS pictures on their identification steps.
        foreach ((string asset, string title, string infoTextStart) in StepMedia)
        {
            ASRS_Station_Type media = LoadStation(asset);
            if (media == null)
                continue;

            for (int i = 0; i < steps.arraySize; i++)
            {
                SerializedProperty step = steps.GetArrayElementAtIndex(i);
                SerializedProperty mediaProp = step.FindPropertyRelative("media");
                if (mediaProp == null || mediaProp.objectReferenceValue != null)
                    continue;

                bool matches = step.FindPropertyRelative("title").stringValue == title
                               || step.FindPropertyRelative("infoText").stringValue.StartsWith(infoTextStart);
                if (!matches)
                    continue;

                mediaProp.objectReferenceValue = media;
                Debug.Log($"[ASRS][Module1][Build] Step {i + 1} now shows '{asset}' in the media box — add its picture in the asset's Image field.", media);
            }
        }

        // 5b. Inspection step names the actual button.
        for (int i = 0; i < steps.arraySize; i++)
        {
            SerializedProperty info = steps.GetArrayElementAtIndex(i).FindPropertyRelative("infoText");
            if (info.stringValue.StartsWith(OldInspectInstructions))
                info.stringValue = InspectInstructions + info.stringValue.Substring(OldInspectInstructions.Length);
        }

        // 5c. Load the ASRS step explains the pointer route as well.
        for (int i = 0; i < steps.arraySize; i++)
        {
            SerializedProperty step = steps.GetArrayElementAtIndex(i);
            SerializedProperty instructions = step.FindPropertyRelative("instructionsText");
            if (step.FindPropertyRelative("requiredActionId").stringValue == LoadAsrsId && instructions.stringValue.StartsWith("Carry the slotted table to its empty slot"))
                instructions.stringValue = LoadInstructions;
        }

        // 6. Bigger text on the original hands-on steps — everything after the
        //    last material-flow step except Module 1's own M1_ steps.
        int afterDiagram = -1;
        for (int i = 0; i < steps.arraySize; i++)
            if (steps.GetArrayElementAtIndex(i).FindPropertyRelative("requiredActionId").stringValue.StartsWith(FlowPrefix))
                afterDiagram = i;
        for (int i = afterDiagram + 1; afterDiagram >= 0 && i < steps.arraySize; i++)
        {
            SerializedProperty step = steps.GetArrayElementAtIndex(i);
            SerializedProperty large = step.FindPropertyRelative("largeText");
            if (large == null || large.boolValue || step.FindPropertyRelative("requiredActionId").stringValue.StartsWith("M1_"))
                continue;

            large.boolValue = true;
            Debug.Log($"[ASRS][Module1][Build] Step {i + 1} ('{step.FindPropertyRelative("title").stringValue}') now uses large text.", sequenceManager);
        }

        SerializedProperty completion = so.FindProperty("completionText");
        if (completion != null && completion.stringValue.Contains("four storage station types"))
            completion.stringValue = completion.stringValue.Replace("four storage station types", "three storage station types");

        so.ApplyModifiedProperties();

        // 5. The quiz's station question no longer offers the Rack.
        if (quiz != null)
        {
            SerializedObject quizSo = new SerializedObject(quiz);
            SerializedProperty questions = quizSo.FindProperty("questions");
            for (int i = 0; i < questions.arraySize; i++)
            {
                SerializedProperty q = questions.GetArrayElementAtIndex(i);
                SerializedProperty options = q.FindPropertyRelative("options");
                bool offersRack = Enumerable.Range(0, options.arraySize).Any(o => options.GetArrayElementAtIndex(o).stringValue == "Rack");
                if (!offersRack)
                    continue;

                options.arraySize = QuizStationOptions.Length;
                for (int o = 0; o < QuizStationOptions.Length; o++)
                    options.GetArrayElementAtIndex(o).stringValue = QuizStationOptions[o];
                q.FindPropertyRelative("correctOption").intValue = 0;
                q.FindPropertyRelative("explanation").stringValue = QuizStationExplanation;
                Debug.Log($"[ASRS][Module1][Build] Quiz Q{i + 1} now offers the three intro stations.", quiz);
            }
            quizSo.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    // The loose table the trainee loads into the ASRS — set explicitly so the
    // Load step doesn't depend on the (optional) epoxy-block step.
    private static void AssignLessonTable(ASRS_Rack_Cell_Inspector inspector, Item_ASRS rack)
    {
        if (GetRef<Item_Slotted_Table>(inspector, "lessonTable") != null)
            return;

        Item_Slotted_Table table = ASRS_Rack_Cell_Inspector.FindLooseTable(rack);
        if (table == null)
        {
            Debug.LogWarning("[ASRS][Module1][Build] No loose carryable slotted table found for the Load step — run 'ASRS/Hook Up ASRS Variant 1 (Module 1)'.", inspector);
            return;
        }

        SetRef(inspector, "lessonTable", table);
        Debug.Log($"[ASRS][Module1][Build] Load step uses '{table.name}' (TableID {table.TableID}).", table);
    }

    private static ASRS_Station_Type LoadStation(string assetName)
    {
        return AssetDatabase.LoadAssetAtPath<ASRS_Station_Type>(StationTypesFolder + assetName + ".asset");
    }

    private static int IndexOfStation(SerializedProperty steps, ASRS_Station_Type station)
    {
        for (int i = 0; i < steps.arraySize; i++)
            if (steps.GetArrayElementAtIndex(i).FindPropertyRelative("stationType").objectReferenceValue == station)
                return i;
        return -1;
    }

    [MenuItem("ASRS/Module 1/Add Video Controls To Lesson Panel Prefab")]
    public static void AddVideoControlsToLessonPanelPrefab()
    {
        GameObject listItemButton = AssetDatabase.LoadAssetAtPath<GameObject>(ListItemButtonPath);
        if (listItemButton == null || AssetDatabase.LoadAssetAtPath<GameObject>(LessonPanelPrefabPath) == null)
        {
            Debug.LogError($"[ASRS][Module1][Build] Need {LessonPanelPrefabPath} and {ListItemButtonPath}.");
            return;
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(LessonPanelPrefabPath);
        try
        {
            Station_Type_Card card = contents.GetComponentInChildren<Station_Type_Card>(true);
            GameObject mediaRoot = card != null ? GetRef<GameObject>(card, "mediaRoot") : null;
            if (mediaRoot == null)
            {
                Debug.LogError("[ASRS][Module1][Build] LessonPanel has no Station_Type_Card with a Media Root — nothing to add controls to.");
                return;
            }

            if (AddVideoControls(card, mediaRoot.GetComponent<RectTransform>(), listItemButton))
            {
                PrefabUtility.SaveAsPrefabAsset(contents, LessonPanelPrefabPath);
                Debug.Log($"[ASRS][Module1][Build] Added Pause/Replay video controls to {LessonPanelPrefabPath}.");
            }
            else
            {
                Debug.Log("[ASRS][Module1][Build] LessonPanel already has video controls — nothing changed.");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    // Shared with Lesson_Panel_Builder so a rebuilt Lesson Panel keeps them.
    // Returns false if the media box already has controls.
    public static bool AddVideoControls(Station_Type_Card card, RectTransform mediaBox, GameObject listItemButton)
    {
        if (card == null || mediaBox == null || listItemButton == null || mediaBox.Find("VideoControls") != null)
            return false;

        UI ui = new UI(null, null, listItemButton);
        RectTransform row = ui.MakeRect("VideoControls", mediaBox);
        row.anchorMin = row.anchorMax = row.pivot = new Vector2(0.5f, 0f);
        row.sizeDelta = new Vector2(40f, 6f);
        row.anchoredPosition = new Vector2(0f, 2.5f);

        (Button pause, TMP_Text pauseLabel) = ui.MakeButton(row, "PauseButton", "Pause", new Vector2(18f, 5f), new Vector2(-10f, 3f), 2.6f);
        (Button replay, _) = ui.MakeButton(row, "ReplayButton", "Replay", new Vector2(18f, 5f), new Vector2(10f, 3f), 2.6f);

        SetRef(card, "videoControls", row.gameObject);
        SetRef(card, "pauseButton", pause);
        SetRef(card, "pauseLabel", pauseLabel);
        SetRef(card, "replayButton", replay);
        row.gameObject.SetActive(false);
        return true;
    }

    // ---------------------------------------------------------------- steps

    private static int InsertMissingSteps(SequenceManager sequenceManager)
    {
        SerializedObject so = new SerializedObject(sequenceManager);
        SerializedProperty steps = so.FindProperty("steps");
        int added = 0;

        // 1. Welcome / what an ASRS is — first.
        added += InsertGroup(steps, 0, new[]
        {
            new StepDef(WelcomeId, "Welcome: What Is an ASRS?",
                "Welcome to Module 1. An Automated Storage and Retrieval System (ASRS) stores parts in numbered, addressable cells and uses a robot to put them away and bring them back on demand — so the manufacturing cell always knows exactly where every part is.",
                "In this module you'll meet the three storage station types, follow a pallet around the SmartCIM cell, inspect the rack's storage cells, load a template into the ASRS and finish with a three-question check. Press Next to begin.",
                true),
        });

        // 2. Storage roles, material flow and the pallet diagram — after the
        //    last station-type step (the Rack), before the hardware/part ID.
        int afterStations = LastStationTypeIndex(steps);
        List<StepDef> flow = new List<StepDef>
        {
            new StepDef(StorageRolesId, "Raw Material, WIP and Finished Goods",
                "The ASRS plays three storage roles in the cell. Raw material: blank parts wait in templates until a job needs them. Work-in-process (WIP): partly finished parts are stored between operations when the next station is busy.",
                "Finished goods: completed products come back to the ASRS and wait there until they're needed. One rack, three kinds of stock — each tracked by its cell address.",
                true),
        };
        for (int i = 0; i < FlowStages.Length; i++)
        {
            string info = (i == 0 ? "Let's follow one part through the SmartCIM cell. This is a concept walkthrough — the diagram beside you illustrates it; nothing in the real rack moves.\n\n" : "")
                          + FlowStages[i].description;
            flow.Add(new StepDef(FlowPrefix + (i + 1), $"Material Flow {i + 1} of {FlowStages.Length}: {FlowStages[i].title}", info, "", true));
        }
        added += InsertGroup(steps, afterStations >= 0 ? afterStations + 1 : Mathf.Min(1, steps.arraySize), flow);

        // 3. Cell inspection + material identification — right before the
        //    trainee loads the template (door already opened by then).
        int beforeLoad = IndexOfAction(steps, LoadSlottedTableId);
        if (beforeLoad < 0) beforeLoad = IndexOfAction(steps, LoadAsrsId);
        if (beforeLoad < 0) beforeLoad = steps.arraySize;
        added += InsertGroup(steps, beforeLoad, new[]
        {
            new StepDef(InspectCellsId, "Inspect the Storage Cells",
                InspectInstructions + " The cell panel shows its address, whether it's occupied, the template stored there and what material it holds. Inspect three different cells to continue.",
                "", false),
            new StepDef(IdentifyMaterialId, "Find the Material",
                "Now use the rack's inventory to find a part: select a cell that stores the material named on the cell panel. A wrong pick just tells you what that cell holds — nothing in the rack changes.",
                "", false),
        });

        // 4. Assessment — last, so it can't stand in for the loading tasks.
        added += InsertGroup(steps, steps.arraySize, new[]
        {
            new StepDef(QuizId, "Module 1 Check",
                "Answer the three questions on the quiz panel. You need at least two correct to pass, and you can retry as often as you like.",
                "", false),
        });

        so.ApplyModifiedProperties();
        return added;
    }

    // Inserts each missing step in order starting at 'index'. An existing
    // step with the same id is left alone and the rest follow it.
    private static int InsertGroup(SerializedProperty steps, int index, IEnumerable<StepDef> defs)
    {
        int added = 0;
        int cursor = Mathf.Clamp(index, 0, steps.arraySize);

        foreach (StepDef def in defs)
        {
            int existing = IndexOfAction(steps, def.Id);
            if (existing >= 0)
            {
                cursor = existing + 1;
                continue;
            }

            steps.InsertArrayElementAtIndex(cursor);
            WriteStep(steps.GetArrayElementAtIndex(cursor), def);
            Debug.Log($"[ASRS][Module1][Build] Inserted step {cursor + 1}: '{def.Title}' ({def.Id}).");
            cursor++;
            added++;
        }

        return added;
    }

    // InsertArrayElementAtIndex duplicates a neighbour, so every field is
    // written explicitly.
    private static void WriteStep(SerializedProperty step, StepDef def)
    {
        step.FindPropertyRelative("title").stringValue = def.Title;
        step.FindPropertyRelative("targets").arraySize = 0;
        step.FindPropertyRelative("infoText").stringValue = def.Info;
        step.FindPropertyRelative("instructionsText").stringValue = def.Instructions;
        step.FindPropertyRelative("RequiredOccupiedTable").objectReferenceValue = null;
        step.FindPropertyRelative("RequiredLoadedSlottedTable").objectReferenceValue = null;
        step.FindPropertyRelative("requiredActionId").stringValue = def.Id;
        step.FindPropertyRelative("passiveAction").boolValue = false;
        step.FindPropertyRelative("hintTarget").objectReferenceValue = null;
        step.FindPropertyRelative("advanceOnNext").boolValue = def.AdvanceOnNext;
        step.FindPropertyRelative("stationType").objectReferenceValue = null;
        step.FindPropertyRelative("narration").objectReferenceValue = null;
        SerializedProperty media = step.FindPropertyRelative("media");
        if (media != null)
            media.objectReferenceValue = null;
    }

    public static int IndexOfAction(SerializedProperty steps, string actionId)
    {
        for (int i = 0; i < steps.arraySize; i++)
            if (steps.GetArrayElementAtIndex(i).FindPropertyRelative("requiredActionId").stringValue == actionId)
                return i;
        return -1;
    }

    private static int LastStationTypeIndex(SerializedProperty steps)
    {
        int last = -1;
        for (int i = 0; i < steps.arraySize; i++)
            if (steps.GetArrayElementAtIndex(i).FindPropertyRelative("stationType").objectReferenceValue != null)
                last = i;
        return last;
    }

    private static void ConfigureSequence(SequenceManager sequenceManager)
    {
        SerializedObject so = new SerializedObject(sequenceManager);

        SerializedProperty logTag = so.FindProperty("logTag");
        if (logTag != null && (string.IsNullOrEmpty(logTag.stringValue) || logTag.stringValue == "[ASRS][Sequence]"))
            logTag.stringValue = "[ASRS][Module1]";

        // Only replace the stock text — a custom message is kept.
        SerializedProperty completion = so.FindProperty("completionText");
        if (completion != null && completion.stringValue == OldCompletionText)
            completion.stringValue = NewCompletionText;

        // The old replay-the-steps quiz would wait forever on Module 1's
        // labelled info steps; Module 1 uses ASRS_Module1_Quiz instead.
        SerializedProperty oldQuiz = so.FindProperty("quizManager");
        if (oldQuiz != null && oldQuiz.objectReferenceValue != null)
            Debug.LogWarning("[ASRS][Module1][Build] SequenceManager still has a Quiz_Manager assigned — Module 1 uses ASRS_Module1_Quiz; clear that field to avoid two quizzes.", sequenceManager);

        so.ApplyModifiedProperties();
    }

    // ------------------------------------------------------------ scene setup

    // Same choice ASRS_Module1_Rack_Hookup makes: the active rack, preferring
    // the ASRS Variant 1 copy when an older one is still in the scene.
    public static Item_ASRS FindLessonRack()
    {
        List<Item_ASRS> active = Object.FindObjectsByType<Item_ASRS>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(r => r.gameObject.activeInHierarchy).ToList();
        return active.FirstOrDefault(r => r.name.Contains("Variant")) ?? active.FirstOrDefault();
    }

    private static Transform FindPanelReference(SequenceManager sequenceManager)
    {
        Lesson_Panel lessonPanel = Object.FindFirstObjectByType<Lesson_Panel>();
        if (lessonPanel != null)
            return lessonPanel.transform;

        InstructionDisplay display = GetRef<InstructionDisplay>(sequenceManager, "infoPanel");
        return display != null ? display.transform : sequenceManager.transform;
    }

    private static GameObject GetOrCreateRoot(SequenceManager sequenceManager)
    {
        GameObject existing = sequenceManager.gameObject.scene.GetRootGameObjects().FirstOrDefault(g => g.name == FeaturesName);
        if (existing != null)
            return existing;

        GameObject root = new GameObject(FeaturesName);
        SceneManager_MoveToScene(root, sequenceManager);
        Undo.RegisterCreatedObjectUndo(root, "Build Module 1");
        Debug.Log($"[ASRS][Module1][Build] Created '{FeaturesName}'.", root);
        return root;
    }

    private static void SceneManager_MoveToScene(GameObject go, Component inScene)
    {
        if (go.scene != inScene.gameObject.scene)
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, inScene.gameObject.scene);
    }

    private static T GetOrBuild<T>(GameObject features, string name, System.Func<GameObject, T> build) where T : Component
    {
        T existing = features.GetComponentInChildren<T>(true);
        if (existing != null)
        {
            Debug.Log($"[ASRS][Module1][Build] '{existing.name}' already exists — re-wiring only.", existing);
            return existing;
        }

        GameObject host = new GameObject(name);
        host.transform.SetParent(features.transform, false);
        Undo.RegisterCreatedObjectUndo(host, "Build Module 1");
        T built = build(host);
        Debug.Log($"[ASRS][Module1][Build] Built '{name}'.", host);
        return built;
    }

    // New panels go beside the Lesson Panel (canvas forward faces away from
    // the reader, so +right is the reader's right). Panels that were already
    // in the scene keep wherever they've been moved to.
    private static void PlaceNewPanels(Transform reference, ASRS_Pallet_Circulation_Diagram diagram, ASRS_Rack_Cell_Inspector inspector, ASRS_Module1_Quiz quiz)
    {
        if (reference == null)
            return;

        PlaceIfNew(GetRef<GameObject>(diagram, "root"), reference, 1.45f);
        PlaceIfNew(GetRef<GameObject>(quiz, "root"), reference, 1.45f); // never on screen with the diagram
        PlaceIfNew(GetRef<GameObject>(inspector, "root"), reference, -1.25f); // moved next to the rack at runtime
    }

    private static void PlaceIfNew(GameObject panel, Transform reference, float rightOffset)
    {
        if (panel == null || panel.transform.position != Vector3.zero)
            return;

        panel.transform.SetPositionAndRotation(reference.position + reference.right * rightOffset, reference.rotation);
    }

    private static void FillMaterialNotes(ASRS_Rack_Cell_Inspector inspector, Item_ASRS rack)
    {
        // Every material the rack can actually load, by Item_Parent.Name.
        List<string> names = new List<string>();
        foreach (GameObject prefab in new[] { rack.EpoxyBlockPrefab }.Concat(rack.MaterialList ?? new List<GameObject>()))
        {
            if (prefab == null) continue;
            Item_Parent part = prefab.GetComponent<Item_Parent>();
            string name = part != null && !string.IsNullOrWhiteSpace(part.Name) ? part.Name : prefab.name;
            if (!names.Contains(name)) names.Add(name);
        }

        SerializedObject so = new SerializedObject(inspector);
        SerializedProperty notes = so.FindProperty("materialNotes");
        HashSet<string> present = new HashSet<string>();
        for (int i = 0; i < notes.arraySize; i++)
            present.Add(notes.GetArrayElementAtIndex(i).FindPropertyRelative("materialName").stringValue);

        foreach (string name in names.Where(n => !present.Contains(n)))
        {
            int i = notes.arraySize;
            notes.InsertArrayElementAtIndex(i);
            SerializedProperty note = notes.GetArrayElementAtIndex(i);
            note.FindPropertyRelative("materialName").stringValue = name;
            note.FindPropertyRelative("description").stringValue = KnownMaterials.FirstOrDefault(k => k.name == name).description ?? "";
            Debug.Log($"[ASRS][Module1][Build] Added material note for '{name}'.", inspector);
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------------------------------------------------------------- panels

    private static ASRS_Pallet_Circulation_Diagram BuildDiagram(GameObject host, UI ui)
    {
        RectTransform canvas = ui.MakeCanvas("DiagramPanel", host.transform, new Vector2(120f, 84f));
        ui.MakeTitle(canvas, "Pallet Circulation  <size=60%><color=#9FB3CC>(concept)</color></size>", 37f);
        TMP_Text note = ui.MakeText("Note", canvas, "Illustration only — the real rack, robot and conveyor don't move.", 2.6f, MutedColor, TextAlignmentOptions.Center);
        Place(note.rectTransform, new Vector2(0f, 31.5f), new Vector2(110f, 4f));

        RectTransform diagramArea = ui.MakeRect("Diagram", canvas);
        Stretch(diagramArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Path segments + direction arrows first, so boxes draw over the
        // line ends.
        for (int i = 0; i < DiagramNodes.Length; i++)
        {
            Vector2 from = DiagramNodes[i].position;
            Vector2 to = DiagramNodes[(i + 1) % DiagramNodes.Length].position;
            ui.MakeLine($"Path_{i + 1}", diagramArea, from, to, 0.7f, new Color(Cyan.r, Cyan.g, Cyan.b, 0.55f));
        }

        Image[] nodes = new Image[DiagramNodes.Length];
        for (int i = 0; i < DiagramNodes.Length; i++)
        {
            Image node = ui.MakeImage($"Node_{i + 1}", diagramArea, ui.Background, null, NodeColor);
            Place(node.rectTransform, DiagramNodes[i].position, new Vector2(28f, 8f));
            AddRoundedCorners(node.gameObject, new Color(Cyan.r, Cyan.g, Cyan.b, 0.6f), 0.35f, 2f);
            TMP_Text label = ui.MakeText("Label", node.rectTransform, DiagramNodes[i].label, 2.9f, Color.white, TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Bold;
            Stretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            nodes[i] = node;
        }

        Image pallet = ui.MakeImage("Pallet", diagramArea, ui.Background, null, PalletColor);
        Place(pallet.rectTransform, DiagramNodes[0].position + new Vector2(0f, 6.5f), new Vector2(7f, 4.5f));
        AddRoundedCorners(pallet.gameObject, new Color(1f, 1f, 1f, 0.8f), 0.3f, 1f);
        TMP_Text palletLabel = ui.MakeText("Label", pallet.rectTransform, "pallet", 1.8f, new Color(0.15f, 0.08f, 0f, 1f), TextAlignmentOptions.Center);
        Stretch(palletLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TMP_Text counter = ui.MakeText("StageCounter", canvas, "Stage 1 of 6", 2.8f, Cyan, TextAlignmentOptions.MidlineLeft);
        Place(counter.rectTransform, new Vector2(0f, -13f), new Vector2(108f, 4f));
        TMP_Text stageTitle = ui.MakeText("StageTitle", canvas, "", 4f, Color.white, TextAlignmentOptions.MidlineLeft);
        stageTitle.fontStyle = FontStyles.Bold;
        Place(stageTitle.rectTransform, new Vector2(0f, -17.5f), new Vector2(108f, 5f));
        TMP_Text description = ui.MakeText("StageDescription", canvas, "", 3.2f, BodyColor, TextAlignmentOptions.TopLeft);
        description.enableAutoSizing = true;
        description.fontSizeMin = 2.4f;
        description.fontSizeMax = 3.2f;
        Place(description.rectTransform, new Vector2(0f, -26f), new Vector2(108f, 11f));

        RectTransform controls = ui.MakeRect("Controls", canvas);
        Place(controls, new Vector2(0f, -37f), new Vector2(110f, 7f));
        (Button previous, _) = ui.MakeButton(controls, "PreviousButton", "‹  Back", new Vector2(18f, 6f), new Vector2(-40f, 0f), 3f);
        (Button playPause, TMP_Text playPauseLabel) = ui.MakeButton(controls, "PlayPauseButton", "Pause", new Vector2(24f, 6f), new Vector2(-12f, 0f), 3f);
        (Button replay, _) = ui.MakeButton(controls, "ReplayButton", "Replay", new Vector2(20f, 6f), new Vector2(13f, 0f), 3f);
        (Button next, _) = ui.MakeButton(controls, "NextButton", "Next  ›", new Vector2(18f, 6f), new Vector2(40f, 0f), 3f);

        ASRS_Pallet_Circulation_Diagram diagram = host.AddComponent<ASRS_Pallet_Circulation_Diagram>();
        SetRef(diagram, "root", canvas.gameObject);
        SetRef(diagram, "pallet", pallet.rectTransform);
        SetRef(diagram, "stageCounterText", counter);
        SetRef(diagram, "stageTitleText", stageTitle);
        SetRef(diagram, "stageDescriptionText", description);
        SetRef(diagram, "controlsRoot", controls.gameObject);
        SetRef(diagram, "playPauseButton", playPause);
        SetRef(diagram, "playPauseLabel", playPauseLabel);
        SetRef(diagram, "replayButton", replay);
        SetRef(diagram, "previousButton", previous);
        SetRef(diagram, "nextButton", next);

        SerializedObject so = new SerializedObject(diagram);
        SerializedProperty stages = so.FindProperty("stages");
        stages.arraySize = FlowStages.Length;
        for (int i = 0; i < FlowStages.Length; i++)
        {
            SerializedProperty stage = stages.GetArrayElementAtIndex(i);
            stage.FindPropertyRelative("title").stringValue = FlowStages[i].title;
            stage.FindPropertyRelative("description").stringValue = FlowStages[i].description;
            stage.FindPropertyRelative("node").objectReferenceValue = nodes[FlowStages[i].node];
            stage.FindPropertyRelative("sceneHighlights").arraySize = 0;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        return diagram;
    }

    private static ASRS_Rack_Cell_Inspector BuildCellInspector(GameObject host, UI ui)
    {
        RectTransform canvas = ui.MakeCanvas("CellInfoPanel", host.transform, new Vector2(66f, 38f));
        TMP_Text title = ui.MakeText("Title", canvas, "Storage Cell", 4.2f, Color.white, TextAlignmentOptions.MidlineLeft);
        title.fontStyle = FontStyles.Bold;
        Place(title.rectTransform, new Vector2(-6f, 15f), new Vector2(50f, 5f));

        (Button close, _) = ui.MakeButton(canvas, "CloseButton", "Close", new Vector2(13f, 5f), new Vector2(24f, 15f), 2.6f);

        TMP_Text prompt = ui.MakeText("Prompt", canvas, "", 2.7f, Cyan, TextAlignmentOptions.TopLeft);
        Place(prompt.rectTransform, new Vector2(0f, 9f), new Vector2(60f, 6f));

        TMP_Text cell = ui.MakeText("CellText", canvas, "No cell selected.", 3f, BodyColor, TextAlignmentOptions.TopLeft);
        cell.enableAutoSizing = true;
        cell.fontSizeMin = 2.2f;
        cell.fontSizeMax = 3f;
        Place(cell.rectTransform, new Vector2(0f, -3f), new Vector2(60f, 17f));

        TMP_Text feedback = ui.MakeText("Feedback", canvas, "", 2.7f, Color.white, TextAlignmentOptions.TopLeft);
        feedback.enableAutoSizing = true;
        feedback.fontSizeMin = 2f;
        feedback.fontSizeMax = 2.7f;
        Place(feedback.rectTransform, new Vector2(0f, -15f), new Vector2(60f, 6f));

        ASRS_Rack_Cell_Inspector inspector = host.AddComponent<ASRS_Rack_Cell_Inspector>();
        SetRef(inspector, "root", canvas.gameObject);
        SetRef(inspector, "panel", canvas);
        SetRef(inspector, "promptText", prompt);
        SetRef(inspector, "cellText", cell);
        SetRef(inspector, "feedbackText", feedback);
        SetRef(inspector, "closeButton", close);
        return inspector;
    }

    private static ASRS_Module1_Quiz BuildQuiz(GameObject host, UI ui)
    {
        RectTransform canvas = ui.MakeCanvas("QuizPanel", host.transform, new Vector2(120f, 84f));
        ui.MakeTitle(canvas, "Module 1 Check", 37f);

        TMP_Text counter = ui.MakeText("Counter", canvas, "Question 1 of 3", 2.8f, Cyan, TextAlignmentOptions.MidlineRight);
        Place(counter.rectTransform, new Vector2(30f, 37f), new Vector2(50f, 5f));

        TMP_Text question = ui.MakeText("Question", canvas, "", 3.8f, Color.white, TextAlignmentOptions.TopLeft);
        question.enableAutoSizing = true;
        question.fontSizeMin = 2.8f;
        question.fontSizeMax = 3.8f;
        Place(question.rectTransform, new Vector2(0f, 20f), new Vector2(108f, 18f));

        Button[] options = new Button[4];
        TMP_Text[] labels = new TMP_Text[4];
        for (int i = 0; i < options.Length; i++)
        {
            (options[i], labels[i]) = ui.MakeButton(canvas, $"Option_{(char)('A' + i)}", "", new Vector2(108f, 6.5f), new Vector2(0f, 6f - i * 7.5f), 3f);
            labels[i].alignment = TextAlignmentOptions.MidlineLeft;
            labels[i].margin = new Vector4(2f, 0f, 2f, 0f);
            labels[i].enableAutoSizing = true;
            labels[i].fontSizeMin = 2.2f;
            labels[i].fontSizeMax = 3f;
        }

        TMP_Text feedback = ui.MakeText("Feedback", canvas, "", 2.9f, BodyColor, TextAlignmentOptions.TopLeft);
        feedback.enableAutoSizing = true;
        feedback.fontSizeMin = 2.2f;
        feedback.fontSizeMax = 2.9f;
        Place(feedback.rectTransform, new Vector2(0f, -28f), new Vector2(108f, 9f));

        (Button retry, _) = ui.MakeButton(canvas, "RetryButton", "Retry", new Vector2(22f, 6.5f), new Vector2(-20f, -37.5f), 3.2f);
        (Button next, TMP_Text nextLabel) = ui.MakeButton(canvas, "NextButton", "Next question  ›", new Vector2(32f, 6.5f), new Vector2(38f, -37.5f), 3.2f);
        (Button cont, _) = ui.MakeButton(canvas, "ContinueButton", "Continue  ›", new Vector2(28f, 6.5f), new Vector2(40f, -37.5f), 3.2f);

        ASRS_Module1_Quiz quiz = host.AddComponent<ASRS_Module1_Quiz>();
        SetRef(quiz, "root", canvas.gameObject);
        SetRef(quiz, "counterText", counter);
        SetRef(quiz, "questionText", question);
        SetRef(quiz, "feedbackText", feedback);
        SetRef(quiz, "nextButton", next);
        SetRef(quiz, "nextLabel", nextLabel);
        SetRef(quiz, "retryButton", retry);
        SetRef(quiz, "continueButton", cont);

        SerializedObject so = new SerializedObject(quiz);
        SerializedProperty buttons = so.FindProperty("optionButtons");
        SerializedProperty labelProps = so.FindProperty("optionLabels");
        buttons.arraySize = options.Length;
        labelProps.arraySize = labels.Length;
        for (int i = 0; i < options.Length; i++)
        {
            buttons.GetArrayElementAtIndex(i).objectReferenceValue = options[i];
            labelProps.GetArrayElementAtIndex(i).objectReferenceValue = labels[i];
        }

        SerializedProperty questions = so.FindProperty("questions");
        questions.arraySize = 3;
        WriteQuestion(questions.GetArrayElementAtIndex(0),
            "Which storage station rotates its storage positions to bring the requested part to one fixed load/unload point, instead of a robot travelling to the part?",
            QuizStationOptions, 0,
            QuizStationExplanation,
            false, "");
        WriteQuestion(questions.GetArrayElementAtIndex(1),
            "After the CNC and assembly stations finish processing a part, where does it go?",
            new[]
            {
                "Back onto the conveyor and into the ASRS, stored as a finished good",
                "It stays on the CNC machine until the next job starts",
                "Into the carousel, where it becomes raw material again",
                "The ASRS robot carries it straight from the CNC machine",
            }, 0,
            "Processed parts return on the conveyor and the completed product is stored back in the ASRS — the ASRS holds raw material, work-in-process and finished goods.",
            false, "");
        WriteQuestion(questions.GetArrayElementAtIndex(2),
            "A job needs a {material}. Using the rack's inventory, select a storage cell that holds one.",
            new string[0], 0,
            "Every cell has an address and the rack's inventory records what each one holds — that's how the ASRS finds a part without searching.",
            true, "020001");

        so.ApplyModifiedPropertiesWithoutUndo();
        return quiz;
    }

    private static void WriteQuestion(SerializedProperty q, string prompt, string[] options, int correct, string explanation, bool cellQuestion, string cellTableId)
    {
        q.FindPropertyRelative("prompt").stringValue = prompt;
        SerializedProperty opts = q.FindPropertyRelative("options");
        opts.arraySize = options.Length;
        for (int i = 0; i < options.Length; i++)
            opts.GetArrayElementAtIndex(i).stringValue = options[i];
        q.FindPropertyRelative("correctOption").intValue = correct;
        q.FindPropertyRelative("explanation").stringValue = explanation;
        q.FindPropertyRelative("rackCellQuestion").boolValue = cellQuestion;
        q.FindPropertyRelative("cellTableId").stringValue = cellTableId;
    }

    // ------------------------------------------------------------ UI helpers

    // Builds widget-styled UI the same way Lesson_Panel_Builder does
    // (UIBackground.mat + UIGradient, rounded cyan borders, ListItemButton).
    private sealed class UI
    {
        public readonly Material Background;
        private readonly Sprite gradient;
        private readonly GameObject listItemButton;

        public UI(Material background, Sprite gradient, GameObject listItemButton)
        {
            Background = background;
            this.gradient = gradient;
            this.listItemButton = listItemButton;
        }

        public RectTransform MakeCanvas(string name, Transform parent, Vector2 size)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster), typeof(TrackedDeviceGraphicRaycaster));
            root.layer = LayerMask.NameToLayer("UI");
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.localScale = Vector3.one * PanelScale;
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            root.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;

            Image background = MakeImage("Background", rect, Background, gradient, new Color(1f, 1f, 1f, 0.8627451f));
            Stretch(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AddRoundedCorners(background.gameObject, new Color(Cyan.r, Cyan.g, Cyan.b, 0.39215687f), 0.5f, 4f);
            return rect;
        }

        public TMP_Text MakeTitle(RectTransform canvas, string text, float y)
        {
            TMP_Text title = MakeText("Title", canvas, text, 5.5f, Color.white, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            Place(title.rectTransform, new Vector2(0f, y), new Vector2(canvas.sizeDelta.x - 12f, 6f));
            return title;
        }

        public RectTransform MakeRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        public Image MakeImage(string name, Transform parent, Material material, Sprite sprite, Color color)
        {
            Image image = MakeRect(name, parent).gameObject.AddComponent<Image>();
            image.material = material;
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public TextMeshProUGUI MakeText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions alignment)
        {
            TextMeshProUGUI tmp = MakeRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        // A straight segment with a ">" at its midpoint pointing along it.
        public void MakeLine(string name, RectTransform parent, Vector2 from, Vector2 to, float thickness, Color color)
        {
            Vector2 delta = to - from;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            Image line = MakeImage(name, parent, null, null, color);
            RectTransform rect = line.rectTransform;
            Place(rect, (from + to) * 0.5f, new Vector2(delta.magnitude, thickness));
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);

            TMP_Text arrow = MakeText("Arrow", parent, ">", 6f, Cyan, TextAlignmentOptions.Center);
            arrow.fontStyle = FontStyles.Bold;
            Place(arrow.rectTransform, (from + to) * 0.5f, new Vector2(6f, 6f));
            arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        // The widget ListItemButton plus an Interactable_Select and collider
        // on the Default layer — clickable by the UI ray and by trigger
        // interact, exactly like the Lesson Panel's Next button.
        public (Button button, TMP_Text label) MakeButton(Transform parent, string name, string label, Vector2 size, Vector2 position, float fontSize)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(listItemButton, parent);
            go.name = name;
            go.layer = 0;
            RectTransform rect = go.GetComponent<RectTransform>();
            Place(rect, position, size);

            TextMeshProUGUI text = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
            {
                text.text = label;
                text.fontSize = fontSize;
                text.alignment = TextAlignmentOptions.Center;
            }

            Image background = go.GetComponent<Image>();
            if (background != null)
                background.color = new Color(1f, 1f, 1f, 0.12f);

            SetColor(go.GetComponent<Image_Rounded_Corners>(), "BorderColor", new Color(Cyan.r, Cyan.g, Cyan.b, 0.6f));
            SetColor(go.GetComponent<Widget_Button>(), "HighlightedBorderColor", Cyan);
            SetFloat(go.GetComponent<Widget_Button>(), "HighlightedBackgroundOpacity", 0.3f);

            BoxCollider collider = go.AddComponent<BoxCollider>();
            collider.size = new Vector3(size.x, size.y, 0.5f);
            collider.center = Vector3.zero;
            go.AddComponent<Interactable_Select>();

            return (go.GetComponent<Button>(), text);
        }
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void AddRoundedCorners(GameObject go, Color borderColor, float borderWidth, float cornerRadius)
    {
        Image_Rounded_Corners corners = go.AddComponent<Image_Rounded_Corners>();
        SetColor(corners, "BorderColor", borderColor);
        SetFloat(corners, "BorderWidth", borderWidth);
        SetFloat(corners, "CornerRadius", cornerRadius);
    }

    private static T GetRef<T>(Component component, string property) where T : Object
    {
        SerializedProperty prop = new SerializedObject(component).FindProperty(property);
        return prop != null ? prop.objectReferenceValue as T : null;
    }

    private static void SetRef(Component component, string property, Object value)
    {
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(property);
        if (prop == null)
        {
            Debug.LogError($"[ASRS][Module1][Build] {component.GetType().Name} has no field '{property}'.", component);
            return;
        }

        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetRefIfEmpty(Component component, string property, Object value)
    {
        if (value != null && GetRef<Object>(component, property) == null)
            SetRef(component, property, value);
    }

    private static void SetColor(Component component, string property, Color value)
    {
        if (component == null) return;
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(property);
        if (prop == null) return;
        prop.colorValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Component component, string property, float value)
    {
        if (component == null) return;
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(property);
        if (prop == null) return;
        prop.floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
