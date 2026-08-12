using System;
using System.Collections;
using UnityEngine;

public class ASRSArmController : MonoBehaviour
{
    public enum Side { NonU, U } // NonU = 0deg (rows 01-06), U = 180deg (rows 07-12)

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
    [Tooltip("Must match the length of ArmRotation / ArmRotationBack so rotation can't be re-triggered mid-swing.")]
    [SerializeField] private float rotationDuration = 6f;

    private float? targetZ;
    private float? targetY;
    private float? targetX;
    private bool isRotating;
    private bool homingInProgress;

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

    public bool IsMoving =>
        targetZ.HasValue || targetY.HasValue || targetX.HasValue || isRotating;

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

        if (homingInProgress && !IsMoving)
        {
            homingInProgress = false;
            AllAxesHomed?.Invoke();
        }
    }

    // Drives every axis back to its recorded home offset and the default
    // (U) side, mirroring SCORBASE's "Search Home" — each axis (and the
    // rotation) reports back via AxisArrived, then AllAxesHomed fires once
    // the whole arm is idle again.
    public void HomeAll()
    {
        homingInProgress = true;
        MoveZ(0f);
        MoveY(0f);
        MoveX(0f);
        RotateY(180f);
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
    public void MoveZ(float localZ)
    {
        targetZ = homeZ + localZ;
    }

    public void MoveY(float localY)
    {
        targetY = homeY + localY;
    }

    public void MoveX(float localX)
    {
        targetX = homeX + localX;
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

        rotationAnimator.Play(targetSide == Side.U ? "ArmRotation" : "ArmRotationBack");
        CurrentSide = targetSide;
        StartCoroutine(LockDuringRotation());
    }

    private IEnumerator LockDuringRotation()
    {
        isRotating = true;
        yield return new WaitForSeconds(rotationDuration);
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