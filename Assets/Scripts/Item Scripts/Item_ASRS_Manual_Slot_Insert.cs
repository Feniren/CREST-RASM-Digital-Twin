using UnityEngine;

// Manual "snap to place" for putting a physically-carried Item_Slotted_Table
// into its own home rack slot — the trainee-driven counterpart to the
// automatic RFID-triggered insert. Mirrors the exact pattern
// Item_Plate/Item_Slotted_Table already use for snapping a dropped item onto
// their own AnchorPoint (OnTriggerEnter -> only act if the incoming object is
// Pickup-flagged and not currently held), just targeting the rack's own
// slot system (Item_ASRS.SlotInsert) instead of a local Item/AnchorPoint
// pair.
//
// Put this on the same GameObject as the rack's own hidden "vacant slot"
// seed table (the one Item_ASRS.Start() hides via SetTableVisibility(false)
// for whichever row is marked vacant) — that object already sits exactly at
// the slot's anchor position and keeps its Collider active even while
// hidden, so no new trigger volume needs to be built.
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Item_Slotted_Table))]
public class Item_ASRS_Manual_Slot_Insert : MonoBehaviour
{
    [SerializeField] private Item_ASRS rack;

    [Header("Lesson (optional)")]
    [Tooltip("Leave empty to use this standalone with no lesson gating.")]
    [SerializeField] private SequenceManager sequenceManager;
    [SerializeField] private string notifyActionId = "LoadASRS";

    // The rack's own hidden seed table for this exact slot — lives on this
    // same GameObject (Item_ASRS hides it via SetTableVisibility(false) but
    // leaves it, and its Collider, active). Its baked TableID is this slot's
    // real address.
    private Item_Slotted_Table expectedSlot;

    // This slot has taken its table — later trigger contacts (the stored
    // table now sits inside this trigger for good) must not re-insert or
    // re-notify.
    private bool inserted;

    // Last table each one-shot message was logged for, so OnTriggerStay
    // (every physics step) can't flood the Console.
    private Item_Slotted_Table lastMismatchLogged;
    private Item_Slotted_Table lastTooEarlyLogged;

    private void Awake()
    {
        expectedSlot = GetComponent<Item_Slotted_Table>();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryInsert(other);
    }

    // Enter alone isn't enough: Entity_XR_Hand parents a carried table to the
    // hand, so it's still "held" when it first touches this trigger and is
    // skipped below. If the trainee then lets go INSIDE the slot, no new
    // Enter ever fires — Stay catches that release.
    private void OnTriggerStay(Collider other)
    {
        TryInsert(other);
    }

    // A correct table parked in the slot before the lesson reached the
    // LoadASRS step. Re-checked here until that step arrives (or the table
    // leaves), rather than relying on a possibly-sleeping body to keep
    // sending OnTriggerStay.
    private Item_Slotted_Table pendingTable;

    private void FixedUpdate()
    {
        if (pendingTable == null || inserted)
            return;

        Collider trigger = GetComponent<Collider>();
        if (trigger == null || !trigger.bounds.Contains(pendingTable.transform.position))
        {
            pendingTable = null;
            return;
        }

        if (sequenceManager == null || sequenceManager.HasReachedStep(notifyActionId))
            TryInsertTable(pendingTable);
    }

    private void TryInsert(Collider other)
    {
        if (inserted || rack == null || expectedSlot == null)
            return;

        TryInsertTable(other.GetComponentInParent<Item_Slotted_Table>());
    }

    private void TryInsertTable(Item_Slotted_Table table)
    {
        if (inserted || rack == null || expectedSlot == null)
            return;

        if (table == null || table == expectedSlot)
            return;

        // The seed slot is itself an Item_Slotted_Table, and its own
        // OnTriggerEnter (earlier on this GameObject, so it runs first)
        // grabs any dropped Pickup item — including a carried table — as if
        // it were a part, parenting it to its AnchorPoint. Hand it back
        // first so the real rack insert below can take it.
        if (expectedSlot.Item == table.gameObject)
        {
            expectedSlot.UnSetItem();
            table.transform.SetParent(null, true);
        }

        // Only a genuinely dropped/carried-then-released table should snap
        // in — not one still parented to a hand (mid-carry) or to some other
        // holder.
        if (table.transform.parent != null)
            return;

        // Double-check: the table being dropped in has to actually be THIS
        // slot's own table (matching TableID) — not some other slotted
        // table that just happens to have an empty home elsewhere in the
        // rack. Item_ASRS.SlotInsert/NeedsRackReturn key off the table's
        // own baked TableID, not physical trigger position, so without this
        // check a mismatched table would silently snap away to its own
        // correct slot instead of seating here, which would look like it
        // teleported instead of being rejected.
        if (string.IsNullOrWhiteSpace(table.TableID) || table.TableID != expectedSlot.TableID)
        {
            if (lastMismatchLogged != table)
            {
                lastMismatchLogged = table;
                Debug.Log($"[ASRS][Module1][Rack] Slot {expectedSlot.TableID}: rejected table '{table.TableID}' — it belongs in its own slot, not this one.", this);
            }
            return;
        }

        // Inserting before the lesson asks for it would leave the
        // LoadASRS step with nothing left to do — it would wait forever.
        if (sequenceManager != null && !sequenceManager.HasReachedStep(notifyActionId))
        {
            pendingTable = table;
            if (lastTooEarlyLogged != table)
            {
                lastTooEarlyLogged = table;
                Debug.Log($"[ASRS][Module1][Rack] Slot {expectedSlot.TableID}: table arrived before step '{notifyActionId}' — it will be inserted once that step starts.", this);
            }
            return;
        }

        pendingTable = null;

        // Not this table's own slot, or that slot's already filled — do
        // nothing rather than guess.
        if (!rack.NeedsRackReturn(table))
        {
            Debug.LogWarning($"[ASRS][Module1][Rack] Slot {expectedSlot.TableID}: rack reports the slot already occupied — not inserting.", this);
            inserted = true;
            return;
        }

        rack.SlotInsert(table);

        // Only the rack's own inventory decides success — not merely having
        // touched this trigger.
        if (!IsStoredHere(table))
        {
            Debug.LogWarning($"[ASRS][Module1][Rack] Slot {expectedSlot.TableID}: SlotInsert did not record table '{table.TableID}' in TableMap — step not completed.", this);
            return;
        }

        inserted = true;

        // SlotInsert positions the table but leaves its physics alone — a
        // just-dropped table would otherwise fall straight out of the slot.
        if (table.TryGetComponent(out Rigidbody body))
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
        }

        Debug.Log($"[ASRS][Module1][Rack] Slot {expectedSlot.TableID}: table inserted and confirmed in rack inventory.", this);

        if (sequenceManager != null)
            sequenceManager.NotifyAction(notifyActionId);
    }

    private bool IsStoredHere(Item_Slotted_Table table)
    {
        if (!int.TryParse(table.TableID, out int rawId))
            return false;

        try
        {
            return rack.TableMap.TryGetValue(rack.GetIndex(rawId), out Item_Slotted_Table stored) && stored == table;
        }
        catch (System.ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    public string NotifyActionId => notifyActionId;
    public Item_ASRS Rack => rack;
    public SequenceManager LessonSequence => sequenceManager;
}
