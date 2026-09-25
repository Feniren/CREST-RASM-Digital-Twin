using System;
using System.Collections;
using UnityEngine;

// Physically carries a slotted table with the arm's gripper (RackHand,
// parented under ArmX inside the ASRS prefab) instead of teleporting it.
// Wired for ASRSSlot as a source, delivering either to the ConveyorBelt or
// to another ASRSSlot (rack-internal relocation, no conveyor needed — used
// for testing in scenes that don't have one). Reuses the arm's existing
// Z/Y/X move primitives (ASRSArmController) and slot-delta math
// (ASRSArmTester) rather than any inverse kinematics — the "grab" is a
// straightforward reparent onto RackHand, the same technique
// Item_Slotted_Table.SetItem() already uses to parent an Item onto its
// AnchorPoint.
public class ASRS_Gripper_Controller : MonoBehaviour
{
    // The physical locations a pick-and-place operation can name as a
    // source or target. ASRSSlot -> ConveyorBelt and ASRSSlot -> ASRSSlot
    // are implemented; every other combination (Workstation, or a source
    // other than ASRSSlot) is rejected with a clear "not yet supported"
    // warning rather than silently doing the wrong thing. Matches the
    // SCORBASE panel's Source/Target dropdown options.
    public enum PickPlaceLocation { ASRSSlot, ConveyorBelt, Workstation }

    [Header("Arm")]
    [SerializeField] private ASRSArmController armController;
    [SerializeField] private ASRSArmTester armTester;
    [SerializeField] private Transform rackHand;

    [Header("Pick and Place")]
    [Tooltip("Required for ManualPickAndPlace — the rack a Source Index (ASRS Slot) is retrieved from.")]
    [SerializeField] private Item_ASRS rack;
    [Tooltip("Required for ManualPickAndPlace — paused for the duration of the operation, then handed the retrieved table once it arrives.")]
    [SerializeField] private Item_Conveyor_Belt conveyorBelt;
    [Tooltip("Optional — queried for whether a pallet is currently in the scanner zone (so this doesn't deliver on top of one already there) and used as the physical drop point. This is a read-only reference — the sensor itself no longer owns or triggers pick-and-place, this controller does.")]
    [SerializeField] private Item_RFID_Sensor_ASRS rfidSensor;

    [Header("Reach")]
    [Tooltip("Local X (depth) extended in far enough to actually grab/release a table — deeper than ASRSArmTester's Parked X. Tune to your rig; too shallow misses the table, too deep drives it into the shelf.")]
    [SerializeField] private float reachDepth = 0.15f;

    [Header("Carry Speed")]
    [Tooltip("ASRSArmController.MoveSpeed is temporarily set to this for the whole carry sequence (grab, carry, release, return home), then restored to whatever it was before — so this demo plays out slower/more deliberately without affecting jog, Search Home, or Go-to-slot speed elsewhere.")]
    [SerializeField] private float carrySpeed = 0.15f;

    public bool IsBusy { get; private set; }

    // Separate from IsBusy (which only covers the physical carry motion
    // inside RetrieveRoutine) — this also covers the pallet-presence wait
    // and rack lookup that happen before the carry even starts, so a second
    // ManualPickAndPlace call can't slip in during that window.
    private bool pickAndPlaceRunning;

    // SCORBASE's manual Pick and Place OK button calls this directly — the
    // arm's own controller is in charge of running a pick-and-place, not
    // the RFID sensor. Only sourceLocation=ASRSSlot, targetLocation=
    // ConveyorBelt is implemented; everything else logs a warning and
    // returns false. sourceIndex/targetIndex are the same plain 1-72 slot
    // numbers ASRSArmTester.IndexToTableId already converts.
    public bool ManualPickAndPlace(int sourceIndex, int targetIndex,
        PickPlaceLocation sourceLocation = PickPlaceLocation.ASRSSlot,
        PickPlaceLocation targetLocation = PickPlaceLocation.ConveyorBelt)
    {
        if (pickAndPlaceRunning || IsBusy)
            return false;

        if (sourceLocation != PickPlaceLocation.ASRSSlot)
        {
            Debug.LogWarning($"[ASRS_Gripper_Controller] {sourceLocation} -> {targetLocation} isn't implemented yet — only ASRSSlot is currently supported as a source.", this);
            return false;
        }

        if (rack == null)
        {
            Debug.LogError("[ASRS_Gripper_Controller] Rack reference is not assigned — can't run pick and place.", this);
            return false;
        }

        string sourceTableId = ASRSArmTester.IndexToTableId(sourceIndex);

        if (targetLocation == PickPlaceLocation.ConveyorBelt)
        {
            if (conveyorBelt == null)
            {
                Debug.LogError("[ASRS_Gripper_Controller] Conveyor Belt reference is not assigned — can't run pick and place.", this);
                return false;
            }

            pickAndPlaceRunning = true;
            conveyorBelt.PauseMovement();
            StartCoroutine(ManualPickAndPlaceRoutine(sourceTableId));
            return true;
        }

        if (targetLocation == PickPlaceLocation.ASRSSlot)
        {
            string targetTableId = ASRSArmTester.IndexToTableId(targetIndex);

            if (!int.TryParse(targetTableId, out int targetId))
            {
                Debug.LogWarning($"[ASRS_Gripper_Controller] '{targetTableId}' is not a valid destination Table ID.", this);
                return false;
            }

            if (rack.IsSlotOccupied(targetId))
            {
                Debug.LogWarning($"[ASRS_Gripper_Controller] Target slot {targetTableId} is already occupied — pick a different Target Index.", this);
                return false;
            }

            pickAndPlaceRunning = true;
            StartCoroutine(ManualSlotToSlotRoutine(sourceTableId, targetId));
            return true;
        }

        Debug.LogWarning($"[ASRS_Gripper_Controller] {sourceLocation} -> {targetLocation} isn't implemented yet — only ASRSSlot -> ConveyorBelt or ASRSSlot -> ASRSSlot are currently supported.", this);
        return false;
    }

