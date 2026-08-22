using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Animates a part flying up in front of the user's face, enlarging and
// spinning in place, then returning to its exact original spot — wire a
// Hint button's OnClick() to PlayHint(). Position while showcased is
// recomputed from Camera.main every frame, so it keeps following the user
// if they walk during the flight or the hold.
public class Hint_PartShowcase : MonoBehaviour
{
    [Tooltip("Default part to fly up and showcase, used when PlayHint() is called with no override (e.g. wired directly to a button). Restored to its exact original parent/position/rotation/scale when the hint finishes.")]
    [SerializeField] private Transform partToShowcase;

    [Header("Placement")]
    [SerializeField] private float distanceFromFace = 0.6f;
    [Tooltip("The part's largest bounds dimension (in meters) once showcased — parts are scaled up OR down to hit this consistently, rather than by a flat multiplier, so a big part doesn't end up so enlarged the camera is effectively inside it.")]
    [SerializeField] private float targetViewSize = 0.35f;
    [Tooltip("Fallback scale multiplier used only if a part has no Renderer to measure bounds from.")]
    [SerializeField] private float fallbackScaleMultiplier = 10f;

    [Header("Timing")]
    [SerializeField] private float flightDuration = 1f;
    [SerializeField] private float holdDuration = 5f;
    [Tooltip("Degrees/sec the part spins while held in front of the face.")]
    [SerializeField] private float spinSpeed = 60f;

    public bool IsShowcasing { get; private set; }

    private Coroutine activeRoutine;

    // Hook this to the Hint button's OnClick() in the Inspector, or call it
    // from code (e.g. SequenceManager.OnHintPressed()) with the current
    // step's part. Falls back to the serialized partToShowcase if target is
    // left null — that keeps direct Inspector-only wiring working too.
    public void PlayHint(Transform target = null)
    {
        Transform part = target != null ? target : partToShowcase;

        if (part == null)
        {
            Debug.LogWarning("[Hint_PartShowcase] No part to showcase — neither an override nor partToShowcase is assigned.", this);
            return;
        }

        if (IsShowcasing)
            return; // already mid-animation — ignore re-clicks

        if (Camera.main == null)
        {
            Debug.LogWarning("[Hint_PartShowcase] No Camera.main found.", this);
            return;
        }

        activeRoutine = StartCoroutine(ShowcaseRoutine(part));
    }

