using UnityEngine;

// One selectable storage cell, created at runtime by ASRS_Rack_Cell_Inspector
// at the rack's own saved slot anchor. Just a trigger box that identifies
// the cell: the inspector's own pointer finds it along the hand ray (looking
// through the rack's glass/frame colliders), so it doesn't depend on being
// the first thing the player's interact ray hits.
public class ASRS_Rack_Cell_Target : MonoBehaviour
{
    public string TableId { get; private set; }
    public ASRS_Rack_Cell_Inspector Inspector { get; private set; }

    public void Init(ASRS_Rack_Cell_Inspector owner, string tableId)
    {
        Inspector = owner;
        TableId = tableId;
    }
}
