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

    private void Awake()
    {
        expectedSlot = GetComponent<Item_Slotted_Table>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (rack == null || expectedSlot == null)
            return;

        Item_Slotted_Table table = other.GetComponentInParent<Item_Slotted_Table>();
        if (table == null || table == expectedSlot)
            return;

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
            return;

        // Not this table's own slot, or that slot's already filled — do
        // nothing rather than guess.
        if (!rack.NeedsRackReturn(table))
            return;

        rack.SlotInsert(table);

        if (sequenceManager != null)
            sequenceManager.NotifyAction(notifyActionId);
    }
}
