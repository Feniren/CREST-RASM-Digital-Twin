using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Lets the trainee pick a lesson step's REAL part (the slotted table, the
// RFID tag, the rack, the door handle…) instead of a green marker box over
// it. SequenceManager adds this to itself at runtime and hands it the
// current step's parts; nothing to wire in the scene.
//
// Same buttons the rest of the project uses to select things — index
// trigger on either controller, left mouse on desktop — but the pick is
// made against the parts' visible bounds rather than the first collider
// the ray hits, so:
//   - a part needs no collider (the RFID tag text has none, a loaded
//     slotted table switches its own off);
//   - the rack's glass and door in front of a part don't block it.
// Only the current step's parts are tested, so a big bounds (the whole
// rack) can't steal a pick meant for something else.
//
// The project's own select (Player_Controller.LeftClick) still runs on the
// same press, so clicking the real door handle also opens the door.
public class Lesson_Part_Pointer : MonoBehaviour
{
    [Tooltip("Furthest a part can be picked from (m) — the same reach as the project's select raycast.")]
    [SerializeField] private float maxDistance = 10f;

    [Tooltip("Grows each part's bounds by this much (m) on every side, so thin parts are easier to hit.")]
    [SerializeField] private float boundsPadding = 0.02f;

    private readonly List<GameObject> parts = new List<GameObject>();
    private Action<GameObject> onPicked;

    private InputAction leftTrigger, rightTrigger, mouseClick;
    private Transform leftHand, rightHand;

    private static bool XRActive => UnityEngine.XR.XRSettings.isDeviceActive;

    private void Awake()
    {
        leftTrigger = new InputAction("Lesson Part Left", InputActionType.Button, "<XRController>{LeftHand}/triggerPressed");
        rightTrigger = new InputAction("Lesson Part Right", InputActionType.Button, "<XRController>{RightHand}/triggerPressed");
        mouseClick = new InputAction("Lesson Part Mouse", InputActionType.Button, "<Mouse>/leftButton");
        leftTrigger.performed += _ => PickAlong(HandRay(true));
        rightTrigger.performed += _ => PickAlong(HandRay(false));
        mouseClick.performed += _ => { if (!XRActive) PickAlong(ScreenCentreRay()); };
    }

    private void OnDestroy()
    {
        leftTrigger?.Dispose();
        rightTrigger?.Dispose();
        mouseClick?.Dispose();
    }

    // Replaces the parts being listened for. The callback gets the part
    // GameObject exactly as it was passed in.
    public void SetParts(IEnumerable<GameObject> newParts, Action<GameObject> callback)
    {
        parts.Clear();
        foreach (GameObject part in newParts)
            if (part != null)
                parts.Add(part);

        onPicked = callback;
        SetListening(parts.Count > 0);
    }

    public void Clear()
    {
        parts.Clear();
        onPicked = null;
        SetListening(false);
    }

    private void SetListening(bool on)
    {
        if (leftTrigger == null)
            return;

        if (on)
        {
            leftTrigger.Enable();
            rightTrigger.Enable();
            mouseClick.Enable();
        }
        else
        {
            leftTrigger.Disable();
            rightTrigger.Disable();
            mouseClick.Disable();
        }
    }

    private void PickAlong(Ray? ray)
    {
        if (ray == null || parts.Count == 0)
            return;

        GameObject best = null;
        float bestDistance = float.MaxValue;

        foreach (GameObject part in parts)
        {
            if (part == null || !part.activeInHierarchy || !TryGetBounds(part, out Bounds bounds))
                continue;

            bounds.Expand(boundsPadding * 2f);
            if (bounds.IntersectRay(ray.Value, out float distance) && distance <= maxDistance && distance < bestDistance)
            {
                best = part;
                bestDistance = distance;
            }
        }

        if (best != null)
            onPicked?.Invoke(best);
    }

    private Ray? HandRay(bool left)
    {
        if (leftHand == null || rightHand == null)
        {
            Player_Controller player = FindFirstObjectByType<Player_Controller>();
            if (player != null)
            {
                if (player.LeftHand != null) leftHand = player.LeftHand.transform;
                if (player.RightHand != null) rightHand = player.RightHand.transform;
            }
        }

        Transform hand = left ? leftHand : rightHand;
        return hand != null ? new Ray(hand.position, hand.forward) : (Ray?)null;
    }

    private static Ray? ScreenCentreRay()
    {
        Camera viewer = Camera.main;
        return viewer != null ? new Ray(viewer.transform.position, viewer.transform.forward) : (Ray?)null;
    }

    // Combined world bounds of everything visible on the part (falls back to
    // its colliders if it has no renderers). False if there's nothing to aim at.
    public static bool TryGetBounds(GameObject part, out Bounds bounds)
    {
        bounds = default;
        bool found = false;

        foreach (Renderer r in part.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || r is ParticleSystemRenderer)
                continue;
            if (found) bounds.Encapsulate(r.bounds);
            else { bounds = r.bounds; found = true; }
        }

        if (found)
            return true;

        foreach (Collider c in part.GetComponentsInChildren<Collider>())
        {
            if (!c.enabled)
                continue;
            if (found) bounds.Encapsulate(c.bounds);
            else { bounds = c.bounds; found = true; }
        }

        return found;
    }
}
