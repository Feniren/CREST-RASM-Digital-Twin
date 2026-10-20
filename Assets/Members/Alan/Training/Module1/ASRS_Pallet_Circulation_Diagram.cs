using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Conceptual, world-space diagram of one pallet's lap around the SmartCIM
// cell: ASRS storage -> robot retrieval -> conveyor/PLC routing ->
// processing stations -> return conveyor -> back into ASRS storage. Purely
// illustrative — it never touches Item_ASRS, the arm, or the real conveyor.
//
// Two kinds of step use it (matched on the step's Required Action Id):
//   Walkthrough steps "M1_Flow_1" … "M1_Flow_6" — info steps paged with the
//     Lesson Panel's Next; the diagram just shows that stage (the pallet
//     glides to it from the previous stage) with no controls.
//   Animation step "M1_PalletCirculation" — Play / Pause / Replay and ‹ ›
//     stepping; the step completes once every stage has been shown.
public class ASRS_Pallet_Circulation_Diagram : ASRS_Module1_Step_Feature
{
    [Serializable]
    public class Stage
    {
        public string title;
        [TextArea(2, 4)] public string description;
        [Tooltip("The diagram box this stage lights up. Stages may share a box (the last stage returns to ASRS Storage).")]
        public Image node;
        [Tooltip("Optional scene objects switched on only while this stage is current, e.g. an arrow or marker next to the real conveyor.")]
        public GameObject[] sceneHighlights;
    }

    [Header("Steps")]
    [SerializeField] private string walkthroughActionPrefix = "M1_Flow_";
    [SerializeField] private string animationActionId = "M1_PalletCirculation";

    [Header("Diagram")]
    [SerializeField] private Stage[] stages = new Stage[0];
    [SerializeField] private RectTransform pallet;
    [Tooltip("Where the pallet sits relative to the centre of the current stage's box.")]
    [SerializeField] private Vector2 palletOffset = new Vector2(0f, 6.5f);

    [Header("Text")]
    [SerializeField] private TMP_Text stageCounterText;
    [SerializeField] private TMP_Text stageTitleText;
    [SerializeField] private TMP_Text stageDescriptionText;

    [Header("Controls (animation step only)")]
    [SerializeField] private GameObject controlsRoot;
    [SerializeField] private Button playPauseButton;
    [SerializeField] private TMP_Text playPauseLabel;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    [Header("Timing")]
    [Tooltip("Pallet speed along the diagram, in canvas units per second (the panel is 120 units wide).")]
    [SerializeField, Min(1f)] private float travelSpeed = 25f;
    [Tooltip("Seconds the pallet waits at each stage during Play, so its explanation can be read.")]
    [SerializeField, Min(0f)] private float stageDwellSeconds = 3f;

    [Header("Lesson Panel embedding")]
    [Tooltip("Show the diagram inside the Lesson Panel's image/video box (instead of as its own floating panel) on its steps. The Lesson Panel's body text then carries the stage explanations.")]
    [SerializeField] private bool showInLessonMediaBox = true;
    [Tooltip("Optional — the Lesson Panel's Station_Type_Card. Found automatically when empty.")]
    [SerializeField] private Station_Type_Card lessonMediaCard;
    [Tooltip("Optional — the Lesson Panel's InstructionDisplay. Found automatically when empty.")]
    [SerializeField] private InstructionDisplay lessonDisplay;
    [Tooltip("Hidden while embedded (the Lesson Panel already shows titles and text). Defaults to children named Title, Note, StageCounter, StageTitle, StageDescription.")]
    [SerializeField] private GameObject[] hideWhenEmbedded = new GameObject[0];
    [Tooltip("Where the controls row moves while embedded, so it sits right under the diagram.")]
    [SerializeField] private Vector2 embeddedControlsPosition = new Vector2(0f, -16f);
    [SerializeField, Min(0f)] private float embeddedPadding = 2f;

    [Header("Colours")]
    [SerializeField] private Color nodeColor = new Color(0.12f, 0.2f, 0.3f, 0.95f);
    [SerializeField] private Color visitedNodeColor = new Color(0.16f, 0.36f, 0.46f, 0.95f);
    [SerializeField] private Color activeNodeColor = new Color(0.3764706f, 0.8509804f, 1f, 1f);

