using UnityEngine;

// Click-to-toggle door swing, driven directly off Interactable_Select
// (fires on a raycast left-click/VR-trigger "Interact", per
// Interactable_Select.Interact() -> OnInteractBegin), not the Open/Close
// trigger transitions in the door's own Animator Controller.
//
// Why not the triggers: Idle only ever holds a single static closed pose
// (LeftDoorClosed/RightDoorClosed), so the "Close" transition just crossfades
// into that static pose over its short transition duration — a snap, not a
// visible reverse-swing.
//
// Why not Animator.speed = -1: Unity only allows negative Animator.speed
// while an Animator Recorder is active (Animator.recorderMode !=
// AnimatorRecorderMode.Offline) — setting it at runtime otherwise throws.
// So instead of scrubbing one clip backward, each direction is its own
// clip (e.g. "LeftDoorOpen" / "LeftDoorClose" — the close clip is the open
// motion's keyframes mirrored in time, so it's the same swing played in
// reverse but always evaluated forward), always played at normal +1 speed.
[RequireComponent(typeof(Interactable_Select))]
public class Door_Handle_Toggle : MonoBehaviour
{
    [Tooltip("The hinge's Animator (e.g. LeftHingeJoint/RightHingeJoint's own Animator component) — not the handle itself.")]
    [SerializeField] private Animator doorAnimator;

    [Tooltip("Name of the opening-swing state in the door's Animator Controller (e.g. \"LeftDoorOpen\" / \"RightDoorOpen\").")]
    [SerializeField] private string openStateName = "LeftDoorOpen";

    [Tooltip("Name of the closing-swing state — the open clip's motion mirrored in time (e.g. \"LeftDoorClose\" / \"RightDoorClose\").")]
    [SerializeField] private string closeStateName = "LeftDoorClose";

    private bool isOpen;

    private void Awake()
    {
        GetComponent<Interactable_Select>().OnInteractBegin.AddListener(Toggle);
    }

    private void Toggle()
    {
        if (doorAnimator == null)
        {
            Debug.LogError("[Door_Handle_Toggle] Door Animator is not assigned.", this);
            return;
        }

        isOpen = !isOpen;
        string targetState = isOpen ? openStateName : closeStateName;
        string interruptedState = isOpen ? closeStateName : openStateName;

        // If we're mid-swing in the OPPOSITE clip, start the new one at the
        // mirrored point instead of from its own start — so clicking again
        // before a swing finishes reverses smoothly from the door's actual
        // current position instead of snapping to an endpoint first.
        float startTime = 0f;
        AnimatorStateInfo info = doorAnimator.GetCurrentAnimatorStateInfo(0);
        if (info.IsName(interruptedState))
            startTime = 1f - Mathf.Repeat(info.normalizedTime, 1f);

        doorAnimator.Play(targetState, 0, startTime);
    }
}
