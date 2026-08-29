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

        [Tooltip("Optional: for steps with no Marker_Interactable targets (e.g. a software/UI action like a control-panel button) — leave targets empty and set this instead. The step completes when NotifyAction() is called with this exact string.")]
        public string requiredActionId;

        [Tooltip("Check this if Required Action Id is driven by something automatic/environmental (e.g. a conveyor table reaching the RFID scanner on its own schedule) rather than a deliberate trainee click (e.g. a panel button). During the quiz, this action firing before its turn is silently ignored instead of counted as a mistake, since its timing isn't something the trainee controls.")]
        public bool passiveAction;

        [Tooltip("Optional: the part to fly up and showcase in front of the trainee's face when the Hint button is pressed on this step. Leave blank if this step has no visual hint.")]
        public Transform hintTarget;
    }

    [Header("Steps in order")]
    [SerializeField] private Step[] steps;

    [Header("Shared references")]
    [SerializeField] private InstructionDisplay infoPanel;
    [SerializeField] private GameObject nextButton; // shown while paging through a step's text, hidden once its interaction begins
    [SerializeField] private Hint_PartShowcase hintShowcase; // optional — wire the Hint button's OnClick() to OnHintPressed()
    [SerializeField] private GameObject hintButton; // optional — hidden once the sequence completes, since quiz mode has no per-step hints to show
    [SerializeField] private string completionText = "Great job — you've identified every part. Let's test what you've learned.";

    [Header("Quiz (optional)")]
    [Tooltip("Optional — NotifyAction() calls are forwarded to it while it's active, and its own start-prompt panel is shown once the walkthrough completes. Leave empty if this module has no quiz yet.")]
    [SerializeField] private Quiz_Manager quizManager;

    // Read-only access so Quiz_Manager can replay the exact same step data
    // (targets, requiredActionId) instead of quiz content being authored
    // twice.
    public Step[] Steps => steps;

    [Header("Events")]
    public UnityEvent onSequenceComplete; // hook the quiz start here in the Inspector

    private int currentIndex = -1;
    private readonly List<Marker_Interactable> pendingMarkers = new List<Marker_Interactable>();
    private bool waitingForTable;
    private bool shownInstructions;
    private bool interactionActive;

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

        if (steps[currentIndex].RequiredOccupiedTable.Item == null)
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
            if (infoPanel != null) infoPanel.UpdateText(completionText);
            if (nextButton != null) nextButton.SetActive(false);
            if (hintButton != null) hintButton.SetActive(false);

            // Only offer to start the quiz once there's actually one to
            // start — a module with no Quiz_Manager assigned just ends here.
            if (quizManager != null)
                quizManager.ShowStartPrompt();

            onSequenceComplete?.Invoke();
            return;
        }

        Step step = steps[index];

        if (infoPanel != null)
            infoPanel.UpdateText(step.infoText);

        // Next is only needed to reveal instructionsText, if this step has
        // any — it no longer gates the targets themselves.
        if (nextButton != null)
            nextButton.SetActive(!string.IsNullOrEmpty(step.instructionsText));

        // Targets go live immediately: the object infoText is describing
        // should already be visible (and clickable) while it's on screen,
        // not just once the trainee has clicked through to instructions.
        BeginStepInteraction(step);
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
            return;

        Step step = steps[currentIndex];

        if (!string.IsNullOrEmpty(step.requiredActionId) && step.requiredActionId == actionId)
            TryCompleteStep();
    }

    // Hook this to the instruction panel's Next button OnClick. Targets are
    // already live by the time this can be pressed (see ActivateStep) — this
    // only reveals the optional instructionsText follow-up, then hides
    // itself since there's nothing further for Next to do on this step.
    public void OnNextPressed()
    {
        if (currentIndex < 0 || currentIndex >= steps.Length)
            return;

        Step step = steps[currentIndex];

        if (shownInstructions || string.IsNullOrEmpty(step.instructionsText))
            return;

        shownInstructions = true;
        if (infoPanel != null) infoPanel.UpdateText(step.instructionsText);
        if (nextButton != null) nextButton.SetActive(false);
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

        if (infoPanel != null) infoPanel.UpdateText(completionText);
        if (nextButton != null) nextButton.SetActive(false);
        if (hintButton != null) hintButton.SetActive(false);

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

        waitingForTable = step.RequiredOccupiedTable != null && step.RequiredOccupiedTable.Item == null;
    }
}