    // ASRS Slot -> ASRS Slot: retrieves the source table and carries it
    // directly to another rack slot's anchor — no conveyor involved at
    // all. Reuses the exact same low-level carry (RetrieveToPoint) as the
    // conveyor path; only the drop point and what happens on arrival
    // (re-seat into the rack instead of handing off to a belt) differ.
    private IEnumerator ManualSlotToSlotRoutine(string sourceTableId, int targetTableId)
    {
        Item_Slotted_Table retrieved = rack.RetrieveByID(sourceTableId);

        if (retrieved == null)
        {
            Debug.LogWarning($"[ASRS_Gripper_Controller] No stored table found for '{sourceTableId}'.", this);
            pickAndPlaceRunning = false;
            yield break;
        }

        if (!rack.TryGetSlotAnchor(targetTableId, out Vector3 dropPosition, out Quaternion dropRotation))
        {
            Debug.LogWarning($"[ASRS_Gripper_Controller] Could not resolve an anchor for target slot {targetTableId} — returning '{sourceTableId}' to its own slot instead.", this);

            if (int.TryParse(sourceTableId, out int sourceId))
                rack.PlaceAtSlot(retrieved, sourceId);

            pickAndPlaceRunning = false;
            yield break;
        }

        Spline_Animate retrievedSpline = retrieved.GetComponent<Spline_Animate>();
        if (retrievedSpline == null)
            retrievedSpline = retrieved.GetComponentInParent<Spline_Animate>();
        if (retrievedSpline != null)
            retrievedSpline.enabled = false;

        bool started = RetrieveToPoint(retrieved, dropPosition, dropRotation, () =>
        {
            if (retrievedSpline != null)
                retrievedSpline.enabled = true;

            rack.PlaceAtSlot(retrieved, targetTableId);
            retrieved.task = RACK_TASK.NONE;
            pickAndPlaceRunning = false;
        });

        if (!started)
        {
            // Shouldn't normally happen — pickAndPlaceRunning/IsBusy were
            // already checked before this coroutine started — but fall
            // back to instant placement so the operation still completes.
            retrieved.transform.position = dropPosition;
            retrieved.transform.rotation = dropRotation;
            if (retrievedSpline != null)
                retrievedSpline.enabled = true;

            rack.PlaceAtSlot(retrieved, targetTableId);
            retrieved.task = RACK_TASK.NONE;
            pickAndPlaceRunning = false;
        }
    }

    private IEnumerator ManualPickAndPlaceRoutine(string tableId)
    {
        // Pallet-presence check — don't deliver into the scanner/conveyor
        // zone while another table is already sitting there.
        if (rfidSensor != null)
            yield return new WaitUntil(() => !rfidSensor.HasPalletPresent);

        Item_Slotted_Table retrieved = rack.RetrieveByID(tableId);

        if (retrieved == null)
        {
            Debug.LogWarning($"[ASRS_Gripper_Controller] No stored table found for '{tableId}'.", this);
            conveyorBelt.ResumeMovement();
            pickAndPlaceRunning = false;
            yield break;
        }

        Vector3 dropPosition = rfidSensor != null ? rfidSensor.transform.position : transform.position;
        Quaternion dropRotation = rfidSensor != null ? rfidSensor.transform.rotation : transform.rotation;

        Spline_Animate retrievedSpline = retrieved.GetComponent<Spline_Animate>();
        if (retrievedSpline == null)
            retrievedSpline = retrieved.GetComponentInParent<Spline_Animate>();
        if (retrievedSpline != null)
            retrievedSpline.enabled = false;

        bool started = RetrieveToPoint(retrieved, dropPosition, dropRotation, () =>
        {
            if (retrievedSpline != null)
                retrievedSpline.enabled = true;

            conveyorBelt.AddPlate(retrieved.gameObject, 0f);
            retrieved.task = RACK_TASK.NONE;
            conveyorBelt.ResumeMovement();
            pickAndPlaceRunning = false;
        });

        if (!started)
        {
            // Shouldn't normally happen — pickAndPlaceRunning/IsBusy were
            // already checked before this coroutine started — but fall back
            // to instant placement so the operation still completes.
            retrieved.transform.position = dropPosition;
            retrieved.transform.rotation = dropRotation;
            if (retrievedSpline != null)
                retrievedSpline.enabled = true;

            conveyorBelt.AddPlate(retrieved.gameObject, 0f);
            retrieved.task = RACK_TASK.NONE;
            conveyorBelt.ResumeMovement();
            pickAndPlaceRunning = false;
        }
    }

