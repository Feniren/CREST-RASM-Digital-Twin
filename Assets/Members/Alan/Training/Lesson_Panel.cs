using UnityEngine;
using UnityEngine.UI;

// Root of the widget-styled Lesson Panel prefab. Points at the panel's parts
// so SequenceManager can be wired to them in one go (see "ASRS/Build Lesson
// Panel In Current Scene"):
//   Display    -> SequenceManager.infoPanel   (title, progress, body text)
//   MediaCard  -> SequenceManager.stationCard (image/video media box)
//   NextButton -> SequenceManager.nextButton  (onClick -> OnNextPressed)
//   HintButton -> SequenceManager.hintButton  (onClick -> OnHintPressed)
//
// A prefab can't keep a reference to a scene's SequenceManager, so a panel
// dragged into a scene by hand arrives with Next/Hint wired to nothing. If a
// button has no persistent listeners at startup, this hooks it to the
// scene's SequenceManager itself.
public class Lesson_Panel : MonoBehaviour
{
    public InstructionDisplay Display;
    public Station_Type_Card MediaCard;
    public Button NextButton;
    [Tooltip("Optional — shown only on steps with a Hint Target; flies that part up in front of the trainee (Hint_PartShowcase).")]
    public Button HintButton;

    [Tooltip("Optional — found in the scene automatically when left empty.")]
    [SerializeField] private SequenceManager sequenceManager;

    // Awake, so this is in place before SequenceManager.Start() shows the
    // first step.
    private void Awake()
    {
        if (sequenceManager == null)
            sequenceManager = FindFirstObjectByType<SequenceManager>();

        if (sequenceManager == null)
        {
            Debug.LogWarning("[Lesson_Panel] No SequenceManager found in the scene — Next, Hint and the media box aren't connected.", this);
            return;
        }

        if (MediaCard != null)
            sequenceManager.UseStationCardIfUnset(MediaCard);

        if (HintButton != null)
            sequenceManager.UseHintButtonIfUnset(HintButton.gameObject);

        WireIfUnwired(NextButton, sequenceManager.OnNextPressed);
        WireIfUnwired(HintButton, sequenceManager.OnHintPressed);
    }

    private static void WireIfUnwired(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || button.onClick.GetPersistentEventCount() > 0)
            return;

        button.onClick.AddListener(action);

        Interactable_Select select = button.GetComponent<Interactable_Select>();
        if (select != null && select.OnInteractBegin.GetPersistentEventCount() == 0)
            select.OnInteractBegin.AddListener(action);
    }
}
