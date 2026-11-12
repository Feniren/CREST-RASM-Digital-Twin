using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// Adds Module 1's station-type intro (Carousel, ASRS-36, ASRS-36u, Rack) in
// front of the existing walkthrough in whichever scene is open:
//   1. Creates one ASRS_Station_Type asset per station under
//      Training/StationTypes — only if missing, so Inspector edits survive
//      a re-run.
//   2. Inserts 6 advanceOnNext steps at the START of SequenceManager.steps
//      (overview, the four stations, a 36-vs-72 recap). The existing steps
//      are kept untouched after them. Skipped if station steps already exist.
//   3. Builds one shared "StationTypeCard" (optional image + preview anchor)
//      on the InfoPanel's canvas, and a 2D narration AudioSource on the
//      SequenceManager, and wires both in.
//   4. Turns on TMP auto-size on the info text (max = its current size), so
//      longer pages shrink to fit instead of spilling off the panel.
//
// Station text for the Carousel, ASRS-36 and Rack is general description —
// ASRS_Station_Tasks_2026.docx only names those three; check the wording
// against the OpenMES/Open-CIM guides and edit the assets as needed.
public static class ASRS_Module1_Station_Types_Builder
{
    private const string AssetFolder = "Assets/Members/Alan/Training/StationTypes";
    private const string CardName = "StationTypeCard";
    private const string PreviewAnchorName = "StationPreviewAnchor";
    private const int IntroStepCount = 6;

    [MenuItem("ASRS/Build Module 1 Station Types Intro")]
    public static void Build()
    {
        SequenceManager sequenceManager = Object.FindFirstObjectByType<SequenceManager>();
        if (sequenceManager == null)
        {
            Debug.LogError("ASRS_Module1_Station_Types_Builder: no SequenceManager found in the open scene.");
            return;
        }

        ASRS_Station_Type carousel = GetOrCreateStation("StationType_Carousel", "Carousel",
            "A rotating storage station. Storage positions are arranged around a carousel that turns to bring the requested position to one fixed load/unload point, so the part comes to the robot instead of the robot travelling to the part.",
            "Compact and mechanically simple: one rotary motion moves all of the stock. In a CIM cell it serves as a small automated buffer of parts or templates next to the robot that loads and unloads it.");

        ASRS_Station_Type asrs36 = GetOrCreateStation("StationType_ASRS36", "ASRS-36",
            "An Automated Storage and Retrieval System built around a 6 × 6 rack: 6 levels × 6 cells = 36 addressable storage cells. A Cartesian robot moves along its X, Y and Z axes to reach any cell and pick or place a template.",
            "It is the CIM cell's raw material source, its warehouse for work-in-process parts, and its finished goods store. Every cell has an address, so the cell manager can request exactly the part it needs.");

        ASRS_Station_Type asrs36u = GetOrCreateStation("StationType_ASRS36u", "ASRS-36u",
            "The SmartCIM 4.0 model. Like the ASRS-36 it stores 6 levels × 6 cells = 36 locations per unit, but its Cartesian robot has an additional rotary axis, and it runs from a USB controller through the Scorbase software interface.",
            "Templates hold the parts and RFID tags verify each template's identity. Plexiglass door sensors halt the robot automatically if a door is opened. Larger SmartCIM installations combine units into a 72-bay ASRS.");

        ASRS_Station_Type rack = GetOrCreateStation("StationType_Rack", "Rack",
            "Passive storage: a fixed set of shelves or cells with no robot of its own. Parts are placed and removed by an operator or by the robot of a neighbouring station.",
            "Simple and inexpensive, but nothing moves the stock automatically — whoever loads it must put the right part in the right cell. Its cells are still tracked as storage locations, just like the ASRS cells.");

        int inserted = InsertIntroSteps(sequenceManager, carousel, asrs36, asrs36u, rack);
        Station_Type_Card card = BuildCard(sequenceManager);
        if (card != null)
            EnsureVideoSurface(card);
        AudioSource narrationSource = GetOrAddNarrationSource(sequenceManager);

        SetRef(sequenceManager, "stationCard", card);
        SetRef(sequenceManager, "narrationSource", narrationSource);
        EnableInfoTextAutoSize(sequenceManager);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = sequenceManager.gameObject;

        if (inserted == 0)
            Debug.Log("ASRS_Module1_Station_Types_Builder: station steps already present — steps left as they are; card, narration source and text auto-size re-checked. Save the scene.");
        else
            Debug.Log($"ASRS_Module1_Station_Types_Builder: inserted {inserted} intro steps ahead of the existing walkthrough. Save the scene.");
    }

