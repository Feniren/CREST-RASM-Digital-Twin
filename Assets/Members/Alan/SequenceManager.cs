using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
// Drives a step-by-step "click the part" sequence.
// Only the current step's part(s) are active/visible in the scene —
// every other part is hidden until it's its turn.
// Put this on a single Managers object, then fill in the Steps array
// in the Inspector, in the order you want them taught.
public class SequenceManager : MonoBehaviour
{
    [System.Serializable]
    public class Step
    {
        [Tooltip("Optional heading shown on panels with a title bar (Lesson Panel). Station-type steps use the station's name instead. Leave blank to use the panel's default title.")]
        public string title;

        [Tooltip("The part GameObjects for this step. Each must have a Marker_Interactable on it. All become visible and clickable as soon as this step begins (alongside infoText) — the step advances once every one of them has been clicked.")]
        public GameObject[] targets;

        [Tooltip("Shown as soon as this step begins, at the same time its targets become visible.")]
        [TextArea(2, 4)]
        public string infoText;

        [Tooltip("Optional: a follow-up hint revealed when Next is pressed (targets are already visible/clickable by then). Leave blank if infoText alone is enough — Next then has nothing to do on this step.")]
        [TextArea(2, 4)]
        public string instructionsText;

        [Tooltip("Optional: this step also waits until this table has an item placed on it (Item != null) — e.g. the player manually places a held part via the table's own AlternateInteract.")]
        public Item_Plate RequiredOccupiedTable;

        [Tooltip("Optional: like Required Occupied Table, but for a slotted table — the step also waits until this table is holding an item (Item != null), e.g. the trainee loading an epoxy block onto it.")]
        public Item_Slotted_Table RequiredLoadedSlottedTable;

        [Tooltip("Optional: for steps with no Marker_Interactable targets (e.g. a software/UI action like a control-panel button) — leave targets empty and set this instead. The step completes when NotifyAction() is called with this exact string.")]
        public string requiredActionId;

        [Tooltip("Check this if Required Action Id is driven by something automatic/environmental (e.g. a conveyor table reaching the RFID scanner on its own schedule) rather than a deliberate trainee click (e.g. a panel button). During the quiz, this action firing before its turn is silently ignored instead of counted as a mistake, since its timing isn't something the trainee controls.")]
        public bool passiveAction;

        [Tooltip("Optional: the part to fly up and showcase in front of the trainee's face when the Hint button is pressed on this step. Leave blank if this step has no visual hint.")]
        public Transform hintTarget;

        [Tooltip("Check this for a read-only info step (no targets, table, or action) — Next advances to the following step once any instructionsText has been shown. Quiz mode skips these steps automatically. Required Action Id may still be set on such a step as a label for step-scoped features (e.g. Module 1's M1_Flow_ steps show the circulation diagram); Next still advances it.")]
        public bool advanceOnNext;

        [Tooltip("Optional: teach one station type on this step. Its displayName/summary/details replace infoText/instructionsText, and its image/prefab show on the Station Type Card. Normally used together with Advance On Next.")]
        public ASRS_Station_Type stationType;

        [Tooltip("Optional: narration played when this step begins. Falls back to the Station Type's own narration if left empty.")]
        public AudioClip narration;

        [Tooltip("Optional: show this asset's picture/video in the media box on this step — display only. Unlike Station Type, it doesn't replace the step's text or count as a station type, and it shows even on steps with marker targets (e.g. a picture of the template while the trainee finds the real one).")]
        public ASRS_Station_Type media;

        [Tooltip("Show this step's text in the panel's larger body size (set on InstructionDisplay).")]
        public bool largeText;
    }

    [Header("Steps in order")]
    [SerializeField] private Step[] steps;

    [Header("Shared references")]
    [SerializeField] private InstructionDisplay infoPanel;
    [SerializeField] private GameObject nextButton; // shown while paging through a step's text, hidden once its interaction begins
    [SerializeField] private Hint_PartShowcase hintShowcase; // optional — wire the Hint button's OnClick() to OnHintPressed()
    [SerializeField] private GameObject hintButton; // optional — hidden once the sequence completes, since quiz mode has no per-step hints to show
    [SerializeField] private string completionText = "Great job — you've identified every part. Let's test what you've learned.";
    [SerializeField] private Station_Type_Card stationCard; // optional — shows the current step's station type image/prefab
    [SerializeField] private AudioSource narrationSource; // optional — plays each step's narration clip, if it has one

    [Header("Quiz (optional)")]
    [Tooltip("Optional — NotifyAction() calls are forwarded to it while it's active, and its own start-prompt panel is shown once the walkthrough completes. Leave empty if this module has no quiz yet.")]
    [SerializeField] private Quiz_Manager quizManager;

