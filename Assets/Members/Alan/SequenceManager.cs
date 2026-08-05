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
        [Tooltip("The part GameObjects for this step. Each must have a Marker_Interactable on it. All are shown together; the step advances once every one of them has been clicked.")]
        public GameObject[] targets;

        [Tooltip("Shown first, before the Next button is pressed.")]
        [TextArea(2, 4)]
        public string infoText;

        [Tooltip("Optional: shown after Next is pressed once. Leave blank to skip straight to this step's interaction on Next. Use this for instructions that follow an introduction in infoText.")]
        [TextArea(2, 4)]
        public string instructionsText;

        [Tooltip("Optional: this step also waits until this table has an item placed on it (Item != null) — e.g. the player manually places a held part via the table's own AlternateInteract.")]
        public Item_Plate RequiredOccupiedTable;
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

        if (infoPanel != null)
            infoPanel.UpdateText(steps[index].infoText);

        if (nextButton != null)
            nextButton.SetActive(true);
    }

    // Hook this to the instruction panel's Next button OnClick.
    public void OnNextPressed()
    {
        if (interactionActive || currentIndex < 0 || currentIndex >= steps.Length)
            return;

        Step step = steps[currentIndex];

        if (!shownInstructions && !string.IsNullOrEmpty(step.instructionsText))
        {
            shownInstructions = true;
            if (infoPanel != null) infoPanel.UpdateText(step.instructionsText);
            return;
        }

        BeginStepInteraction(step);
    }

    private void BeginStepInteraction(Step step)
    {
        interactionActive = true;

        if (nextButton != null)
            nextButton.SetActive(false);

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
