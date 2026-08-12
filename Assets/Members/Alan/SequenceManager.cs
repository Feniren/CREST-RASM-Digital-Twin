using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
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
    }

    [Header("Steps in order")]
    [SerializeField] private Step[] steps;

    [Header("Shared references")]
    [SerializeField] private InstructionDisplay infoPanel;
    [SerializeField] private GameObject nextButton; // shown while paging through a step's text, hidden once its interaction begins
    [SerializeField] private string completionText = "Great job — you've identified every part. Let's test what you've learned.";

    [Header("Events")]
    public UnityEvent onSequenceComplete; // hook the quiz start here in the Inspector

    private int currentIndex = -1;
    private readonly List<Marker_Interactable> pendingMarkers = new List<Marker_Interactable>();
    private bool waitingForTable;
    private bool shownInstructions;
    private bool interactionActive;

    private void Start()
    {
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

    // For steps with no Marker_Interactable targets — call this once a
    // software/UI action (e.g. a control-panel button) succeeds, passing the
    // same string as that step's requiredActionId. No-ops if the current step
    // isn't waiting on that action (or isn't waiting on an action at all).
    public void NotifyAction(string actionId)
    {
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