    // Read-only access so Quiz_Manager can replay the exact same step data
    // (targets, requiredActionId) instead of quiz content being authored
    // twice.
    public Step[] Steps => steps;

    [Header("Events")]
    public UnityEvent onSequenceComplete; // hook the quiz start here in the Inspector

    [Header("Debugging")]
    [Tooltip("Prefix for this sequence's Console messages, e.g. [ASRS][Module1].")]
    [SerializeField] private string logTag = "[ASRS][Sequence]";

    // Raised whenever a step becomes current — index == Steps.Length and a
    // null step once the walkthrough completes. Lets step-scoped features
    // (diagram, cell inspector, quiz) show themselves on their own steps
    // and clean up on every other one, without this class knowing them.
    public event System.Action<int, Step> StepActivated;

    public int CurrentStepIndex => currentIndex;
    public Step CurrentStep => steps != null && currentIndex >= 0 && currentIndex < steps.Length ? steps[currentIndex] : null;

    private int currentIndex = -1;
    private readonly List<Marker_Interactable> pendingMarkers = new List<Marker_Interactable>();
    private bool waitingForTable;
    private bool shownInstructions;
    private bool interactionActive;

    // The NextButton can be wired through both its Button.onClick and its
    // Interactable_Select, so one physical press may arrive twice. Harmless
    // when Next only reveals text, but on advanceOnNext steps it would skip
    // a whole step — so presses this close together count as one.
    private const float NextPressDebounce = 0.3f;
    private float lastNextPressTime = float.NegativeInfinity;

    // Set just before reloading the scene from OnStartQuizPressed() — static
    // so it survives the reload (statics aren't reset by loading a new
    // scene, only by a domain reload/exiting Play mode). Read once in the
    // fresh instance's Start() so the trainee lands straight in the quiz
    // instead of replaying the whole walkthrough after a "Start Quiz" reset.
    private static bool skipToQuizOnLoad;

    private void Start()
    {
        if (skipToQuizOnLoad)
        {
            skipToQuizOnLoad = false;
            StartQuizDirectly();
            return;
        }

        // Hide every part to start clean
        foreach (Step step in steps)
            foreach (GameObject target in step.targets)
                if (target != null)
                    target.SetActive(false);

        ActivateStep(0);
    }

    private void Update()
    {
        if (!waitingForTable)
            return;

        if (!AreTablesLoaded(steps[currentIndex]))
            return;

        waitingForTable = false;
        TryCompleteStep();
    }

    private void OnDestroy()
    {
        foreach (Marker_Interactable marker in pendingMarkers)
            if (marker != null)
                marker.Selected -= OnMarkerSelected;
    }

    private void OnMarkerSelected(Marker_Interactable marker, InteractionType interactionType)
    {
        if (!pendingMarkers.Contains(marker))
            return; // not one of the current step's targets

        pendingMarkers.Remove(marker);
        marker.Selected -= OnMarkerSelected;

        TryCompleteStep();
    }

    private void TryCompleteStep()
    {
        if (pendingMarkers.Count == 0 && !waitingForTable)
            ActivateStep(currentIndex + 1);
    }

    private void ActivateStep(int index)
    {
        currentIndex = index;
        shownInstructions = false;
        interactionActive = false;

        if (index >= steps.Length)
        {
            Debug.Log($"{logTag} Walkthrough complete ({steps.Length} steps).", this);
            ShowCompletion();

            // Only offer to start the quiz once there's actually one to
            // start — a module with no Quiz_Manager assigned just ends here.
            if (quizManager != null)
                quizManager.ShowStartPrompt();

            StepActivated?.Invoke(index, null);
            onSequenceComplete?.Invoke();
            return;
        }

        Step step = steps[index];
        Debug.Log($"{logTag} Step {index + 1}/{steps.Length} '{DescribeStep(step)}' started.", this);

        if (infoPanel != null)
        {
            infoPanel.UpdateTitle(step.stationType != null ? step.stationType.displayName : step.title);
            infoPanel.UpdateProgress(GetProgressText(index));
            infoPanel.SetLargeText(step.largeText);
            infoPanel.UpdateText(GetInfoText(index));
        }

        // Next is only needed to reveal instructionsText, if this step has
        // any — it no longer gates the targets themselves — or to leave an
        // advanceOnNext info step.
        if (nextButton != null)
            nextButton.SetActive(!string.IsNullOrEmpty(GetInstructionsText(index)) || step.advanceOnNext);

        // The media box stays up for the whole lesson except part
        // identification (steps with marker targets), where the trainee's
        // attention belongs on the real object instead.
        if (stationCard != null)
        {
            if (step.stationType != null)
                stationCard.Show(step.stationType);
            else if (step.media != null)
                stationCard.Show(step.media);
            else if (HasMarkerTargets(step))
                stationCard.Hide();
            else
                stationCard.ShowEmpty();
        }

        PlayNarration(step);

        // Targets go live immediately: the object infoText is describing
        // should already be visible (and clickable) while it's on screen,
        // not just once the trainee has clicked through to instructions.
        BeginStepInteraction(step);

        StepActivated?.Invoke(index, step);
    }

