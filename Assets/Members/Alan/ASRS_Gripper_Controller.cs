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

    // Why the last ManualPickAndPlace call was refused (empty when it was
    // accepted) — shown on the SCORBASE panel's error line.
    public string LastError { get; private set; } = string.Empty;

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
        LastError = string.Empty;

        if (pickAndPlaceRunning || IsBusy)
            return Refuse("The arm is still busy with the last pick and place — wait for it to finish.");

        if (sourceLocation != PickPlaceLocation.ASRSSlot)
            return Refuse($"Source '{sourceLocation}' isn't supported yet — set Source to ASRS Slot.");

        if (rack == null)
        {
            Debug.LogError("[ASRS_Gripper_Controller] Rack reference is not assigned — can't run pick and place.", this);
            return Refuse("The rack isn't connected (see the Console).");
        }

        string sourceTableId = ASRSArmTester.IndexToTableId(sourceIndex);

        // Checked now, not halfway through the routine, so an empty slot is
        // refused on the panel instead of the OK silently doing nothing.
        if (int.TryParse(sourceTableId, out int sourceId) && !rack.IsSlotOccupied(sourceId))
            return Refuse($"Source slot {sourceIndex} ({sourceTableId}) is empty — pick a slot that holds a table.");

        if (targetLocation == PickPlaceLocation.ConveyorBelt)
        {
            if (conveyorBelt == null)
            {
                Debug.LogError("[ASRS_Gripper_Controller] Conveyor Belt reference is not assigned — can't run pick and place.", this);
                return Refuse("The conveyor isn't connected (see the Console).");
            }

            pickAndPlaceRunning = true;
            StartCoroutine(ManualPickAndPlaceRoutine(sourceTableId));
            return true;
        }

        if (targetLocation == PickPlaceLocation.ASRSSlot)
        {
            string targetTableId = ASRSArmTester.IndexToTableId(targetIndex);

            if (!int.TryParse(targetTableId, out int targetId))
                return Refuse($"Target Index {targetIndex} isn't a valid rack slot.");

            if (rack.IsSlotOccupied(targetId))
                return Refuse($"Target slot {targetIndex} ({targetTableId}) already holds a table — pick an empty slot (row 5, slots 25-30, starts empty).");

            pickAndPlaceRunning = true;
            StartCoroutine(ManualSlotToSlotRoutine(sourceTableId, targetId));
            return true;
        }

        return Refuse($"Target '{targetLocation}' isn't supported yet — set Target to ASRS Slot or Conveyor Belt.");
    }

    private bool Refuse(string reason)
    {
        LastError = reason;
        Debug.LogWarning($"[ASRS_Gripper_Controller] {reason}", this);
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

        CarryAndHandOff(retrieved, dropPosition, dropRotation, () => rack.PlaceAtSlot(retrieved, targetTableId), targetTableId);
    }

    private IEnumerator ManualPickAndPlaceRoutine(string tableId)
    {
        // Pallet-presence check — don't deliver into the scanner/conveyor
        // zone while another table is already sitting there. The belt keeps
        // running while we wait (pausing it first would leave that table
        // stuck in the zone forever), and is paused only once it's clear.
        if (rfidSensor != null)
            yield return new WaitUntil(() => !rfidSensor.HasPalletPresent);

        conveyorBelt.PauseMovement();

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

        CarryAndHandOff(retrieved, dropPosition, dropRotation, () =>
        {
            conveyorBelt.AddPlate(retrieved.gameObject, 0f);
            conveyorBelt.ResumeMovement();
        });
    }

    // Shared tail of both pick-and-place routines: hands the table's
    // movement from its spline to the gripper, carries it to the drop
    // point, then runs 'handOff' (re-seat in the rack, or give it to the
    // belt) and ends the pick-and-place.
    private void CarryAndHandOff(Item_Slotted_Table table, Vector3 dropPosition, Quaternion dropRotation, Action handOff, int dropTableId = 0)
    {
        Spline_Animate spline = Item_ASRS.GetSplineAnimate(table);
        if (spline != null)
            spline.enabled = false;

        void OnArrived()
        {
            if (spline != null)
                spline.enabled = true;

            handOff();
            table.task = RACK_TASK.NONE;
            pickAndPlaceRunning = false;
        }

        if (!RetrieveToPoint(table, dropPosition, dropRotation, OnArrived, dropTableId))
        {
            // Shouldn't normally happen — pickAndPlaceRunning/IsBusy were
            // already checked before the routine started — but fall back
            // to instant placement so the operation still completes.
            table.transform.SetPositionAndRotation(dropPosition, dropRotation);
            OnArrived();
        }
    }

    // Grabs 'table' from its current (rack) position and carries it to
    // dropWorldPosition/dropWorldRotation, invoking onArrived once it's been
    // placed there (e.g. to hand it off to the conveyor), then returns to a
    // parked pose. Returns false immediately — does nothing — if already
    // busy or a required reference is missing.
    // dropTableId: when the drop point is a rack slot (row*10000+col), the
    // arm turns to face that slot's side before placing — needed when the
    // table is carried from one side of the rack to the other.
    public bool RetrieveToPoint(Item_Slotted_Table table, Vector3 dropWorldPosition, Quaternion dropWorldRotation, Action onArrived, int dropTableId = 0)
    {
        if (IsBusy || table == null)
            return false;

        if (armController == null || armTester == null || rackHand == null)
        {
            Debug.LogError("[ASRS_Gripper_Controller] Missing armController/armTester/rackHand reference.", this);
            return false;
        }

        StartCoroutine(RetrieveRoutine(table, dropWorldPosition, dropWorldRotation, onArrived, dropTableId));
        return true;
    }

    private IEnumerator RetrieveRoutine(Item_Slotted_Table table, Vector3 dropWorldPosition, Quaternion dropWorldRotation, Action onArrived, int dropTableId)
    {
        IsBusy = true;

        float originalSpeed = armController.MoveSpeed;
        armController.MoveSpeed = carrySpeed;

        // 1. Align Z/Y with the table's rack slot, parked (not reaching in
        // yet) — reuses the exact math the Go-to-slot panel command uses,
        // which also turns the arm to face the slot's side (via Safe
        // Position), so no separate turn is needed first.
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

        // 3b. Dropping into a slot on the other side of the rack: back to
        // center, then turn (carrying the table) to face that side.
        if (dropTableId > 0 && !armController.IsReachable(dropTableId))
        {
            armController.MoveZ(0f);
            armController.MoveY(0f);
            yield return WaitUntilIdle();

            armController.RotateY(dropTableId / 10000 >= 7 ? 180f : 0f);
            yield return WaitUntilIdle();
        }

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
        // Wait a frame first so a move issued just now has registered, then
        // until the arm is still AND no multi-leg command is between legs.
        yield return null;
        yield return new WaitUntil(() => !armController.IsMoving && !armTester.IsCommandRunning);
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