    // Grabs 'table' from its current (rack) position and carries it to
    // dropWorldPosition/dropWorldRotation, invoking onArrived once it's been
    // placed there (e.g. to hand it off to the conveyor), then returns to a
    // parked pose. Returns false immediately — does nothing — if already
    // busy or a required reference is missing.
    public bool RetrieveToPoint(Item_Slotted_Table table, Vector3 dropWorldPosition, Quaternion dropWorldRotation, Action onArrived)
    {
        if (IsBusy || table == null)
            return false;

        if (armController == null || armTester == null || rackHand == null)
        {
            Debug.LogError("[ASRS_Gripper_Controller] Missing armController/armTester/rackHand reference.", this);
            return false;
        }

        StartCoroutine(RetrieveRoutine(table, dropWorldPosition, dropWorldRotation, onArrived));
        return true;
    }

    private IEnumerator RetrieveRoutine(Item_Slotted_Table table, Vector3 dropWorldPosition, Quaternion dropWorldRotation, Action onArrived)
    {
        IsBusy = true;

        float originalSpeed = armController.MoveSpeed;
        armController.MoveSpeed = carrySpeed;

        // 0. Rotate to face this slot's side first — nothing else is
        // driving this automatically the way a human pressing a panel
        // button would.
        if (int.TryParse(table.TableID, out int id) && !armController.IsReachable(id))
        {
            int row = id / 10000;
            armController.RotateY(row >= 7 ? 180f : 0f);
            yield return WaitUntilIdle();
        }

        // 1. Align Z/Y with the table's rack slot, parked (not reaching in
        // yet) — reuses the exact math the Go-to-slot panel command uses.
        armTester.TryMoveToTableId(table.TableID);
        yield return WaitUntilIdle();

        // 2. Reach in and grab.
        armController.MoveX(reachDepth);
        yield return WaitUntilIdle();

        AttachToGripper(table);

        // 3. Pull back out, carrying the table with it — it's parented to
        // RackHand now, which is a child of ArmX.
        armController.MoveX(armTester.ParkedX);
        yield return WaitUntilIdle();

        // 4. Move Z/Y to the drop point — via center, per the center-crossing
        // movement rule (every commanded position change passes through
        // center before its actual destination).
        if (armTester.TryComputeDeltaToWorldPoint(dropWorldPosition, out float dropZ, out float dropY))
        {
            armTester.MoveToZYViaCenter(dropZ, dropY);
            yield return WaitUntilIdle();
        }
        else
        {
            Debug.LogWarning($"[ASRS_Gripper_Controller] Could not compute a delta to the drop point for '{table.TableID}' — releasing at the current position instead.", this);
        }

        // 5. Reach in, set the table down, release, then pull back out.
        armController.MoveX(reachDepth);
        yield return WaitUntilIdle();

        DetachFromGripper(table, dropWorldPosition, dropWorldRotation);
        onArrived?.Invoke();

        armController.MoveX(armTester.ParkedX);
        yield return WaitUntilIdle();

        // 6. Return to Safe Position — the arm's resting state after any
        // pick-and-place, not just a Z/Y=0 return.
        bool safeReached = false;
        armController.ReturnToSafePosition(() => safeReached = true);
        yield return new WaitUntil(() => safeReached);

        armController.MoveSpeed = originalSpeed;
        IsBusy = false;
    }

    private IEnumerator WaitUntilIdle()
    {
        yield return new WaitUntil(() => !armController.IsMoving);
    }

    private void AttachToGripper(Item_Slotted_Table table)
    {
        Rigidbody rb = table.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        table.transform.SetParent(rackHand, true);
    }

    private void DetachFromGripper(Item_Slotted_Table table, Vector3 worldPosition, Quaternion worldRotation)
    {
        table.transform.SetParent(null, true);
        table.transform.SetPositionAndRotation(worldPosition, worldRotation);
    }
}