    private IEnumerator ShowcaseRoutine(Transform part)
    {
        IsShowcasing = true;
        Camera cam = Camera.main;

        Transform originalParent = part.parent;
        Vector3 originalLocalPos = part.localPosition;
        Quaternion originalLocalRot = part.localRotation;
        Vector3 originalLocalScale = part.localScale;

        Vector3 startWorldPos = part.position;
        Quaternion startWorldRot = part.rotation;
        Vector3 startWorldScale = part.lossyScale;

        // Spin around the part's own visual center, not its transform pivot
        // — most meshes aren't pivoted at their center, and spinning an
        // off-center pivot at showcase scale swings the mesh through a wide
        // arc instead of turning in place. pivotToCenter is the constant
        // (rotation-relative) offset from pivot to visual center; it scales
        // and rotates along with the part every frame below.
        Bounds worldBounds = GetWorldBounds(part);
        Vector3 worldCenter = worldBounds.center;
        Vector3 pivotToCenter = Quaternion.Inverse(startWorldRot) * (worldCenter - startWorldPos);

        // Scale to a consistent viewed size instead of a flat multiplier —
        // otherwise a part that's already large ends up so enlarged the
        // camera is effectively inside it at showcase distance.
        float largestDimension = Mathf.Max(worldBounds.size.x, worldBounds.size.y, worldBounds.size.z);
        float scaleFactor = largestDimension > 0.0001f ? targetViewSize / largestDimension : fallbackScaleMultiplier;

        // Colliders off for the round trip — an enlarged part hovering at
        // face height in VR is very easy to grab by accident mid-animation.
        List<Collider> disabledColliders = new List<Collider>();
        foreach (Collider col in part.GetComponentsInChildren<Collider>(true))
        {
            if (col.enabled)
            {
                col.enabled = false;
                disabledColliders.Add(col);
            }
        }

        Rigidbody rb = part.GetComponent<Rigidbody>();
        bool wasKinematic = false;
        if (rb != null)
        {
            wasKinematic = rb.isKinematic;
            rb.isKinematic = true;
        }

        // Detach so the flight math stays in plain world space, unaffected
        // by whatever the original parent is doing underneath it.
        part.SetParent(null, true);

        Vector3 targetScale = startWorldScale * scaleFactor;

        yield return FlyTo(part, worldCenter, startWorldRot, startWorldScale,
            () => cam.transform.position + cam.transform.forward * distanceFromFace,
            () => Quaternion.LookRotation(cam.transform.forward),
            targetScale, pivotToCenter, startWorldScale, flightDuration);

        // Hold in front of the face, spinning in place around its own
        // center (position target below is constant per-frame, not
        // rotation-dependent), still tracking the user.
        float elapsed = 0f;
        float spinAngle = 0f;
        while (elapsed < holdDuration)
        {
            elapsed += Time.deltaTime;
            spinAngle += spinSpeed * Time.deltaTime;

            Vector3 faceCenter = cam.transform.position + cam.transform.forward * distanceFromFace;
            Quaternion rot = Quaternion.LookRotation(cam.transform.forward) * Quaternion.AngleAxis(spinAngle, Vector3.up);

            part.rotation = rot;
            part.position = PivotForCenter(faceCenter, rot, targetScale, pivotToCenter, startWorldScale);

            yield return null;
        }

        // Fly back to its original spot.
        Vector3 holdCenter = cam.transform.position + cam.transform.forward * distanceFromFace;
        yield return FlyTo(part, holdCenter, part.rotation, part.localScale,
            () => worldCenter, () => startWorldRot, startWorldScale, pivotToCenter, startWorldScale, flightDuration);

        // Snap back onto its original parent/local transform exactly, so
        // there's no floating-point drift from the round trip.
        part.SetParent(originalParent, false);
        part.localPosition = originalLocalPos;
        part.localRotation = originalLocalRot;
        part.localScale = originalLocalScale;

        foreach (Collider col in disabledColliders)
            if (col != null)
                col.enabled = true;

        if (rb != null)
            rb.isKinematic = wasKinematic;

        IsShowcasing = false;
        activeRoutine = null;
    }

    // Combined world-space bounds of every renderer on the part, so we can
    // find its visual center and size regardless of where its transform
    // pivot is or how big its mesh actually is.
    private static Bounds GetWorldBounds(Transform t)
    {
        Renderer[] renderers = t.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return new Bounds(t.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    // How much pivotToCenter has grown/shrunk since it was measured at baseScale.
    private static Vector3 ScaleRatio(Vector3 currentScale, Vector3 baseScale)
    {
        return new Vector3(
            baseScale.x != 0 ? currentScale.x / baseScale.x : 1f,
            baseScale.y != 0 ? currentScale.y / baseScale.y : 1f,
            baseScale.z != 0 ? currentScale.z / baseScale.z : 1f);
    }

    // The pivot position that puts the part's visual center at centerTarget,
    // given its current rotation/scale.
    private static Vector3 PivotForCenter(Vector3 centerTarget, Quaternion rotation, Vector3 currentScale,
        Vector3 pivotToCenter, Vector3 baseScale)
    {
        Vector3 offset = rotation * Vector3.Scale(pivotToCenter, ScaleRatio(currentScale, baseScale));
        return centerTarget - offset;
    }

    // Elapsed-time Lerp of the part's visual center (not its pivot) from a
    // fixed start to a target re-evaluated every frame (so it can track a
    // moving camera during the flight), keeping the spin anchored in place.
    private static IEnumerator FlyTo(Transform part, Vector3 startCenter, Quaternion startRot, Vector3 startScale,
        System.Func<Vector3> targetCenter, System.Func<Quaternion> targetRot, Vector3 targetScale,
        Vector3 pivotToCenter, Vector3 baseScale, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            Vector3 center = Vector3.Lerp(startCenter, targetCenter(), t);
            Quaternion rot = Quaternion.Slerp(startRot, targetRot(), t);
            Vector3 scale = Vector3.Lerp(startScale, targetScale, t);

            part.localScale = scale;
            part.rotation = rot;
            part.position = PivotForCenter(center, rot, scale, pivotToCenter, baseScale);

            yield return null;
        }
    }
}
