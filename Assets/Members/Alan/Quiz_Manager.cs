using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Interactive end-of-module quiz — NOT multiple choice. Replays the exact
// same steps SequenceManager already teaches (reused via its Steps getter,
// so quiz content is never authored twice), but instead of walking through
// them one at a time with retry prompts, every step's target(s) go live and
// clickable simultaneously. The trainee has to find and click/perform them
// in the right order; anything clicked/performed out of order doesn't block
// or skip — it just tallies as a mistake and stays available for its real
// turn.
//
// Wiring: assign this on SequenceManager's optional Quiz Manager field.
// SequenceManager calls BeginQuiz() itself the moment its own walkthrough
// finishes, and forwards NotifyAction() calls here (via HandleAction) while
// the quiz is active — no separate UnityEvent hookup needed for the handoff.
public class Quiz_Manager : MonoBehaviour
{
    [SerializeField] private SequenceManager sequenceManager;

    [Header("Status UI")]
    [SerializeField] private GameObject quizPanel;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Results UI")]
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private TextMeshProUGUI resultsText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button continueButton;

    [Tooltip("Maximum out-of-sequence clicks/actions allowed and still pass.")]
    [SerializeField] private int maxAllowedErrors = 3;

    [Header("Events")]
    [Tooltip("Invoked when Continue is pressed after a passing run — wire this to whatever advances the trainee to the next module.")]
    public UnityEvent onQuizPassed;

    public bool IsActive { get; private set; }

    private SequenceManager.Step[] steps;
    private int expectedIndex;
    private int errorCount;
    private readonly List<Marker_Interactable> subscribedMarkers = new List<Marker_Interactable>();
    private readonly HashSet<Marker_Interactable> pendingForCurrentStep = new HashSet<Marker_Interactable>();

    private void Awake()
    {
        if (retryButton != null) retryButton.onClick.AddListener(BeginQuiz);
        if (continueButton != null) continueButton.onClick.AddListener(OnContinuePressed);

        if (quizPanel != null) quizPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);

        // The whole panel (title/background included) stays hidden until
        // the quiz actually starts, not just its inner sub-panels.
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        UnsubscribeAll();
    }

    public void BeginQuiz()
    {
        if (sequenceManager == null)
        {
            Debug.LogError("[Quiz_Manager] No SequenceManager assigned.", this);
            return;
        }

        UnsubscribeAll();

        gameObject.SetActive(true);

        steps = sequenceManager.Steps;
        expectedIndex = 0;
        errorCount = 0;
        IsActive = true;

        if (resultsPanel != null) resultsPanel.SetActive(false);
        if (quizPanel != null) quizPanel.SetActive(true);

        // Every target from every step goes live and clickable at once — the
        // trainee has to find the right one instead of being shown it.
        foreach (SequenceManager.Step step in steps)
        {
            foreach (GameObject target in step.targets)
            {
                if (target == null) continue;

                target.SetActive(true);

                Marker_Interactable marker = target.GetComponent<Marker_Interactable>();
                if (marker == null) continue;

                marker.SetVisible(true);
                marker.Selected += OnMarkerSelected;
                subscribedMarkers.Add(marker);
            }
        }

        LoadExpectedStep();
        UpdateStatus();
    }

    // Called by SequenceManager.NotifyAction() while the quiz is active.
    public void HandleAction(string actionId)
    {
        if (!IsActive || expectedIndex >= steps.Length)
            return;

        SequenceManager.Step expectedStep = steps[expectedIndex];

        if (!string.IsNullOrEmpty(expectedStep.requiredActionId) && expectedStep.requiredActionId == actionId)
        {
            if (pendingForCurrentStep.Count == 0)
            {
                expectedIndex++;
                LoadExpectedStep();
            }
            // else: this step also has marker targets still pending — a
            // matching action alone doesn't complete it.
        }
        else if (IsKnownActionId(actionId))
        {
            errorCount++;
        }
        // else: not an action id used anywhere in this lesson — ignore.

        UpdateStatus();
    }

    private void OnMarkerSelected(Marker_Interactable marker, InteractionType interactionType)
    {
        if (!IsActive)
            return;

        if (pendingForCurrentStep.Contains(marker))
        {
            pendingForCurrentStep.Remove(marker);
            marker.Selected -= OnMarkerSelected;
            subscribedMarkers.Remove(marker);

            if (pendingForCurrentStep.Count == 0)
            {
                expectedIndex++;
                LoadExpectedStep();
            }
        }
        else
        {
            errorCount++;
            // Undo the marker's own click-driven auto-hide so it's still
            // visible/findable for its real turn later.
            marker.SetVisible(true);
        }

        UpdateStatus();
    }

    // Advances past any step with nothing to click/notify (a pure info step)
    // — quiz mode has no Next button to page through those with. Finishes
    // the quiz once every remaining step is like that.
    private void LoadExpectedStep()
    {
        pendingForCurrentStep.Clear();

        while (expectedIndex < steps.Length)
        {
            SequenceManager.Step step = steps[expectedIndex];
            bool hasMarkerTargets = false;

            foreach (GameObject target in step.targets)
            {
                if (target == null) continue;

                Marker_Interactable marker = target.GetComponent<Marker_Interactable>();
                if (marker == null) continue;

                pendingForCurrentStep.Add(marker);
                hasMarkerTargets = true;
            }

            if (hasMarkerTargets || !string.IsNullOrEmpty(step.requiredActionId))
                return;

            expectedIndex++;
        }

        FinishQuiz();
    }

    private bool IsKnownActionId(string actionId)
    {
        foreach (SequenceManager.Step step in steps)
            if (step.requiredActionId == actionId)
                return true;

        return false;
    }

    private void UpdateStatus()
    {
        if (statusText != null)
            statusText.text = $"Errors: {errorCount}";
    }

    private void FinishQuiz()
    {
        IsActive = false;
        UnsubscribeAll();

        if (quizPanel != null) quizPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(true);

        bool passed = errorCount <= maxAllowedErrors;

        if (resultsText != null)
        {
            string verdict = passed
                ? "<color=#66FF88>Passed — you can move on to the next module.</color>"
                : "<color=#FF6666>Not passed — review and try again.</color>";

            resultsText.text = $"Out-of-sequence clicks: {errorCount}\n\n{verdict}";
        }

        if (continueButton != null) continueButton.gameObject.SetActive(passed);
        if (retryButton != null) retryButton.gameObject.SetActive(!passed);
    }

    private void UnsubscribeAll()
    {
        foreach (Marker_Interactable marker in subscribedMarkers)
            if (marker != null)
                marker.Selected -= OnMarkerSelected;

        subscribedMarkers.Clear();
    }

    private void OnContinuePressed()
    {
        onQuizPassed?.Invoke();
    }
}
