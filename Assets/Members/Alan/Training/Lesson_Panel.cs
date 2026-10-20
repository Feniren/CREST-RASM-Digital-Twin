using UnityEngine;
using UnityEngine.UI;

// Root of the widget-styled Lesson Panel prefab. Points at the panel's parts
// so SequenceManager can be wired to them in one go (see "ASRS/Build Lesson
// Panel In Current Scene"):
//   Display    -> SequenceManager.infoPanel   (title, progress, body text)
//   MediaCard  -> SequenceManager.stationCard (image/video media box)
//   NextButton -> SequenceManager.nextButton  (onClick -> OnNextPressed)
//
// A prefab can't keep a reference to a scene's SequenceManager, so a panel
// dragged into a scene by hand arrives with Next wired to nothing. If Next
// has no persistent listeners at startup, this hooks it to the scene's
// SequenceManager itself.
public class Lesson_Panel : MonoBehaviour
{
    public InstructionDisplay Display;
    public Station_Type_Card MediaCard;
    public Button NextButton;

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
            Debug.LogWarning("[Lesson_Panel] No SequenceManager found in the scene — Next and the media box aren't connected.", this);
            return;
        }

        if (MediaCard != null)
            sequenceManager.UseStationCardIfUnset(MediaCard);

        if (NextButton == null || NextButton.onClick.GetPersistentEventCount() > 0)
            return;

        NextButton.onClick.AddListener(sequenceManager.OnNextPressed);

        Interactable_Select select = NextButton.GetComponent<Interactable_Select>();
        if (select != null && select.OnInteractBegin.GetPersistentEventCount() == 0)
            select.OnInteractBegin.AddListener(sequenceManager.OnNextPressed);
    }
}