    private readonly ASRS_Module1_UI.Debouncer debouncer = new ASRS_Module1_UI.Debouncer();

    private bool[] visited = new bool[0];
    private int currentStage = -1;
    private bool travelling;
    private Vector2 travelTarget;
    private float dwellRemaining;
    private bool autoPlay;
    private bool paused;
    private bool allStagesSeen;
    private bool completionReported;

    protected override string LogTag => ASRS_Module1_Log.Flow;

    public int StageCount => stages != null ? stages.Length : 0;
    public string AnimationActionId => animationActionId;
    public string WalkthroughActionPrefix => walkthroughActionPrefix;
    public Stage[] Stages => stages;
    public RectTransform Pallet => pallet;

    protected override void Awake()
    {
        base.Awake();

        ASRS_Module1_UI.Bind(playPauseButton, debouncer, OnPlayPausePressed);
        ASRS_Module1_UI.Bind(replayButton, debouncer, Replay);
        ASRS_Module1_UI.Bind(previousButton, debouncer, () => StepManually(-1));
        ASRS_Module1_UI.Bind(nextButton, debouncer, () => StepManually(+1));

        if (lessonMediaCard == null || lessonDisplay == null)
        {
            Lesson_Panel lessonPanel = FindFirstObjectByType<Lesson_Panel>();
            if (lessonPanel != null)
            {
                if (lessonMediaCard == null) lessonMediaCard = lessonPanel.MediaCard;
                if (lessonDisplay == null) lessonDisplay = lessonPanel.Display;
            }
        }

        if ((hideWhenEmbedded == null || hideWhenEmbedded.Length == 0) && root != null)
        {
            List<GameObject> found = new List<GameObject>();
            foreach (string childName in new[] { "Title", "Note", "StageCounter", "StageTitle", "StageDescription" })
            {
                Transform child = root.transform.Find(childName);
                if (child != null) found.Add(child.gameObject);
            }
            hideWhenEmbedded = found.ToArray();
        }

        if (showInLessonMediaBox && (lessonMediaCard == null || lessonMediaCard.MediaRoot == null))
            ASRS_Module1_Log.Warn(LogTag, "No Lesson Panel media box found — the diagram will show as its own panel.", this);
    }

    public override bool HandlesAction(string actionId)
    {
        return actionId == animationActionId || TryGetWalkthroughStage(actionId, out _);
    }

    protected override void OnFeatureEnter(string actionId)
    {
        if (StageCount == 0)
        {
            ASRS_Module1_Log.Error(LogTag, "No stages configured — run 'ASRS/Module 1/Build Module 1 In Current Scene'.", this);
            return;
        }

        if (visited.Length != StageCount)
            visited = new bool[StageCount];

        Embed();

        if (actionId == animationActionId)
        {
            completionReported = false;
            allStagesSeen = false;
            if (controlsRoot != null) controlsRoot.SetActive(true);
            ASRS_Module1_Log.Info(LogTag, "Pallet circulation animation shown — playing from the start.", this);
            Replay();
            return;
        }

        // Walkthrough: show the matching stage, gliding over from the
        // previous one when the diagram was just showing it.
        TryGetWalkthroughStage(actionId, out int stage);
        if (controlsRoot != null) controlsRoot.SetActive(false);
        autoPlay = false;
        paused = false;

        bool continuing = currentStage >= 0 && currentStage == stage - 1;
        if (continuing)
            BeginTravel(stage);
        else
            SnapTo(stage);

        ASRS_Module1_Log.Info(LogTag, $"Walkthrough stage {stage + 1}/{StageCount}: {stages[stage].title}.", this);
    }

    protected override void OnFeatureExit(string actionId)
    {
        autoPlay = false;
        paused = false;
        travelling = false;
        SetSceneHighlights(-1);
        Unembed();

        // Leaving the diagram entirely (not just moving to the next flow
        // step) resets it, so a later visit starts clean.
        if (actionId == animationActionId || !IsNextStepAlsoMine())
        {
            currentStage = -1;
            Array.Clear(visited, 0, visited.Length);
        }
    }