    private static string DescribeStep(Step step)
    {
        if (step.stationType != null) return "station: " + step.stationType.displayName;
        if (!string.IsNullOrEmpty(step.title)) return step.title;
        if (!string.IsNullOrEmpty(step.requiredActionId)) return step.requiredActionId;
        return "untitled";
    }

    // True if the current step is actively waiting on this exact action id —
    // lets other scripts gate one-off behavior on "are we on this specific
    // step right now" (e.g. a demo sequence that should only play out during
    // one particular step) without needing to know/guess its index.
    // True once the walkthrough has reached (or passed, or finished — quiz
    // mode included) the step with this action id — lets other scripts gate
    // behavior that shouldn't fire until the lesson is actually ready for it
    // (e.g. don't consume a physical prop for a step the trainee hasn't
    // reached yet). Unlike IsOnStep, this stays true for the rest of the
    // module once reached, rather than only while sitting on that exact step.
    public bool HasReachedStep(string actionId)
    {
        if (steps == null)
            return false;

        int targetIndex = System.Array.FindIndex(steps, s => s.requiredActionId == actionId);

        // Unknown action id — fail open rather than silently deadlocking
        // whatever's gating on it.
        return targetIndex < 0 || currentIndex >= targetIndex;
    }

    public bool IsOnStep(string actionId)
    {
        return interactionActive && currentIndex >= 0 && currentIndex < steps.Length
            && steps[currentIndex].requiredActionId == actionId;
    }

    // For steps with no Marker_Interactable targets — call this once a
    // software/UI action (e.g. a control-panel button) succeeds, passing the
    // same string as that step's requiredActionId. No-ops if the current step
    // isn't waiting on that action (or isn't waiting on an action at all).
    public void NotifyAction(string actionId)
    {
        if (quizManager != null && quizManager.IsActive)
        {
            quizManager.HandleAction(actionId);
            return;
        }

        if (!interactionActive || currentIndex < 0 || currentIndex >= steps.Length)
        {
            Debug.Log($"{logTag} Action '{actionId}' ignored — no step is waiting for an action right now.", this);
            return;
        }

        Step step = steps[currentIndex];

        if (!string.IsNullOrEmpty(step.requiredActionId) && step.requiredActionId == actionId)
        {
            Debug.Log($"{logTag} Action '{actionId}' completes step {currentIndex + 1}.", this);
            TryCompleteStep();
        }
        else
        {
            Debug.Log($"{logTag} Action '{actionId}' ignored — step {currentIndex + 1} waits for '{step.requiredActionId}'.", this);
        }
    }

    // Hook this to the instruction panel's Next button OnClick. Targets are
    // already live by the time this can be pressed (see ActivateStep) — this
    // reveals the optional instructionsText follow-up, then hides itself
    // since there's nothing further for Next to do on this step. On an
    // advanceOnNext step it stays visible, and the following press moves on.
    public void OnNextPressed()
    {
        if (currentIndex < 0 || currentIndex >= steps.Length)
            return;

        if (Time.unscaledTime - lastNextPressTime < NextPressDebounce)
            return;
        lastNextPressTime = Time.unscaledTime;

        Step step = steps[currentIndex];
        string instructions = GetInstructionsText(currentIndex);

        if (!shownInstructions && !string.IsNullOrEmpty(instructions))
        {
            shownInstructions = true;
            if (infoPanel != null) infoPanel.UpdateText(instructions);
            if (nextButton != null) nextButton.SetActive(step.advanceOnNext);
            return;
        }

        // Every page of this step has been seen — an info step is done.
        if (step.advanceOnNext && interactionActive && pendingMarkers.Count == 0 && !waitingForTable)
            ActivateStep(currentIndex + 1);
    }

    private void ShowCompletion()
    {
        if (infoPanel != null)
        {
            infoPanel.UpdateTitle("Module Complete");
            infoPanel.UpdateProgress("");
            infoPanel.SetLargeText(false);
            infoPanel.UpdateText(completionText);
        }

        if (nextButton != null) nextButton.SetActive(false);
        if (hintButton != null) hintButton.SetActive(false);
        if (stationCard != null) stationCard.ShowEmpty();
    }

