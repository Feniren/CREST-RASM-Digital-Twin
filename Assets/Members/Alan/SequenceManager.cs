using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
// Drives a step-by-step "click the part" sequence.
// Only the current step's part is active/visible in the scene —
// every other part is hidden until it's its turn.
// Put this on a single Managers object, then fill in the Steps array
// in the Inspector, in the order you want them taught.
public class SequenceManager : MonoBehaviour
{
    [System.Serializable]
    public class Step
    {
        [Tooltip("The part's GameObject. Must have an XR Simple Interactable on it. This object will be shown/hidden by the sequence.")]
        public GameObject target;

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
    private XRBaseInteractable[] interactables;
    private UnityEngine.Events.UnityAction<SelectEnterEventArgs>[] listeners;

    private void Start()
    {
        interactables = new XRBaseInteractable[steps.Length];
        listeners = new UnityEngine.Events.UnityAction<SelectEnterEventArgs>[steps.Length];

        for (int i = 0; i < steps.Length; i++)
        {
            if (steps[i].target == null) continue;

            // Hide every part to start clean
            steps[i].target.SetActive(false);

            interactables[i] = steps[i].target.GetComponent<XRBaseInteractable>();
            if (interactables[i] == null)
            {
                Debug.LogError($"[SequenceManager] Step {i} target '{steps[i].target.name}' has no XR Simple Interactable attached.");
                continue;
            }

            int capturedIndex = i; // avoid closure bug — capture per-iteration copy
            listeners[i] = (args) => OnPartClicked(capturedIndex);
            interactables[i].selectEntered.AddListener(listeners[i]);
        }

        ActivateStep(0);
    }

    private void OnDestroy()
    {
        if (interactables == null) return;
        for (int i = 0; i < interactables.Length; i++)
        {
            if (interactables[i] != null && listeners[i] != null)
                interactables[i].selectEntered.RemoveListener(listeners[i]);
        }
    }

    private void OnPartClicked(int index)
    {
        Debug.Log($"[SequenceManager] selectEntered fired for step {index} ({steps[index].target.name}); currentIndex is {currentIndex}");

        // Ignore clicks on parts that aren't the current active step
        if (index != currentIndex) return;

        if (steps[currentIndex].target != null)
            steps[currentIndex].target.SetActive(false);

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

        if (steps[index].target != null)
            steps[index].target.SetActive(true);

        if (infoPanel != null)
            infoPanel.UpdateText(steps[index].infoText);
    }
}