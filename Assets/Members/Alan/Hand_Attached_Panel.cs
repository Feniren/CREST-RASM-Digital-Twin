using System.Collections;
using UnityEngine;

// Parents this object onto the player's left-hand controller anchor at
// runtime, turning it into a wrist-mounted panel. Not wireable as a scene
// reference in the Inspector — the player (Entity_Player, with its
// LeftHandAnchor) is spawned from a persistent bootstrap scene rather than
// existing in this scene at edit time, so it has to be found once Play mode
// actually starts.
//
// Entity_Player only activates LeftHandAnchor when an XR device is actually
// running (see Entity_Player.LaunchXR) — on desktop it stays inactive, so
// this deliberately leaves the panel at wherever it was placed in the editor
// instead of forcing it onto a hand that doesn't exist, keeping desktop
// testing working the way it already does elsewhere in this project.
public class Hand_Attached_Panel : MonoBehaviour
{
    [Tooltip("Local position offset from the hand anchor. Tune in-editor once attached (enter Play mode, adjust, copy the values back here) to match your controller model.")]
    [SerializeField] private Vector3 localPositionOffset = new Vector3(0f, 0.05f, 0.05f);

    [Tooltip("Local rotation offset from the hand anchor, in Euler angles — tilts the panel to face up toward the player's eyes when they raise their wrist.")]
    [SerializeField] private Vector3 localEulerOffset = new Vector3(60f, 0f, 0f);

    private void Start()
    {
        StartCoroutine(AttachRoutine());
    }

    private IEnumerator AttachRoutine()
    {
        // Entity_Player.LaunchXR runs its own XR-detection check after a
        // short delay — wait past that before deciding whether a hand
        // anchor is actually available to attach to.
        yield return new WaitForSeconds(0.3f);

        Entity_Player player = FindFirstObjectByType<Entity_Player>();

        if (player == null || player.LeftHandAnchor == null || !player.LeftHandAnchor.activeSelf)
            yield break;

        transform.SetParent(player.LeftHandAnchor.transform, false);
        transform.localPosition = localPositionOffset;
        transform.localRotation = Quaternion.Euler(localEulerOffset);
    }
}
