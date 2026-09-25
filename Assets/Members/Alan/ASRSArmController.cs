using System;
using System.Collections;
using UnityEngine;

public class ASRSArmController : MonoBehaviour
{
    public enum Side { NonU, U, Safe } // NonU = 0deg (rows 01-06), U = 180deg (rows 07-12), Safe = the hardcoded Safe Position rotation (Y = -90), reachable from either side but not itself a rack-facing side

    [Header("Axis Transforms")]
    [SerializeField] private Transform armZ;
    [SerializeField] private Transform armY;
    [SerializeField] private Transform armX;

    [Header("Speeds")]
    [SerializeField] private float moveSpeed = 0.5f;

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = value;
    }

    [Header("Rotation (separate from Arm Y — Rotatation's own Animator, plays ArmRotation / ArmRotationBack)")]
    [SerializeField] private Animator rotationAnimator;

    [Header("Manual-move travel limits (offset from home)")]
    [Tooltip("Z/Y are overwritten automatically at startup by ASRSArmTester.SetZYLimits(), derived from the rack's own corner slots (010001/010006/060001/060006) — these starting values only matter if that hookup is missing. X (depth) isn't part of the slot grid, so it's always manual — tune it to how far the arm can safely extend into a slot.")]
    [SerializeField] private float minZ = -5f;
    [SerializeField] private float maxZ = 5f;
    [SerializeField] private float minY = -5f;
    [SerializeField] private float maxY = 5f;
    [SerializeField] private float minX = -2f;
    [SerializeField] private float maxX = 2f;

    private float? targetZ;
    private float? targetY;
    private float? targetX;
    private bool isRotating;

    private float homeZ;
    private float homeY;
    private float homeX;

    // Fired with "Z"/"Y"/"X" the frame that axis reaches its current target, and
    // with "Rotate" when a side-swap (or a no-op already-home rotate) completes.
    // Drives a SCORBASE-style per-axis "Search Home" checkmark UI.
    public event Action<string> AxisArrived;

    // Fired once, after HomeAll(), the frame every axis and the rotation have
    // both arrived — the analog of SCORBASE's "checkmark next to Robot."
    public event Action AllAxesHomed;

    public Transform ArmZ => armZ;
    public Transform ArmY => armY;
    public Transform ArmX => armX;

    // Current position expressed the same way MoveZ/MoveY/MoveX take it — as an
    // offset from home — so callers (jog buttons, status readouts) never need
    // to know the raw home value itself.
    public float OffsetZ => armZ != null ? armZ.localPosition.z - homeZ : 0f;
    public float OffsetY => armY != null ? armY.localPosition.y - homeY : 0f;
    public float OffsetX => armX != null ? armX.localPosition.x - homeX : 0f;

    // The rig starts facing the U side at rest — not NonU — so RotateY(0) is
    // needed before reaching non-U slots, not the other way around.
    public Side CurrentSide { get; private set; } = Side.U;

    // Safe Position is hardcoded, not read/derived from anything at
    // runtime: Z/Y/X = 0 (home, the same reference every offset is already
    // measured from) and the rotation object's own local Y = -90 — a fixed
    // third orientation, distinct from both the U (180) and NonU (0) sides,
    // driven by its own Animator state ("SafeRotation" on the Rotation
    // controller) rather than RotateY's two-side logic. Kept constant on
    // purpose so it can't drift if the rig's resting pose changes later.
    private const string SafeRotationStateName = "SafeRotation";
    private const float SafeRotationTransitionSeconds = 1.5f;

    // Blends the rotation object smoothly into the Safe Position pose from
    // wherever it currently is (CrossFade, not Play — there's no authored
    // "swing" clip covering every possible starting angle, so the blend
    // itself supplies the motion). CurrentSide becomes Side.Safe once done,
    // which is neither U nor NonU — deliberately forcing any subsequent
    // move to explicitly rotate to whatever side it actually needs, rather
    // than assuming it's already there.
    public void RotateToSafeRotation()
    {
        if (isRotating || rotationAnimator == null)
            return;

        StartCoroutine(RotateToSafeRotationRoutine());
    }

    private IEnumerator RotateToSafeRotationRoutine()
    {
        isRotating = true;

        // CrossFadeInFixedTime, not CrossFade: its transition duration is
        // real seconds, not normalized against the destination clip's own
        // length — which matters here because SafeRotation is a
        // zero-length single-pose clip (no authored "swing" covering every
        // possible starting angle), so a normalized duration against it
        // would be meaningless.
        rotationAnimator.CrossFadeInFixedTime(SafeRotationStateName, SafeRotationTransitionSeconds, 0, 0f);

        yield return new WaitForSeconds(SafeRotationTransitionSeconds);

        isRotating = false;
        CurrentSide = Side.Safe;
        AxisArrived?.Invoke("Rotate");
    }

    // Drives every axis back to Safe Position (Z/Y/X = 0, rotation object
    // at Y = -90) before a GP-style commanded move (Search Home, Pick and
    // Place, Go-to-slot) proceeds with whatever it actually asked for.
    // Manual jog nudges deliberately don't go through this — only
    // higher-level commanded moves do.
    public void ReturnToSafePosition(Action onComplete = null)
    {
        StartCoroutine(ReturnToSafePositionRoutine(onComplete));
    }

    private IEnumerator ReturnToSafePositionRoutine(Action onComplete)
    {
        MoveX(0f);
        MoveZ(0f);
        MoveY(0f);
        RotateToSafeRotation();

        yield return new WaitUntil(() => !IsMoving);

        onComplete?.Invoke();
    }

    public bool IsMoving =>
        targetZ.HasValue || targetY.HasValue || targetX.HasValue || isRotating;

    // True once Search Home (ASRSArmTester.SearchHome -> NotifyHomed) has
    // completed at least once this session — lets
    // callers (e.g. the SCORBASE panel's Pick and Place OK) require the
    // robot to actually be homed first, matching real SCORBASE/SmartCIM
    // where nothing trusts a position until Search Home has run.
    public bool IsHomed { get; private set; }

    private void Awake()
    {
        if (armZ != null) homeZ = armZ.localPosition.z;
        if (armY != null) homeY = armY.localPosition.y;
        if (armX != null) homeX = armX.localPosition.x;

        if (rotationAnimator == null)
            Debug.LogWarning("[ASRSArmController] Rotation Animator not assigned — RotateY will do nothing.", this);
    }

    private void Update()
    {
        CancelRotationTilt();
        MoveAxis(armZ, ref targetZ, Axis.Z);
        MoveAxis(armY, ref targetY, Axis.Y);
        MoveAxis(armX, ref targetX, Axis.X);
    }

    // Called once by ASRSArmTester at startup, after it loads the rack's slot
    // grid — derives the safe Z/Y travel range from the rack's own corner
    // slots instead of a hand-guessed number.
    public void SetZYLimits(float newMinZ, float newMaxZ, float newMinY, float newMaxY)
    {
        minZ = newMinZ;
        maxZ = newMaxZ;
        minY = newMinY;
        maxY = newMaxY;
    }

    // Called by ASRSArmTester once its own Search Home routine — a visible
    // sweep of the rack's corners, ending parked at the resting slot —
    // finishes. Kept as an explicit call rather than Update() polling
    // IsMoving, because a multi-leg routine has IsMoving go true/false
    // between legs, which would otherwise fire AllAxesHomed after just the
    // first leg instead of the whole sequence.
    public void NotifyHomed()
    {
        IsHomed = true;
        AllAxesHomed?.Invoke();
    }

    // Arm Y's baked local rotation only cancels Rotatation's tilt at
    // Rotatation's original resting pose — but the rig actually starts already
    // swung to the U-facing side, and can swing again at runtime. Recomputing
    // the cancellation every frame keeps Arm Y's world rotation clean no
    // matter what Rotatation's current rotation is.
    private void CancelRotationTilt()
    {
        if (rotationAnimator == null || armY == null || armY.parent != rotationAnimator.transform)
            return;

        armY.localRotation = Quaternion.Inverse(rotationAnimator.transform.localRotation);
    }

    // localZ/Y/X are offsets from whichever position the assigned Transform
    // started at (recorded in Awake), not raw absolute local-space values —
    // so "MoveY(1)" always means "1 unit up from home", regardless of what
    // object ends up plugged into the Arm Y field or where it happens to sit.
    //
    // Each clamps to the configured min/max travel limit and returns false if
    // the requested value had to be clamped — so a manual jog that would take
    // the arm out of the rack still moves it right up to the limit, instead
    // of silently doing nothing or overshooting.
    public bool MoveZ(float localZ)
    {
        float clamped = Mathf.Clamp(localZ, minZ, maxZ);
        targetZ = homeZ + clamped;
        return Mathf.Approximately(clamped, localZ);
    }

    public bool MoveY(float localY)
    {
        float clamped = Mathf.Clamp(localY, minY, maxY);
        targetY = homeY + clamped;
        return Mathf.Approximately(clamped, localY);
    }

    public bool MoveX(float localX)
    {
        float clamped = Mathf.Clamp(localX, minX, maxX);
        targetX = homeX + clamped;
        return Mathf.Approximately(clamped, localX);
    }

    // angle 0 = non-U side, angle 180 (or anything else) = U side.
    // No-ops if already on the requested side or a rotation is already in progress.
    public void RotateY(float angle = 180f)
    {
        Side targetSide = Mathf.Approximately(angle, 0f) ? Side.NonU : Side.U;

        if (targetSide == CurrentSide)
        {
            AxisArrived?.Invoke("Rotate");
            return;
        }

        if (isRotating || rotationAnimator == null)
            return;

        // CurrentSide starts at U, so the very first rotation anyone triggers
        // targets NonU — mapped to "ArmRotation" here (not "ArmRotationBack")
        // so that's the clip that plays first.
        string stateName = targetSide == Side.U ? "ArmRotationBack" : "ArmRotation";
        rotationAnimator.Play(stateName, 0, 0f);
        CurrentSide = targetSide;
        StartCoroutine(LockDuringRotation(stateName));
    }

    // Waits on the Animator's own reported progress rather than a fixed
    // duration, so it can't drift out of sync with what's actually playing —
    // a fixed WaitForSeconds only matches reality if it's hand-kept equal to
    // the clip length, and the very first Play() after the Animator becomes
    // active can evaluate faster than normal, throwing that assumption off.
    private IEnumerator LockDuringRotation(string stateName)
    {
        isRotating = true;

        // Play() doesn't take effect until the Animator's own update runs;
        // wait a frame so the state info below reflects the new state, not
        // whatever was playing (or not) before this call.
        yield return null;

        while (rotationAnimator.GetCurrentAnimatorStateInfo(0).IsName(stateName)
               && rotationAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }

        isRotating = false;
        AxisArrived?.Invoke("Rotate");
    }

    // U side = rows 07-12 (TableID 070001-120006); non-U side = rows 01-06 (010001-060006),
    // matching Item_ASRS's TableID = row * 10000 + col.
    public bool IsReachable(int tableID)
    {
        int row = tableID / 10000;
        bool isURow = row >= 7;
        return isURow == (CurrentSide == Side.U);
    }

    public void StopAll()
    {
        targetZ = null;
        targetY = null;
        targetX = null;
    }

    private enum Axis
    {
        X,
        Y,
        Z
    }

    private void MoveAxis(Transform target, ref float? targetValue, Axis axis)
    {
        if (target == null || !targetValue.HasValue)
            return;

        Vector3 pos = target.localPosition;

        float current = GetAxis(pos, axis);
        float next = Mathf.MoveTowards(current, targetValue.Value, moveSpeed * Time.deltaTime);

        SetAxis(ref pos, axis, next);
        target.localPosition = pos;

        if (Mathf.Approximately(next, targetValue.Value))
        {
            targetValue = null;
            AxisArrived?.Invoke(axis.ToString());
        }
    }

    private float GetAxis(Vector3 value, Axis axis)
    {
        switch (axis)
        {
            case Axis.X: return value.x;
            case Axis.Y: return value.y;
            case Axis.Z: return value.z;
            default: return 0f;
        }
    }

    private void SetAxis(ref Vector3 value, Axis axis, float axisValue)
    {
        switch (axis)
        {
            case Axis.X:
                value.x = axisValue;
                break;

            case Axis.Y:
                value.y = axisValue;
                break;

            case Axis.Z:
                value.z = axisValue;
                break;
        }
    }
}