    private bool IsNextStepAlsoMine()
    {
        SequenceManager.Step step = sequenceManager != null ? sequenceManager.CurrentStep : null;
        return step != null && !string.IsNullOrEmpty(step.requiredActionId) && HandlesAction(step.requiredActionId);
    }

    private void Update()
    {
        if (!IsShowing || paused || StageCount == 0 || currentStage < 0)
            return;

        if (travelling)
        {
            if (pallet != null)
            {
                pallet.anchoredPosition = Vector2.MoveTowards(pallet.anchoredPosition, travelTarget, travelSpeed * Time.deltaTime);
                if ((pallet.anchoredPosition - travelTarget).sqrMagnitude > 0.0001f)
                    return;
            }

            travelling = false;
            dwellRemaining = stageDwellSeconds;
            ArriveAt(currentStage);
            return;
        }

        // Once every stage has been seen (by Play or by stepping), the step
        // completes when the current stage's dwell runs out — the same moment
        // a full lap ends and "Play again" would appear — so the last stage's
        // text gets read before the lesson moves on.
        bool awaitingCompletion = allStagesSeen && !completionReported && ActiveActionId == animationActionId;
        if (!autoPlay && !awaitingCompletion)
            return;

        dwellRemaining -= Time.deltaTime;
        if (dwellRemaining > 0f)
            return;

        if (awaitingCompletion)
        {
            completionReported = true;
            autoPlay = false;
            RefreshPlayPauseLabel();
            ASRS_Module1_Log.Info(LogTag, "All six stages seen — continuing Module 1.", this);
            CompleteStep(animationActionId);
            return;
        }

        if (currentStage < StageCount - 1)
        {
            BeginTravel(currentStage + 1);
        }
        else
        {
            autoPlay = false;
            RefreshPlayPauseLabel();
            ASRS_Module1_Log.Info(LogTag, "Pallet completed a full lap.", this);
        }
    }

    public void OnPlayPausePressed()
    {
        if (!IsShowing)
            return;

        bool atEnd = currentStage >= StageCount - 1 && !travelling;

        if (autoPlay)
        {
            paused = !paused;
            ASRS_Module1_Log.Info(LogTag, paused ? "Paused." : "Resumed.", this);
        }
        else if (atEnd)
        {
            Replay();
            return;
        }
        else
        {
            autoPlay = true;
            paused = false;
            dwellRemaining = Mathf.Min(dwellRemaining, stageDwellSeconds);
            ASRS_Module1_Log.Info(LogTag, "Playing.", this);
        }

        RefreshPlayPauseLabel();
    }

    public void Replay()
    {
        if (!IsShowing || StageCount == 0)
            return;

        Array.Clear(visited, 0, visited.Length);
        autoPlay = true;
        paused = false;
        SnapTo(0);
        RefreshPlayPauseLabel();
    }

    // ‹ / › — stops auto-play and glides one stage back or forward, so the
    // trainee can read each stage at their own pace.
    public void StepManually(int direction)
    {
        if (!IsShowing || StageCount == 0)
            return;

        autoPlay = false;
        paused = false;

        int target = Mathf.Clamp(currentStage + direction, 0, StageCount - 1);
        if (target != currentStage)
            BeginTravel(target);

        RefreshPlayPauseLabel();
    }

    private void SnapTo(int stage)
    {
        currentStage = stage;
        travelling = false;
        dwellRemaining = stageDwellSeconds;

        if (pallet != null)
            pallet.anchoredPosition = PalletPositionFor(stage);

        ArriveAt(stage);
    }

    private void BeginTravel(int stage)
    {
        currentStage = stage;
        travelTarget = PalletPositionFor(stage);
        travelling = true;

        // Text and colours switch as the pallet sets off, so the trainee
        // reads where it's heading while it moves.
        ShowStage(stage);
    }

    private void ArriveAt(int stage)
    {
        visited[stage] = true;
        ShowStage(stage);

        if (ActiveActionId == animationActionId && !allStagesSeen && AllVisited())
        {
            allStagesSeen = true;
            ASRS_Module1_Log.Info(LogTag, $"All stages seen — the step completes after this stage's {stageDwellSeconds:0.#} s dwell.", this);
        }
    }