    private static ASRS_Station_Type GetOrCreateStation(string fileName, string displayName, string summary, string details)
    {
        string path = $"{AssetFolder}/{fileName}.asset";
        ASRS_Station_Type existing = AssetDatabase.LoadAssetAtPath<ASRS_Station_Type>(path);
        if (existing != null)
            return existing;

        if (!AssetDatabase.IsValidFolder(AssetFolder))
            AssetDatabase.CreateFolder("Assets/Members/Alan/Training", "StationTypes");

        ASRS_Station_Type station = ScriptableObject.CreateInstance<ASRS_Station_Type>();
        station.displayName = displayName;
        station.summary = summary;
        station.details = details;

        AssetDatabase.CreateAsset(station, path);
        AssetDatabase.SaveAssets();
        return station;
    }

    private static int InsertIntroSteps(SequenceManager sequenceManager, params ASRS_Station_Type[] stations)
    {
        SerializedObject so = new SerializedObject(sequenceManager);
        SerializedProperty steps = so.FindProperty("steps");

        for (int i = 0; i < steps.arraySize; i++)
            if (steps.GetArrayElementAtIndex(i).FindPropertyRelative("stationType").objectReferenceValue != null)
                return 0;

        // InsertArrayElementAtIndex copies its neighbour's values, so every
        // field of the new steps is reset explicitly in SetIntroStep.
        for (int i = 0; i < IntroStepCount; i++)
            steps.InsertArrayElementAtIndex(0);

        SetIntroStep(steps.GetArrayElementAtIndex(0), null,
            "Before working with the station in front of you, let's look at where its parts are stored. In the SmartCIM 4.0 cell, storage stations supply raw material, hold work-in-process parts and keep finished goods.",
            "There are four storage station types: the Carousel, the ASRS-36, the ASRS-36u and the Rack. Press Next to review each one — you'll need to view all four before continuing.");

        for (int i = 0; i < stations.Length; i++)
            SetIntroStep(steps.GetArrayElementAtIndex(1 + i), stations[i], "", "");

        SetIntroStep(steps.GetArrayElementAtIndex(5), null,
            "The physical SmartCIM ASRS-36u stores 36 templates per unit (6 levels × 6 cells). This Unity scene models the larger 72-slot arrangement: two 36-slot racks, one on each side of the rotating arm.",
            "Next, you'll identify the parts of the ASRS station in front of you.");

        so.ApplyModifiedPropertiesWithoutUndo();
        return IntroStepCount;
    }

    private static void SetIntroStep(SerializedProperty step, ASRS_Station_Type station, string infoText, string instructionsText)
    {
        step.FindPropertyRelative("targets").arraySize = 0;
        step.FindPropertyRelative("infoText").stringValue = infoText;
        step.FindPropertyRelative("instructionsText").stringValue = instructionsText;
        step.FindPropertyRelative("RequiredOccupiedTable").objectReferenceValue = null;
        step.FindPropertyRelative("requiredActionId").stringValue = "";
        step.FindPropertyRelative("passiveAction").boolValue = false;
        step.FindPropertyRelative("hintTarget").objectReferenceValue = null;
        step.FindPropertyRelative("advanceOnNext").boolValue = true;
        step.FindPropertyRelative("stationType").objectReferenceValue = station;
        step.FindPropertyRelative("narration").objectReferenceValue = null;
    }

