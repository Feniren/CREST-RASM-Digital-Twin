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
    // SlotsB side: 070001–120006 (rows 7-12, cols 1-6, 36 slots).
    // Assign the SlotsB empty GameObject to slotsContainer. Its children must
    // be ordered left→right, bottom→top in the Hierarchy (070001 first, 120006 last).

    [Header("Slot Grid — SlotsB")]
    [Tooltip("Parent GameObject whose children are the 36 SlotsB Transforms, ordered left→right bottom→top.")]
    [SerializeField] private Transform slotsContainer;
    [Tooltip("Index (in the slotsContainer children) of the slot the arm physically sits at when all axes are at local 0. " +
             "Default 15 = slot 090004, the center of a 6×6 grid.")]
    [SerializeField] private int centerSlotIndex = 15;
    [Tooltip("Additional world-space nudge applied to every slot position. Fine-tune if the slot pivot is not exactly where the arm should align.")]
    [SerializeField] private Vector3 slotOffset = Vector3.zero;
    [Tooltip("Local X when the arm is parked in front of a slot (not extended). X is the depth axis.")]
    [SerializeField] private float parkedX = 0f;
    [Tooltip("Seconds the arm pauses at each slot during auto-traverse before moving on.")]
    [SerializeField] private float autoTraverseDelay = 0.5f;

    private Transform[] slotTransforms;

    private const int Rows = 6;
    private const int Cols = 6;
    private const int TotalSlots = Rows * Cols; // 36

    private int currentSlotIndex = 0;
    private bool isAutoTraversing = false;
    private Coroutine traverseCoroutine;

    private void Awake()
    {
        if (slotsContainer == null)
        {
            Debug.LogError("[ASRS] SlotsContainer is not assigned.", this);
            return;
        }

        slotTransforms = new Transform[slotsContainer.childCount];
        for (int i = 0; i < slotsContainer.childCount; i++)
            slotTransforms[i] = slotsContainer.GetChild(i);

        Debug.Log($"[ASRS] Loaded {slotTransforms.Length} slots from '{slotsContainer.name}'.");
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
        // Right Arrow  — advance one slot (left→right, bottom→top)
        // Left Arrow   — retreat one slot
        // Up Arrow     — move up one row (same column)
        // Down Arrow   — move down one row (same column)
        // H            — jump to home slot 070001 (bottom-left of this side)
        // T            — toggle automatic full-rack traversal

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
            if (next < TotalSlots)
            {
                currentSlotIndex = next;
                MoveToCurrentSlot();
            }
            else
                Debug.Log("[ASRS] Already at the top row.");
        }

        if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            StopAutoTraverse();
            int next = currentSlotIndex - Cols;
            if (next >= 0)
            {
                currentSlotIndex = next;
                MoveToCurrentSlot();
            }
            else
                Debug.Log("[ASRS] Already at the bottom row.");
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

    // Moves the arm to the current slot using a delta from the center slot (090004).
    // The arm sits at local (0,0,0) when at the center, so the target local value
    // for each axis equals the delta from center converted to that axis's local space.
    private void MoveToCurrentSlot()
    {
        if (slotTransforms == null || currentSlotIndex >= slotTransforms.Length)
        {
            Debug.LogWarning("[ASRS] Slot transforms not ready.");
            return;
        }

        Transform slot = slotTransforms[currentSlotIndex];
        if (slot == null)
        {
            Debug.LogWarning($"[ASRS] Slot {currentSlotIndex} has no Transform.");
            return;
        }

        Transform center = slotTransforms[centerSlotIndex];
        if (center == null)
        {
            Debug.LogWarning("[ASRS] Center slot transform is null.");
            return;
        }

        // World-space vector from the center slot to the target slot.
        // InverseTransformDirection maps direction (no translation) into local space,
        // giving the correct per-axis delta regardless of parent position.
        Vector3 worldDelta = slot.position + slotOffset - center.position;

        Transform zParent = armController.ArmZ != null ? armController.ArmZ.parent : null;
        Transform yParent = armController.ArmY != null ? armController.ArmY.parent : null;

        float targetZ = zParent != null ? zParent.InverseTransformDirection(worldDelta).z : worldDelta.z;
        float targetY = yParent != null ? yParent.InverseTransformDirection(worldDelta).y : worldDelta.y;

        int row = currentSlotIndex / Cols;
        int col = currentSlotIndex % Cols;
        int displayRow = row + 7;
        string tableId = (displayRow * 10000 + col + 1).ToString("D6");

        Debug.Log($"[ASRS] Slot {currentSlotIndex + 1}/{TotalSlots} → {tableId}" +
                  $"  (Row {displayRow}, Col {col + 1})" +
                  $"  Z={targetZ:F2}  Y={targetY:F2}");

        armController.MoveZ(targetZ);
        armController.MoveY(targetY);
        armController.MoveX(parkedX);
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
        int slotCount = TotalSlots;

        for (int i = 0; i < slotCount; i++)
        {
            currentSlotIndex = i;
            MoveToCurrentSlot();

            yield return new WaitUntil(() => !armController.IsMoving);
            yield return new WaitForSeconds(autoTraverseDelay);
        }

        isAutoTraversing = false;
        Debug.Log("[ASRS] Auto-traverse complete — all slots visited.");
    }
}
