using System.Collections;
using UnityEngine;
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

    [Tooltip("Index (within whichever side's slots) of the slot the arm physically sits at when all axes are at local 0. " +
             "Default 15 = the center of a 6×6 grid.")]
    [SerializeField] private int centerSlotIndex = 15;
    [Tooltip("Additional world-space nudge applied to every slot position. Fine-tune if the slot pivot is not exactly where the arm should align.")]
    [SerializeField] private Vector3 slotOffset = Vector3.zero;
    [Tooltip("Local X when the arm is parked in front of a slot (not extended). X is the depth axis.")]
    [SerializeField] private float parkedX = 0f;
    [Tooltip("Seconds the arm pauses at each slot during auto-traverse before moving on.")]
    [SerializeField] private float autoTraverseDelay = 0.5f;

    private Transform[] slotsA;
    private Transform[] slotsB;

    private const int Rows = 6;
    private const int Cols = 6;
    private const int SlotsPerSide = Rows * Cols; // 36
    private const int TotalSlots = SlotsPerSide * 2; // 72

    private int currentSlotIndex = 0;
    private bool isAutoTraversing = false;
    private Coroutine traverseCoroutine;

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

        Debug.Log($"[ASRS] Loaded {slotsA.Length} SlotsA + {slotsB.Length} SlotsB slots. Tracking starts at index {currentSlotIndex}.");
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

    private bool SameSide(int indexA, int indexB) =>
        (indexA < SlotsPerSide) == (indexB < SlotsPerSide);

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

        Transform center = slots[centerSlotIndex];
        if (center == null)
        {
            Debug.LogWarning("[ASRS] Center slot transform is null.");
            return false;
        }

        // World-space vector from this side's center slot to the target slot.
        // InverseTransformDirection maps direction (no translation) into local space,
        // giving the correct per-axis delta regardless of parent position.
        Vector3 worldDelta = slot.position + slotOffset - center.position;

        Transform zParent = armController.ArmZ != null ? armController.ArmZ.parent : null;
        Transform yParent = armController.ArmY != null ? armController.ArmY.parent : null;

        targetZ = zParent != null ? zParent.InverseTransformDirection(worldDelta).z : worldDelta.z;
        targetY = yParent != null ? yParent.InverseTransformDirection(worldDelta).y : worldDelta.y;
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

        if (!armController.IsReachable(id))
        {
            Debug.LogWarning($"[ASRS] Table ID {tableId} is on the far side — rotate the arm first.");
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

        armController.MoveZ(targetZ);
        armController.MoveY(targetY);
        armController.MoveX(parkedX);
        return true;
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