    private static bool HasMarkerTargets(Step step)
    {
        if (step.targets == null)
            return false;

        foreach (GameObject target in step.targets)
            if (target != null)
                return true;

        return false;
    }

    // For a panel that brings its own media box (Lesson_Panel) — fills the
    // Station Card slot only if the scene left it empty, so an explicit
    // Inspector assignment always wins.
    public void UseStationCardIfUnset(Station_Type_Card card)
    {
        if (stationCard == null)
            stationCard = card;
    }

    // A station-type step takes its text from the ASRS_Station_Type asset;
    // any other step uses its own infoText/instructionsText as before. On a
    // panel without a title bar the station's name and "n of N" are written
    // into the body instead, so older panels still show them.
    private string GetInfoText(int index)
    {
        Step step = steps[index];
        return step.stationType != null ? StationHeader(index) + step.stationType.summary : step.infoText;
    }

    private string GetInstructionsText(int index)
    {
        Step step = steps[index];

        if (step.stationType == null)
            return step.instructionsText;

        return string.IsNullOrEmpty(step.stationType.details) ? "" : StationHeader(index) + step.stationType.details;
    }

    private string StationHeader(int index)
    {
        if (infoPanel != null && infoPanel.HasTitle)
            return "";

        GetStationPosition(index, out int position, out int total);
        return $"<b>{steps[index].stationType.displayName}</b>  <size=80%>(station type {position} of {total})</size>\n";
    }

    private string GetProgressText(int index)
    {
        string progress = $"Step {index + 1} of {steps.Length}";

        if (steps[index].stationType == null)
            return progress;

        GetStationPosition(index, out int position, out int total);
        return $"{progress}  ·  Station type {position} of {total}";
    }

    private void GetStationPosition(int index, out int position, out int total)
    {
        position = 0;
        total = 0;

        for (int i = 0; i < steps.Length; i++)
        {
            if (steps[i].stationType == null)
                continue;

            total++;
            if (i <= index)
                position = total;
        }
    }

    private void PlayNarration(Step step)
    {
        if (narrationSource == null)
            return;

        narrationSource.Stop();

        AudioClip clip = step.narration != null ? step.narration
            : step.stationType != null ? step.stationType.narration
            : null;

        if (clip != null)
            narrationSource.PlayOneShot(clip);
    }

    // Hook this to the Start Quiz button's OnClick() — only shown once the
    // walkthrough completes. Reloads the scene (so the rack/conveyor/arm and
    // every marker come back in their clean, freshly-authored state, not
    // whatever's left over from the walkthrough that just ran) and flags the
    // fresh instance to jump straight into the quiz instead of replaying the
    // guided walkthrough.
    public void OnStartQuizPressed()
    {
        skipToQuizOnLoad = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Runs once, right after a reload triggered by OnStartQuizPressed() —
    // skips straight to the completed/quiz state instead of Start()'s normal
    // ActivateStep(0).
    private void StartQuizDirectly()
    {
        currentIndex = steps.Length;
        interactionActive = false;

        ShowCompletion();

        if (quizManager != null)
            quizManager.BeginQuiz();
    }

    // Hook this to the Hint button's OnClick(). Shows the current step's
    // hintTarget (if any) flying up in front of the trainee's face.
    public void OnHintPressed()
    {
        if (currentIndex < 0 || currentIndex >= steps.Length || hintShowcase == null)
            return;

        Transform hintTarget = steps[currentIndex].hintTarget;
        if (hintTarget != null)
            hintShowcase.PlayHint(hintTarget);
    }

    private void BeginStepInteraction(Step step)
    {
        interactionActive = true;

        foreach (GameObject target in step.targets)
        {
            if (target == null) continue;

            target.SetActive(true);

            Marker_Interactable marker = target.GetComponent<Marker_Interactable>();
            if (marker == null)
            {
                Debug.LogError($"[SequenceManager] Step {currentIndex} target '{target.name}' has no Marker_Interactable attached.");
                continue;
            }

            marker.Selected += OnMarkerSelected;
            pendingMarkers.Add(marker);
        }

        waitingForTable = !AreTablesLoaded(step);
    }

    private static bool AreTablesLoaded(Step step)
    {
        return (step.RequiredOccupiedTable == null || step.RequiredOccupiedTable.Item != null)
            && (step.RequiredLoadedSlottedTable == null || step.RequiredLoadedSlottedTable.Item != null);
    }
}
