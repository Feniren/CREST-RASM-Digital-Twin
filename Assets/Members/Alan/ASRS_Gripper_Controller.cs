using System;
using System.Collections;
using UnityEngine;

// Physically carries a slotted table with the arm's gripper (RackHand,
// parented under ArmX inside the ASRS prefab) instead of teleporting it.
// Currently wired for the retrieve direction only (rack shelf -> conveyor).
// Reuses the arm's existing Z/Y/X move primitives (ASRSArmController) and
// slot-delta math (ASRSArmTester) rather than any inverse kinematics — the
// "grab" is a straightforward reparent onto RackHand, the same technique
// Item_Slotted_Table.SetItem() already uses to parent an Item onto its
// AnchorPoint.
public class ASRS_Gripper_Controller : MonoBehaviour
{
    [Header("Arm")]
    [SerializeField] private ASRSArmController armController;
    [SerializeField] private ASRSArmTester armTester;
    [SerializeField] private Transform rackHand;

    [Header("Reach")]
    [Tooltip("Local X (depth) extended in far enough to actually grab/release a table — deeper than ASRSArmTester's Parked X. Tune to your rig; too shallow misses the table, too deep drives it into the shelf.")]
    [SerializeField] private float reachDepth = 0.3f;

    [Header("Carry Speed")]
    [Tooltip("ASRSArmController.MoveSpeed is temporarily set to this for the whole carry sequence (grab, carry, release, return home), then restored to whatever it was before — so this demo plays out slower/more deliberately without affecting jog, Search Home, or Go-to-slot speed elsewhere.")]
    [SerializeField] private float carrySpeed = 0.15f;

    public bool IsBusy { get; private set; }

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

        // 4. Move Z/Y to the drop point.
        if (armTester.TryComputeDeltaToWorldPoint(dropWorldPosition, out float dropZ, out float dropY))
        {
            armController.MoveZ(dropZ);
            armController.MoveY(dropY);
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

        // 6. Return home.
        armController.MoveZ(0f);
        armController.MoveY(0f);
        yield return WaitUntilIdle();

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