    // One shared card on the InfoPanel's world-space canvas: an Image to the
    // right of the panel (hidden until a station has a sprite), plus a
    // preview anchor for an optional prefab. Reused if already built.
    private static Station_Type_Card BuildCard(SequenceManager sequenceManager)
    {
        // Keep whatever card is already assigned (e.g. the Lesson Panel's) —
        // otherwise a re-run could re-point SequenceManager at an old,
        // deactivated card.
        Station_Type_Card existing = GetRef<Station_Type_Card>(sequenceManager, "stationCard");
        if (existing == null)
            existing = Object.FindFirstObjectByType<Station_Type_Card>(FindObjectsInactive.Include);
        if (existing != null)
            return existing;

        InstructionDisplay infoPanel = GetRef<InstructionDisplay>(sequenceManager, "infoPanel");
        Canvas canvas = infoPanel != null ? infoPanel.GetComponentInChildren<Canvas>(true) : null;
        if (canvas == null)
        {
            Debug.LogWarning("ASRS_Module1_Station_Types_Builder: SequenceManager's Info Panel has no Canvas — card not built. Station text still works without it.");
            return null;
        }

        GameObject cardGO = new GameObject(CardName, typeof(RectTransform));
        RectTransform cardRect = cardGO.GetComponent<RectTransform>();
        cardRect.SetParent(canvas.transform, false);
        cardRect.anchoredPosition = new Vector2(100f, 0f);
        cardRect.sizeDelta = new Vector2(60f, 60f);

        GameObject imageGO = new GameObject("StationImage", typeof(RectTransform), typeof(Image));
        RectTransform imageRect = imageGO.GetComponent<RectTransform>();
        imageRect.SetParent(cardRect, false);
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.sizeDelta = Vector2.zero;
        imageGO.GetComponent<Image>().raycastTarget = false;
        imageGO.SetActive(false);

        // Outside the canvas, so a spawned prefab isn't scaled down by the
        // canvas's 0.01 world scale.
        GameObject anchorGO = new GameObject(PreviewAnchorName);
        anchorGO.transform.SetParent(infoPanel.transform, false);
        anchorGO.transform.localPosition = new Vector3(1.2f, 0f, 0f);

        Station_Type_Card card = cardGO.AddComponent<Station_Type_Card>();
        SetRef(card, "image", imageGO.GetComponent<Image>());
        SetRef(card, "previewAnchor", anchorGO.transform);
        return card;
    }

    // A RawImage + VideoPlayer filling the card, alongside StationImage —
    // added to an already-built card too, so re-running the builder upgrades
    // scenes built before video support existed.
    private static void EnsureVideoSurface(Station_Type_Card card)
    {
        if (GetRef<RawImage>(card, "videoSurface") != null)
            return;

        GameObject videoGO = new GameObject("StationVideo", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter), typeof(VideoPlayer));
        RectTransform videoRect = videoGO.GetComponent<RectTransform>();
        videoRect.SetParent(card.transform, false);
        videoRect.anchorMin = Vector2.zero;
        videoRect.anchorMax = Vector2.one;
        videoRect.sizeDelta = Vector2.zero;

        videoGO.GetComponent<RawImage>().raycastTarget = false;

        AspectRatioFitter fitter = videoGO.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 16f / 9f;

        VideoPlayer player = videoGO.GetComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.isLooping = true;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.audioOutputMode = VideoAudioOutputMode.None;

        videoGO.SetActive(false);

        SetRef(card, "videoSurface", videoGO.GetComponent<RawImage>());
        SetRef(card, "videoPlayer", player);
        SetRef(card, "videoAspect", fitter);
    }

    private static AudioSource GetOrAddNarrationSource(SequenceManager sequenceManager)
    {
        AudioSource source = sequenceManager.GetComponent<AudioSource>();
        if (source != null)
            return source;

        source = sequenceManager.gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }

    private static void EnableInfoTextAutoSize(SequenceManager sequenceManager)
    {
        InstructionDisplay infoPanel = GetRef<InstructionDisplay>(sequenceManager, "infoPanel");
        TextMeshProUGUI text = infoPanel != null ? GetRef<TextMeshProUGUI>(infoPanel, "infoText") : null;
        if (text == null || text.enableAutoSizing)
            return;

        float size = text.fontSize;
        text.fontSizeMax = size;
        text.fontSizeMin = Mathf.Max(1f, size * 0.6f);
        text.enableAutoSizing = true;
        EditorUtility.SetDirty(text);
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
            Debug.LogError($"ASRS_Module1_Station_Types_Builder: property '{property}' not found on {component.GetType().Name}");
            return;
        }

        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
