using UnityEngine;

// Keeps the parts-table items sitting on the parts table.
//
// Parts_Table_Items is a scene root of its own rather than a child of Parts_Table,
// because the table is a scale-100 FBX rotated X 270 and parenting under it would drag
// that onto every part. The cost of being a separate root was that moving the table left
// the parts floating where the table used to be, which is exactly what happened. This
// anchors the root to the table's top face instead, so the parts ride along without
// inheriting the table's transform.
//
// The arrangement is whatever the parts are currently in — this only slides the whole set
// as one, so parts stay where they were put relative to each other.
//
// It measures the parts by their rendered bounds rather than their transforms, because a
// Part_* root is nowhere near the part it owns: M1_Parts_Table_Swap bakes the mill's world
// coordinates into the clones and offsets the Model container to compensate, so a root can
// sit metres from the geometry hanging off it. Anchoring by transform silently drags the
// visible parts off the table.
//
// Nothing here touches the glide. Table_Part anchors its mill destination to the live
// mill node it was cloned from, so the destination is unaffected by where the table end
// of the journey happens to be.
[ExecuteAlways]
// Ahead of Table_Part, which caches the table end of its glide in Awake. That end is the
// Model's local pose, so today's cache would survive the anchor moving — the order is
// kept so a future cache of anything parent-relative doesn't quietly read a stale anchor.
[DefaultExecutionOrder(-100)]
public class Parts_Table_Layout : MonoBehaviour{
    // The renderer rather than the transform: the table's pivot is neither its centre nor
    // its surface, so its bounds are the only honest description of where the top is.
    [SerializeField] private Renderer Table;

    // Matches the container M1_Parts_Table_Swap builds under each Part_*.
    private const string ModelName = "Model";

    private void Awake(){
        Apply();
    }

#if UNITY_EDITOR
    // Driven off the editor loop rather than a MonoBehaviour Update, because edit-mode
    // Update is part of the player loop and only ticks while the editor is in the
    // foreground — an editor left in the background stops re-anchoring entirely, which
    // makes the whole thing look broken the moment anything drives Unity remotely.
    // At runtime Awake has already run, and the table does not move during play.
    private void OnEnable(){
        if (!Application.isPlaying)
            UnityEditor.EditorApplication.update += Apply;
    }

    private void OnDisable(){
        UnityEditor.EditorApplication.update -= Apply;
    }
#endif

    private void Apply(){
        if (Table == null)
            return;

        // Only the Model containers, which hold the cloned geometry. A Part_* root and its
        // Glow_Shell are placed by the rebuild tool and can be left metres from the clone
        // they belong to once anything moves without a re-run — measuring those instead of
        // the meshes is how you end up centring the table on nothing.
        bool any = false;
        Bounds cluster = new Bounds();

        foreach (Transform child in transform){
            Transform model = child.Find(ModelName);

            if (model == null)
                continue;

            // Active renderers only. A part mid-glide has its table copy switched off and
            // the real mill geometry switched on, and measuring that would drag every
            // other part across the room after it.
            foreach (Renderer part in model.GetComponentsInChildren<Renderer>()){
                if (any){
                    cluster.Encapsulate(part.bounds);
                }
                else{
                    cluster = part.bounds;
                    any = true;
                }
            }
        }

        if (!any)
            return;

        Bounds table = Table.bounds;

        // Centred across the top face and resting on it, rather than centred in the table's
        // volume — the parts stand on the surface, and the table's pivot is neither its
        // centre nor its top.
        Vector3 wanted = new Vector3(table.center.x, table.max.y, table.center.z);
        Vector3 actual = new Vector3(cluster.center.x, cluster.min.y, cluster.center.z);

        // Moved by the shortfall rather than assigned, because the thing being placed is
        // the geometry, not this transform. Converges in one step and then costs nothing:
        // once the set is centred the shortfall is zero, so the scene stops being dirtied.
        // Moved by the shortfall rather than assigned, because the thing being placed is
        // the geometry, not this transform. Converges in one step from wherever the set
        // happens to be.
        Vector3 shortfall = wanted - actual;

        // Ignoring sub-0.1 mm corrections keeps float noise in the bounds from writing a
        // new position on every editor tick, which would leave the scene permanently
        // dirty and prompting to save.
        if (shortfall.sqrMagnitude > 1e-10f)
            transform.position += shortfall;
    }
}
