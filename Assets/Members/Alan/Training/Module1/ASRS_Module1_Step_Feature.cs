using System.Collections;
using UnityEngine;

// Base for a Module 1 feature that only exists on its own lesson steps —
// the pallet circulation diagram, the rack cell inspector, the quiz. It
// listens to SequenceManager.StepActivated and matches the new step's
// Required Action Id against its own ids: on a match it shows its Root and
// gets OnFeatureEnter; on any other step it gets OnFeatureExit and hides
// again, so nothing (animation, highlight, panel) is left running after the
// lesson moves on.
//
// Keep this component on an always-active GameObject and put the visuals
// under Root — a deactivated host would miss the step events.
public abstract class ASRS_Module1_Step_Feature : MonoBehaviour
{
    [SerializeField] protected SequenceManager sequenceManager;

    [Tooltip("Shown only while one of this feature's steps is current.")]
    [SerializeField] protected GameObject root;

    [Tooltip("Once the step's condition is met, keep the result on screen this long before the lesson moves on, so the trainee can read it.")]
    [SerializeField, Min(0f)] protected float completionHoldSeconds = 2f;

    protected bool IsShowing { get; private set; }
    protected string ActiveActionId { get; private set; }

    private int activeStepIndex = -1;

    protected abstract string LogTag { get; }

    // True if this feature owns steps with this Required Action Id.
    public abstract bool HandlesAction(string actionId);

    protected abstract void OnFeatureEnter(string actionId);
    protected abstract void OnFeatureExit(string actionId);

    protected virtual void Awake()
    {
        if (sequenceManager == null)
            sequenceManager = FindFirstObjectByType<SequenceManager>();

        if (root != null)
            root.SetActive(false);

        if (sequenceManager == null)
        {
            ASRS_Module1_Log.Warn(LogTag, $"'{name}' found no SequenceManager — it will never be shown.", this);
            return;
        }

        sequenceManager.StepActivated += OnStepActivated;
    }

    // Covers a feature that wakes up after SequenceManager already started
    // its current step.
    protected virtual void Start()
    {
        if (sequenceManager != null && sequenceManager.CurrentStep != null && !IsShowing)
            OnStepActivated(sequenceManager.CurrentStepIndex, sequenceManager.CurrentStep);
    }

    protected virtual void OnDestroy()
    {
        if (sequenceManager != null)
            sequenceManager.StepActivated -= OnStepActivated;
    }

    private void OnStepActivated(int index, SequenceManager.Step step)
    {
        string id = step != null ? step.requiredActionId : null;
        bool mine = !string.IsNullOrEmpty(id) && HandlesAction(id);

        if (mine && IsShowing && index == activeStepIndex)
            return; // already entered for this exact step

        if (IsShowing)
        {
            string previous = ActiveActionId;
            IsShowing = false;
            ActiveActionId = null;
            activeStepIndex = -1;
            OnFeatureExit(previous);

            if (!mine && root != null)
                root.SetActive(false);
        }

        if (!mine)
            return;

        IsShowing = true;
        ActiveActionId = id;
        activeStepIndex = index;

        if (root != null)
            root.SetActive(true);

        OnFeatureEnter(id);
    }

    // Reports this feature's step as done. SequenceManager ignores it unless
    // that step is the current one, so a stray call can't skip ahead.
    protected void CompleteStep(string actionId)
    {
        if (sequenceManager == null)
            return;

        ASRS_Module1_Log.Info(LogTag, $"Reporting '{actionId}' to SequenceManager.", this);
        sequenceManager.NotifyAction(actionId);
    }

    // Same, after Completion Hold Seconds — only if this step is still the
    // one showing by then (the condition was already met; this is reading
    // time, not a substitute for it).
    protected void CompleteStepAfterHold(string actionId)
    {
        if (completionHoldSeconds <= 0f)
        {
            CompleteStep(actionId);
            return;
        }

        StartCoroutine(CompleteAfterHoldRoutine(actionId));
    }

    private IEnumerator CompleteAfterHoldRoutine(string actionId)
    {
        yield return new WaitForSecondsRealtime(completionHoldSeconds);

        if (IsShowing && ActiveActionId == actionId)
            CompleteStep(actionId);
    }
}