    private void ShowStage(int stage)
    {
        Stage s = stages[stage];
        ASRS_Module1_UI.SetText(stageCounterText, $"Stage {stage + 1} of {StageCount}");
        ASRS_Module1_UI.SetText(stageTitleText, s.title);
        ASRS_Module1_UI.SetText(stageDescriptionText, s.description);

        for (int i = 0; i < StageCount; i++)
        {
            Image node = stages[i].node;
            if (node == null) continue;
            node.color = visited[i] ? visitedNodeColor : nodeColor;
        }

        // Applied last: when two stages share a box, the current one wins.
        if (s.node != null)
            s.node.color = activeNodeColor;

        SetSceneHighlights(stage);

        // Embedded, the diagram's own text area is hidden — on the animation
        // step the Lesson Panel's body follows the pallet instead. (On the
        // walkthrough steps that body already holds the stage's text.)
        if (embedded && ActiveActionId == animationActionId && lessonDisplay != null)
            lessonDisplay.UpdateText("Follow one pallet around the cell (a concept — nothing real moves). Use Play, Pause, Replay, or ‹ › to step.\n\n" +
                                     $"<b>Stage {stage + 1} of {StageCount}: {s.title}</b>\n{s.description}");
    }

    // ------------------------------------------------------------ embedding

    private bool embedded;
    private Transform savedParent;
    private Vector3 savedLocalPosition, savedLocalScale;
    private Quaternion savedLocalRotation;
    private Vector2 savedAnchorMin, savedAnchorMax, savedPivot, savedSizeDelta;
    private Vector2 savedBackgroundAnchorMin, savedBackgroundAnchorMax, savedBackgroundOffsetMin, savedBackgroundOffsetMax;
    private Vector2 savedControlsPosition;
    private bool[] savedHiddenStates = new bool[0];

    // Moves the diagram into the Lesson Panel's media box, hides its own
    // titles/text, shrinks its background to the diagram + controls, and
    // scales it to fit the box.
    private void Embed()
    {
        if (embedded || !showInLessonMediaBox || root == null || lessonMediaCard == null)
            return;

        RectTransform box = lessonMediaCard.MediaRoot;
        RectTransform panel = root.GetComponent<RectTransform>();
        if (box == null || panel == null)
            return;

        lessonMediaCard.ShowBoxOnly();
        Canvas.ForceUpdateCanvases();

        savedParent = panel.parent;
        savedLocalPosition = panel.localPosition;
        savedLocalRotation = panel.localRotation;
        savedLocalScale = panel.localScale;
        savedAnchorMin = panel.anchorMin;
        savedAnchorMax = panel.anchorMax;
        savedPivot = panel.pivot;
        savedSizeDelta = panel.sizeDelta;

        savedHiddenStates = new bool[hideWhenEmbedded.Length];
        for (int i = 0; i < hideWhenEmbedded.Length; i++)
        {
            if (hideWhenEmbedded[i] == null) continue;
            savedHiddenStates[i] = hideWhenEmbedded[i].activeSelf;
            hideWhenEmbedded[i].SetActive(false);
        }

        RectTransform controls = controlsRoot != null ? controlsRoot.GetComponent<RectTransform>() : null;
        if (controls != null)
        {
            savedControlsPosition = controls.anchoredPosition;
            controls.anchoredPosition = embeddedControlsPosition;
        }

        panel.SetParent(box, false);
        panel.SetAsLastSibling();
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        panel.localRotation = Quaternion.identity;
        panel.localScale = Vector3.one;

        RectTransform background = root.transform.Find("Background") as RectTransform;
        Rect content = ContentBounds(panel, background);

        if (background != null)
        {
            savedBackgroundAnchorMin = background.anchorMin;
            savedBackgroundAnchorMax = background.anchorMax;
            savedBackgroundOffsetMin = background.offsetMin;
            savedBackgroundOffsetMax = background.offsetMax;

            background.anchorMin = background.anchorMax = new Vector2(0.5f, 0.5f);
            background.sizeDelta = content.size + Vector2.one * (embeddedPadding * 2f);
            background.anchoredPosition = content.center;
        }

        Vector2 fitted = content.size + Vector2.one * (embeddedPadding * 2f);
        Vector2 boxSize = box.rect.size;
        float scale = boxSize.x < 1f || boxSize.y < 1f
            ? 0.6f // box not laid out yet — a sensible size for the Lesson Panel's 80-wide media box
            : Mathf.Min(boxSize.x / Mathf.Max(fitted.x, 0.01f), boxSize.y / Mathf.Max(fitted.y, 0.01f));
        panel.localScale = Vector3.one * scale;
        panel.anchoredPosition3D = new Vector3(-content.center.x * scale, -content.center.y * scale, 0f);

        embedded = true;
    }

