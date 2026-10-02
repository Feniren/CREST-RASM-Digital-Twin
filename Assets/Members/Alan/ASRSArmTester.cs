using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ASRSArmTester : MonoBehaviour
{
    [SerializeField] private ASRSArmController armController;

    [Header("Manual Axis Targets")]
    [SerializeField] private float zTarget = 2f;
    [SerializeField] private float yTarget = 1f;
    [SerializeField] private float xTarget = 1f;

    [Header("Reset")]
    [SerializeField] private float resetPosition = 0f;

    // ── Slot Grid ──────────────────────────────────────────────────────────
    // Axis mapping:
    //   Z = row    (vertical height along the rack)
    //   Y = column (horizontal left-right along the rack)
    //   X = depth  (arm extending into a slot)
    //
    // SlotsA: 010001–060006 (rows 1-6, cols 1-6, 36 slots).
    // SlotsB: 070001–120006 (rows 7-12, cols 1-6, 36 slots).
    // Assign both empty GameObjects below. Each one's children must be ordered
    // left→right, bottom→top in the Hierarchy.
    // Slot index 0-35 = SlotsA, 36-71 = SlotsB.

    [Header("Slot Grid — SlotsA (010001–060006)")]
    [Tooltip("Parent GameObject whose children are the 36 SlotsA Transforms, ordered left→right bottom→top.")]
    [SerializeField] private Transform slotsContainerA;

    [Header("Slot Grid — SlotsB (070001–120006)")]
    [Tooltip("Parent GameObject whose children are the 36 SlotsB Transforms, ordered left→right bottom→top.")]
    [SerializeField] private Transform slotsContainerB;

    [Tooltip("Index within SlotsB (the U side the rig rests facing) of the slot the arm physically sits at when all axes are at local 0. " +
             "This one physical position is the home reference for BOTH sides — SlotsA's columns run in the opposite Z direction, so the same index there is a different column.")]
    [SerializeField] private int centerSlotIndex = 15;
    [Tooltip("Additional world-space nudge applied to every slot position. Fine-tune if the slot pivot is not exactly where the arm should align.")]
    [SerializeField] private Vector3 slotOffset = Vector3.zero;
    [Tooltip("Optional override for the Z distance between two physically adjacent columns, in SlotsB's column order (col 1 → col 6), sign included. When non-zero, Z is computed arithmetically from the column difference instead of read from the slot Transform positions. SlotsA's reversed column order is accounted for automatically. Leave at 0 to keep using the measured Transform positions.")]
    [SerializeField] private float columnSpacingZ = 0f;
    [Tooltip("Local X when the arm is parked in front of a slot (not extended). X is the depth axis.")]
    [SerializeField] private float parkedX = 0f;
    [Tooltip("Seconds the arm pauses at each slot during auto-traverse before moving on.")]
    [SerializeField] private float autoTraverseDelay = 0.5f;

    [Header("Search Home")]
    [Tooltip("Seconds the arm pauses at each corner during Search Home, so the sweep reads as a visible sequence of moves rather than one instant jump.")]
    [SerializeField] private float searchHomeCornerDelay = 0.3f;

    [Header("Manual Travel Bounds (optional)")]
    [Tooltip("If enabled, this single Z/Y range is used directly for BOTH sides instead of computing limits from the rack's corner slot transforms — simpler and more reliable if you've already measured the real safe travel range yourself (e.g. by jogging to each corner and reading the position off the panel).")]
    [SerializeField] private bool useManualTravelBounds = false;
    [SerializeField] private float manualMinZ = 0f;
    [SerializeField] private float manualMaxZ = 0f;
    [SerializeField] private float manualMinY = 0f;
    [SerializeField] private float manualMaxY = 0f;

    // Read-only so other scripts (e.g. the gripper controller) can match this
    // exact depth for their own "parked, not reaching in" moves instead of
    // needing a second field that could silently drift out of sync with it.
    public float ParkedX => parkedX;

    private Transform[] slotsA;
    private Transform[] slotsB;

    private const int Rows = 6;
    private const int Cols = 6;
    private const int SlotsPerSide = Rows * Cols; // 36
    // Public so other scripts (e.g. the SCORBASE panel's Pick and Place
    // index validation) can reference the real slot count instead of a
    // hardcoded magic number.
    public const int TotalSlots = SlotsPerSide * 2; // 72

    private int currentSlotIndex = 0;
    private bool isAutoTraversing = false;
    private Coroutine traverseCoroutine;
    private Coroutine searchHomeCoroutine;

    private void Awake()
    {
        if (slotsContainerA == null || slotsContainerB == null)
        {
            Debug.LogError("[ASRS] Both Slots Container A and Slots Container B must be assigned.", this);
            return;
        }

        slotsA = LoadSlots(slotsContainerA);
        slotsB = LoadSlots(slotsContainerB);

        currentSlotIndex = SlotsPerSide + centerSlotIndex; // start on SlotsB, matching the rig's U-facing rest pose

        ApplyTravelLimitsForCurrentSide();

        if (armController != null)
            armController.AxisArrived += OnArmAxisArrived;

        Debug.Log($"[ASRS] Loaded {slotsA.Length} SlotsA + {slotsB.Length} SlotsB slots. Tracking starts at index {currentSlotIndex}.");
    }

    private void OnDestroy()
    {
        if (armController != null)
            armController.AxisArrived -= OnArmAxisArrived;
    }

    // Refreshes the travel limits every time a rotation actually completes
    // (Search Home's own rotate-to-NonU, a Go-to-slot's rotate-to-target,
    // the manual Rotate Side button — anything that changes CurrentSide),
    // so whichever side is now faced always has its own real limits before
    // anything tries to move to that side's corners.
    private void OnArmAxisArrived(string axis)
    {
        if (axis == "Rotate")
            ApplyTravelLimitsForCurrentSide();
    }

    // Derives the arm's safe Z/Y travel range from the rack's own corner
    // slots instead of a hand-guessed number — and, critically, recomputes
    // it fresh for WHICHEVER side is currently faced, rather than measuring
    // once (at Awake, on SlotsB/U) and reusing a single symmetric ±guess
    // for both sides forever after.
    //
    // That symmetric-guess approach was a deliberate workaround for a real
    // constraint — at Awake() the rig is still facing U, so SlotsA's own
    // corners can't be measured through the correct (NonU) rotation frame
    // yet — but it silently assumed both sides' corners sit at the same
    // distance from center. If they don't (the rack's real anchor
    // positions were hand-tuned, not perfectly symmetric), the resulting
    // clamp is tighter than SlotsA's real extent on at least one axis —
    // and since MoveZ/MoveY silently clamp, that clips Search Home's
    // corner-sweep targets short of the actual corner by a fixed amount:
    // exactly the "constant offset at the boundaries" symptom. Manual
    // moves toward the middle of the range never hit that ceiling, so they
    // looked correct.
    //
    // Fix: recompute exact (non-symmetric) limits for the CURRENT side
    // every time the side actually changes (subscribed to AxisArrived
    // below), so whichever side Search Home/a GP move is about to use
    // always has its own real measured bounds, not a guess mirrored from
    // the other side.
    private void ApplyTravelLimitsForCurrentSide()
    {
        if (armController == null)
        {
            Debug.LogWarning("[ASRS] No ASRSArmController assigned — can't set travel limits.", this);
            return;
        }

        // Known-good endpoints, measured directly rather than derived from
        // slot transforms — skips all the corner-computation/rotation-frame
        // fragility below entirely, same range regardless of which side is
        // faced.
        if (useManualTravelBounds)
        {
            armController.SetZYLimits(manualMinZ, manualMaxZ, manualMinY, manualMaxY);
            return;
        }

        // Only U/NonU have their own rack corners to measure against — Safe
        // Position isn't a rack-facing side, so there's nothing to
        // recompute for it (the limits from whichever real side was last
        // faced stay in effect).
        if (armController.CurrentSide != ASRSArmController.Side.U && armController.CurrentSide != ASRSArmController.Side.NonU)
            return;

        bool wantsB = armController.CurrentSide == ASRSArmController.Side.U;

        int[] cornerIndices =
        {
            0,                                    // col 1, row nearest home
            Cols - 1,                             // col 6, row nearest home
            (Rows - 1) * Cols,                     // col 1, row farthest
            (Rows - 1) * Cols + (Cols - 1)         // col 6, row farthest
        };

        float minZ = float.MaxValue, maxZ = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        bool any = false;

        foreach (int index in cornerIndices)
        {
            if (!TryComputeSlotDelta(wantsB, index, out float z, out float y))
                continue;

            any = true;
            minZ = Mathf.Min(minZ, z);
            maxZ = Mathf.Max(maxZ, z);
            minY = Mathf.Min(minY, y);
            maxY = Mathf.Max(maxY, y);
        }

        if (!any)
        {
            Debug.LogWarning("[ASRS] Could not compute corner travel limits — corner slot transforms not ready.", this);
            return;
        }

        armController.SetZYLimits(minZ, maxZ, minY, maxY);
        Debug.Log($"[ASRS] Travel limits set from {(wantsB ? "SlotsB" : "SlotsA")}'s own corners: Z [{minZ:F2}, {maxZ:F2}]  Y [{minY:F2}, {maxY:F2}]");
    }

    private Transform[] LoadSlots(Transform container)
    {
        Transform[] slots = new Transform[container.childCount];
        for (int i = 0; i < container.childCount; i++)
            slots[i] = container.GetChild(i);
        return slots;
    }

    private void Update()
    {
        if (Keyboard.current == null || armController == null)
            return;

        // Skip every debug keybind below while any UI element is focused —
        // e.g. typing "12" or "34" into the SCORBASE panel's Source/Target
        // Index fields (1-72) also presses these same digit keys, which
        // would otherwise jog the arm mid-typing before OK is ever pressed.
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            return;

        // ── Original keybinds (unchanged) ─────────────────────────────────

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            Debug.Log("Moving Z");
            armController.MoveZ(zTarget);
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            Debug.Log("Moving Y");
            armController.MoveY(yTarget);
        }

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            Debug.Log("Rotating Y 180");
            armController.RotateY();
        }

        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            Debug.Log("Moving X");
            armController.MoveX(xTarget);
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            Debug.Log("Resetting");
            StopAutoTraverse();
            armController.MoveZ(resetPosition);
            armController.MoveY(resetPosition);
            armController.RotateY(0f);
            armController.MoveX(resetPosition);
            currentSlotIndex = 0;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("Stopping");
            StopAutoTraverse();
            armController.StopAll();
        }

        // ── Slot traversal keybinds ────────────────────────────────────────
        // Right Arrow  — advance one slot (left→right, bottom→top, wraps across sides)
        // Left Arrow   — retreat one slot
        // Up Arrow     — move up one row (same column, same side)
        // Down Arrow   — move down one row (same column, same side)
        // H            — jump to slot index 0 (010001, SlotsA)
        // T            — toggle automatic full-rack traversal (all 72 slots)

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            StopAutoTraverse();
            currentSlotIndex = (currentSlotIndex + 1) % TotalSlots;
            MoveToCurrentSlot();
        }

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            StopAutoTraverse();
            currentSlotIndex = (currentSlotIndex - 1 + TotalSlots) % TotalSlots;
            MoveToCurrentSlot();
        }

        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            StopAutoTraverse();
            int next = currentSlotIndex + Cols;
            if (next < TotalSlots && SameSide(currentSlotIndex, next))
            {
                currentSlotIndex = next;
                MoveToCurrentSlot();
            }
            else
                Debug.Log("[ASRS] Already at the top row on this side.");
        }

        if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            StopAutoTraverse();
            int next = currentSlotIndex - Cols;
            if (next >= 0 && SameSide(currentSlotIndex, next))
            {
                currentSlotIndex = next;
                MoveToCurrentSlot();
            }
            else
                Debug.Log("[ASRS] Already at the bottom row on this side.");
        }

        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            StopAutoTraverse();
            currentSlotIndex = 0;
            MoveToCurrentSlot();
        }

        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            if (isAutoTraversing)
                StopAutoTraverse();
            else
                StartAutoTraverse();
        }
    }

    // SCORBASE-style "Search Home": a visible sweep of the rack's four
    // corners (010001 -> 060001 -> 060006 -> 010006) on the 1-36 (NonU)
    // side, ending parked at the bottom-right corner — 010006 — instead of
    // an instant snap back to wherever the arm happened to rest. Retracts
    // the depth axis first (a real ASRS/SCORBASE controller always fully
    // retracts before any Z/Y travel), then rotates to the NonU side if
    // needed, then walks the perimeter.
    public void SearchHome()
    {
        if (armController == null || slotsA == null)
        {
            Debug.LogWarning("[ASRS] Arm controller or SlotsA not ready — can't Search Home.", this);
            return;
        }

        StopAutoTraverse();

        if (searchHomeCoroutine != null)
            StopCoroutine(searchHomeCoroutine);

        searchHomeCoroutine = StartCoroutine(SearchHomeRoutine());
    }

    private IEnumerator SearchHomeRoutine()
    {
        // Search Home is itself a GP-style commanded move — return to Safe
        // Position first, same as any other, before starting its own
        // corner-sweep sequence.
        bool safeReached = false;
        armController.ReturnToSafePosition(() => safeReached = true);
        yield return new WaitUntil(() => safeReached);

        armController.MoveX(0f);
        yield return new WaitUntil(() => !armController.IsMoving);

        if (armController.CurrentSide != ASRSArmController.Side.NonU)
        {
            armController.RotateY(0f);
            yield return new WaitUntil(() => !armController.IsMoving);
        }

        int[] cornerLocalIndices =
        {
            0,                              // row 1, col 1 (010001) — bottom-left
            (Rows - 1) * Cols,              // row 6, col 1 (060001) — top-left
            (Rows - 1) * Cols + (Cols - 1), // row 6, col 6 (060006) — top-right
            Cols - 1                        // row 1, col 6 (010006) — bottom-right, final
        };

        foreach (int localIndex in cornerLocalIndices)
        {
            if (!TryComputeSlotDelta(false, localIndex, out float targetZ, out float targetY))
                continue;

            armController.MoveZ(targetZ);
            armController.MoveY(targetY);
            yield return new WaitUntil(() => !armController.IsMoving);
            yield return new WaitForSeconds(searchHomeCornerDelay);
        }

        currentSlotIndex = cornerLocalIndices[cornerLocalIndices.Length - 1];
        searchHomeCoroutine = null;
        armController.NotifyHomed();
    }

    private bool SameSide(int indexA, int indexB) =>
        (indexA < SlotsPerSide) == (indexB < SlotsPerSide);

    // Converts a plain 1-based slot number (1-TotalSlots) into the rack's
    // own row*10000+col TableID string — the same addressing
    // TryMoveToTableId already accepts, and the same math MoveToCurrentSlot
    // uses for its own display string. Lets a trainee (or the SCORBASE
    // panel's Source/Target Index fields) address a slot by its plain
    // number instead of memorizing the row/column TableID format.
    public static string IndexToTableId(int oneBasedIndex)
    {
        int slotsPerSide = Rows * Cols;
        int zeroBasedIndex = oneBasedIndex - 1;
        bool wantsB = zeroBasedIndex >= slotsPerSide;
        int localIndex = wantsB ? zeroBasedIndex - slotsPerSide : zeroBasedIndex;
        int row = localIndex / Cols;
        int col = localIndex % Cols;
        int displayRow = wantsB ? row + 7 : row + 1;
        return (displayRow * 10000 + col + 1).ToString("D6");
    }

    // Moves the arm to the current slot using a delta from that side's own
    // center slot. Rotates first if the target slot is on the other side from
    // where the arm currently is.
    private void MoveToCurrentSlot()
    {
        bool wantsB = currentSlotIndex >= SlotsPerSide;
        int localIndex = wantsB ? currentSlotIndex - SlotsPerSide : currentSlotIndex;

        ASRSArmController.Side wantedSide = wantsB ? ASRSArmController.Side.U : ASRSArmController.Side.NonU;
        if (armController.CurrentSide != wantedSide)
            armController.RotateY(wantsB ? 180f : 0f);

        if (!TryComputeSlotDelta(wantsB, localIndex, out float targetZ, out float targetY))
            return;

        int row = localIndex / Cols;
        int col = localIndex % Cols;
        int displayRow = wantsB ? row + 7 : row + 1;
        string tableId = (displayRow * 10000 + col + 1).ToString("D6");

        Debug.Log($"[ASRS] Slot {currentSlotIndex + 1}/{TotalSlots} → {tableId}" +
                  $"  ({(wantsB ? "SlotsB" : "SlotsA")}, Row {displayRow}, Col {col + 1})" +
                  $"  Z={targetZ:F2}  Y={targetY:F2}");

        armController.MoveZ(targetZ);
        armController.MoveY(targetY);
        armController.MoveX(parkedX);
    }

    // Shared core of the slot-to-world-delta math: given a side and a 0-35
    // index within that side's 6x6 grid, returns the Z/Y offsets (from that
    // side's own center slot) that ASRSArmController.MoveZ/MoveY expect.
    // Used by both keyboard traversal (MoveToCurrentSlot) and ID-based
    // addressing (TryMoveToTableId) so the two paths can't drift apart.
    private bool TryComputeSlotDelta(bool wantsB, int localIndex, out float targetZ, out float targetY)
    {
        targetZ = 0f;
        targetY = 0f;

        Transform[] slots = wantsB ? slotsB : slotsA;

        if (slots == null || localIndex < 0 || localIndex >= slots.Length)
        {
            Debug.LogWarning("[ASRS] Slot transforms not ready or index out of range.");
            return false;
        }

        Transform slot = slots[localIndex];
        if (slot == null)
        {
            Debug.LogWarning($"[ASRS] Slot {localIndex} has no Transform.");
            return false;
        }

        if (!TryGetHomeReference(out Transform center))
            return false;

        // World-space vector from the arm's home reference to the target
        // slot. InverseTransformDirection maps direction (no translation)
        // into local space, giving the correct per-axis delta regardless of
        // parent position.
        Vector3 worldDelta = slot.position + slotOffset - center.position;
        ToArmAxes(worldDelta, out targetZ, out targetY);

        // Manual override: replace the measured Z with an exact arithmetic
        // value from the column difference, instead of trusting the slot
        // Transforms' real positions (which may not be perfectly consistent
        // slot-to-slot). SlotsA is the mirror image of SlotsB (its column 1
        // sits where SlotsB's column 6 does), so convert to SlotsB's
        // physical column order before comparing against the reference.
        if (!Mathf.Approximately(columnSpacingZ, 0f))
        {
            int col = localIndex % Cols;
            int physicalCol = wantsB ? col : (Cols - 1) - col;
            int centerCol = centerSlotIndex % Cols;
            targetZ = (physicalCol - centerCol) * columnSpacingZ;
        }

        return true;
    }

    // The arm's home (all axes at local 0) is ONE physical position — it
    // doesn't move when the rig rotates to face the other rack. So every slot
    // on both sides must be measured from that same point.
    //
    // Previously each side was measured from its OWN slot at centerSlotIndex,
    // but the two racks are mirror images: SlotsB's columns run +Z (col 1 at
    // the low-Z end) while SlotsA's run -Z (col 1 at the high-Z end). Index 15
    // is therefore a different physical column on each side — one full
    // slotted-table width apart (~0.197). The U side lined up, but every NonU
    // (SlotsA) target, and the travel limits derived from its corners, was
    // shifted one column: short of the rack at one Z end and past it at the
    // other. A single slotOffset couldn't fix it because the error only
    // existed on one side.
    private bool TryGetHomeReference(out Transform center)
    {
        center = null;

        if (slotsB == null || centerSlotIndex < 0 || centerSlotIndex >= slotsB.Length)
        {
            Debug.LogWarning("[ASRS] Center slot index is out of range for SlotsB.");
            return false;
        }

        center = slotsB[centerSlotIndex];
        if (center == null)
        {
            Debug.LogWarning("[ASRS] Center slot transform is null.");
            return false;
        }

        return true;
    }

    private void ToArmAxes(Vector3 worldDelta, out float targetZ, out float targetY)
    {
        Transform zParent = armController.ArmZ != null ? armController.ArmZ.parent : null;
        Transform yParent = armController.ArmY != null ? armController.ArmY.parent : null;

        targetZ = zParent != null ? zParent.InverseTransformDirection(worldDelta).z : worldDelta.z;
        targetY = yParent != null ? yParent.InverseTransformDirection(worldDelta).y : worldDelta.y;
    }

    // General-purpose version of the slot-delta math for an arbitrary world
    // point — e.g. where the gripper should drop a table at the conveyor —
    // rather than one of the rack's own numbered slots. Measured from the
    // same shared home reference as the slots, so it's correct whichever
    // side the arm is facing.
    public bool TryComputeDeltaToWorldPoint(Vector3 worldPoint, out float targetZ, out float targetY)
    {
        targetZ = 0f;
        targetY = 0f;

        if (armController == null || !TryGetHomeReference(out Transform center))
            return false;

        ToArmAxes(worldPoint - center.position, out targetZ, out targetY);
        return true;
    }

    // ID-based addressing for a SCORBASE-style "Go to position" control: pass
    // the same row*10000+col TableID format Item_ASRS/MoveToCurrentSlot already
    // use (e.g. "070003"), and the arm drives straight there. Returns false
    // (with a warning) for an out-of-range or wrong-side ID instead of moving.
    public bool TryMoveToTableId(string tableId)
    {
        if (armController == null || slotsA == null || slotsB == null)
        {
            Debug.LogWarning("[ASRS] Arm controller or slots not ready.");
            return false;
        }

        if (!int.TryParse(tableId, out int id))
        {
            Debug.LogWarning($"[ASRS] '{tableId}' is not a valid Table ID.");
            return false;
        }

        int row = id / 10000;
        int col = id % 10000;
        int totalRows = Rows * 2;

        if (row < 1 || row > totalRows || col < 1 || col > Cols)
        {
            Debug.LogWarning($"[ASRS] Table ID {tableId} is out of range (row 1-{totalRows}, col 1-{Cols}).");
            return false;
        }

        bool wantsB = row > Rows;
        int localRow = wantsB ? row - Rows - 1 : row - 1;
        int localCol = col - 1;
        int localIndex = localRow * Cols + localCol;

        if (!TryComputeSlotDelta(wantsB, localIndex, out float targetZ, out float targetY))
            return false;

        StopAutoTraverse();
        currentSlotIndex = (wantsB ? SlotsPerSide : 0) + localIndex;

        Debug.Log($"[ASRS] Go to {tableId} → Z={targetZ:F2} Y={targetY:F2}");

        ASRSArmController.Side targetSide = wantsB ? ASRSArmController.Side.U : ASRSArmController.Side.NonU;

        // Every GP-style commanded move returns to Safe Position first —
        // Z/Y/X retracted to 0, rotated to whichever side counts as "safe"
        // — before proceeding with the actual requested move, rather than
        // jumping straight there from wherever the arm happens to be
        // sitting.
        armController.ReturnToSafePosition(() =>
        {
            if (armController.CurrentSide != targetSide)
                armController.RotateY(targetSide == ASRSArmController.Side.U ? 180f : 0f);

            MoveToZYViaCenter(targetZ, targetY);
            armController.MoveX(parkedX);
        });

        return true;
    }

    // Center-crossing movement rule: every commanded Z/Y position change
    // (a SCORBASE-style "GP"/Go-to-Position command, not a manual jog nudge)
    // returns to the center — Z=0, Y=0, the same home-relative origin every
    // slot delta is already measured from — before proceeding to its actual
    // destination, matching the real SCORBASE/SmartCIM convention of
    // Current Position -> Center -> Next Position rather than jumping
    // directly between two arbitrary positions.
    public void MoveToZYViaCenter(float targetZ, float targetY)
    {
        StartCoroutine(MoveToZYViaCenterRoutine(targetZ, targetY));
    }

    private IEnumerator MoveToZYViaCenterRoutine(float targetZ, float targetY)
    {
        armController.MoveZ(0f);
        armController.MoveY(0f);
        yield return new WaitUntil(() => !armController.IsMoving);

        armController.MoveZ(targetZ);
        armController.MoveY(targetY);
    }

    private void StartAutoTraverse()
    {
        isAutoTraversing = true;
        currentSlotIndex = 0;
        traverseCoroutine = StartCoroutine(AutoTraverseRoutine());
        Debug.Log("[ASRS] Auto-traverse started — press T or Space to cancel.");
    }

    private void StopAutoTraverse()
    {
        if (traverseCoroutine != null)
        {
            StopCoroutine(traverseCoroutine);
            traverseCoroutine = null;
        }

        if (isAutoTraversing)
        {
            isAutoTraversing = false;
            Debug.Log("[ASRS] Auto-traverse cancelled.");
        }
    }

    private IEnumerator AutoTraverseRoutine()
    {
        for (int i = 0; i < TotalSlots; i++)
        {
            currentSlotIndex = i;
            MoveToCurrentSlot();

            yield return new WaitUntil(() => !armController.IsMoving);
            yield return new WaitForSeconds(autoTraverseDelay);
        }

        isAutoTraversing = false;
        Debug.Log("[ASRS] Auto-traverse complete — all 72 slots visited.");
    }
}
