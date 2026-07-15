using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Events;

// Drives a step-by-step "click the highlighted part" sequence.
// Put this on a single Managers object in the scene, then fill in
// the Steps array in the Inspector, in the order you want them taught.
public class SequenceManager : MonoBehaviour
{
    [System.Serializable]
    public class Step
    {
        [Tooltip("The part's XR Simple Interactable (the clickable object)")]
        public UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable target;

        [Tooltip("The green highlight object for this part (e.g. the HighlightProxy cube)")]
        public GameObject highlightVisual;

        [Tooltip("Text shown in the info panel while this step is active")]
        [TextArea(2, 4)]
        public string infoText;
    }

    [Header("Steps in order")]
    [SerializeField] private Step[] steps;

    [Header("Shared references")]
    [SerializeField] private InstructionDisplay infoPanel;
    [SerializeField] private string completionText = "Great job — you've identified every part. Let's test what you've learned.";

    [Header("Events")]
    public UnityEvent onSequenceComplete; // hook the quiz start here in the Inspector

    private int currentIndex = 0;
    private UnityAction<SelectEnterEventArgs>[] listeners;

    private void Start()
    {
        // Turn off every highlight to start clean
        foreach (var step in steps)
        {
            if (step.highlightVisual != null)
                step.highlightVisual.SetActive(false);
        }

        // Subscribe each step's target with a listener that remembers its own index
        listeners = new UnityAction<SelectEnterEventArgs>[steps.Length];
        for (int i = 0; i < steps.Length; i++)
        {
            int capturedIndex = i; // avoid closure bug — capture per-iteration copy
            listeners[i] = (args) => OnPartClicked(capturedIndex);
            steps[i].target.selectEntered.AddListener(listeners[i]);
        }

        ActivateStep(0);
    }

    private void OnDestroy()
    {
        if (listeners == null) return;
        for (int i = 0; i < steps.Length; i++)
        {
            if (steps[i].target != null)
                steps[i].target.selectEntered.RemoveListener(listeners[i]);
        }
    }

    private void OnPartClicked(int index)
    {
        // Ignore clicks on parts that aren't the current active step
        if (index != currentIndex) return;

        if (steps[currentIndex].highlightVisual != null)
            steps[currentIndex].highlightVisual.SetActive(false);

        ActivateStep(currentIndex + 1);
    }

    private void ActivateStep(int index)
    {
        currentIndex = index;

        if (index >= steps.Length)
        {
            if (infoPanel != null) infoPanel.UpdateText(completionText);
            onSequenceComplete?.Invoke();
            return;
        }

        if (steps[index].highlightVisual != null)
            steps[index].highlightVisual.SetActive(true);

        if (infoPanel != null)
            infoPanel.UpdateText(steps[index].infoText);
    }
}