    private void Unembed()
    {
        if (!embedded)
            return;

        embedded = false;
        RectTransform panel = root.GetComponent<RectTransform>();

        panel.SetParent(savedParent, false);
        panel.anchorMin = savedAnchorMin;
        panel.anchorMax = savedAnchorMax;
        panel.pivot = savedPivot;
        panel.sizeDelta = savedSizeDelta;
        panel.localPosition = savedLocalPosition;
        panel.localRotation = savedLocalRotation;
        panel.localScale = savedLocalScale;

        RectTransform background = root.transform.Find("Background") as RectTransform;
        if (background != null)
        {
            background.anchorMin = savedBackgroundAnchorMin;
            background.anchorMax = savedBackgroundAnchorMax;
            background.offsetMin = savedBackgroundOffsetMin;
            background.offsetMax = savedBackgroundOffsetMax;
        }

        if (controlsRoot != null)
            controlsRoot.GetComponent<RectTransform>().anchoredPosition = savedControlsPosition;

        for (int i = 0; i < hideWhenEmbedded.Length && i < savedHiddenStates.Length; i++)
            if (hideWhenEmbedded[i] != null)
                hideWhenEmbedded[i].SetActive(savedHiddenStates[i]);
    }

    // Bounds, in the panel's own space, of everything still visible except
    // the background.
    private static Rect ContentBounds(RectTransform panel, RectTransform background)
    {
        Vector3[] corners = new Vector3[4];
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        // Only things that draw — stretched containers (the "Diagram" area,
        // the controls row) would otherwise make the bounds the whole panel.
        foreach (Graphic graphic in panel.GetComponentsInChildren<Graphic>(false))
        {
            RectTransform rect = graphic.rectTransform;
            if (rect == panel || rect == background)
                continue;

            rect.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector2 local = panel.InverseTransformPoint(corner);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }
        }

        return min.x > max.x ? new Rect(-60f, -42f, 120f, 84f) : Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private void SetSceneHighlights(int activeStage)
    {
        for (int i = 0; i < StageCount; i++)
        {
            GameObject[] highlights = stages[i].sceneHighlights;
            if (highlights == null) continue;

            foreach (GameObject highlight in highlights)
                if (highlight != null)
                    highlight.SetActive(i == activeStage);
        }
    }

    private Vector2 PalletPositionFor(int stage)
    {
        Image node = stages[stage].node;
        if (node == null || pallet == null)
            return pallet != null ? pallet.anchoredPosition : Vector2.zero;

        // Nodes and pallet share the same parent, so anchored positions are
        // directly comparable.
        return node.rectTransform.anchoredPosition + palletOffset;
    }

    private bool AllVisited()
    {
        foreach (bool v in visited)
            if (!v)
                return false;
        return visited.Length > 0;
    }

    private void RefreshPlayPauseLabel()
    {
        bool playing = autoPlay && !paused;
        ASRS_Module1_UI.SetText(playPauseLabel, playing ? "Pause" : currentStage >= StageCount - 1 && !travelling ? "Play again" : "Play");
    }

    private bool TryGetWalkthroughStage(string actionId, out int stage)
    {
        stage = -1;
        if (string.IsNullOrEmpty(actionId) || string.IsNullOrEmpty(walkthroughActionPrefix) || !actionId.StartsWith(walkthroughActionPrefix))
            return false;

        if (!int.TryParse(actionId.Substring(walkthroughActionPrefix.Length), out int oneBased))
            return false;

        stage = oneBased - 1;
        return stage >= 0 && stage < StageCount;
    }
}
