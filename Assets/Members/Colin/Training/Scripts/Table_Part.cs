using UnityEngine;

// A duplicated component sitting on the Module 1 parts table. Table_Part_Display
// toggles its glow shell to mark it as the part the trainee is currently
// identifying. Mirrors the glow handling in Component_Marker.
//
// Table_Part_Glide drives the guided-tour demonstration through Set_Glide: the copy
// slides between its slot on the table and the place it belongs on the mill.
public class Table_Part : MonoBehaviour{
    public string Part_Id;

    [SerializeField] private Renderer Glow;

    [Header("Glide (wired by Training/9 Parts Table - Rebuild From Mill)")]
    [SerializeField] private Transform Model;
    [SerializeField] private Transform[] Mill_Sources;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock block;

    private Vector3 tablePosition;
    private Quaternion tableRotation;
    private Vector3 tableScale;

    // Bake-staleness correction, in the first source's frame — see Awake.
    private Vector3 residual;

    // Parts with no clone on the table (guard door, door unlock, electronics cabinet)
    // just get the marker highlight — their steps wait on Continue like any other.
    public bool Can_Glide => Model != null;

    // 0 = on the table, 1 = landed on the mill. Lets a demonstration that is
    // interrupted mid-flight finish from where it actually is.
    public float Glide_T { get; private set; }

    private void Awake(){
        if (Model == null)
            return;

        tablePosition = Model.localPosition;
        tableRotation = Model.localRotation;
        tableScale = Model.localScale;

        Measure_Residual();
    }

    // The pivot-anchored solve in Mill_Pose lands the clone's ROOT on the source's root,
    // but the spindle nodes keep their pivots at the FBX origin while their child meshes
    // carry the offsets — and an FBX re-export can shift those internals without touching
    // the pivot, leaving a clone whose root lands perfectly while its meshes hang metres
    // away. So measure the truth once: snap the Model to the solved destination, compare
    // rendered bounds centres between the landed clone and the live sources, and keep the
    // shortfall in the source's frame so it rides any runtime motion. It is a constant —
    // stale bake data against live nodes — so once at load is enough.
    private void Measure_Residual(){
        residual = Vector3.zero;

        if (Mill_Sources == null || Mill_Sources.Length == 0 || Mill_Sources[0] == null || Model.childCount == 0)
            return;

        Mill_Pose(out Vector3 position, out Quaternion rotation, out Vector3 scale);
        Model.localPosition = position;
        Model.localRotation = rotation;
        Model.localScale = scale;

        Bounds clone = Rendered_Bounds(new[]{ Model }, out bool cloneAny);
        Bounds live = Rendered_Bounds(Mill_Sources, out bool liveAny);

        Model.localPosition = tablePosition;
        Model.localRotation = tableRotation;
        Model.localScale = tableScale;

        if (cloneAny && liveAny)
            residual = Quaternion.Inverse(Mill_Sources[0].rotation) * (live.center - clone.center);
    }

    private static Bounds Rendered_Bounds(Transform[] roots, out bool any){
        Bounds bounds = new Bounds();
        any = false;

        foreach (Transform root in roots){
            if (root == null)
                continue;

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true)){
                if (any){
                    bounds.Encapsulate(renderer.bounds);
                }
                else{
                    bounds = renderer.bounds;
                    any = true;
                }
            }
        }

        return bounds;
    }

    public void Set_Glide(float t){
        if (Model == null)
            return;

        Glide_T = t;
        Mill_Pose(out Vector3 millPosition, out Quaternion millRotation, out Vector3 millScale);
        Model.localPosition = Vector3.Lerp(tablePosition, millPosition, t);
        Model.localRotation = Quaternion.Slerp(tableRotation, millRotation, t);
        Model.localScale = Vector3.Lerp(tableScale, millScale, t);
    }

    // The Model pose that lands the copy on the machine, in the part's local space. The
    // clones were copied carrying their mill nodes' world transforms, so world identity is
    // where the bake said they belong — but the mill (or a subassembly like the cabinet)
    // can be moved after the table is built, so the landing is anchored to where the first
    // source node is NOW: read fresh on every glide frame rather than cached, it stays
    // honest even against a source that a demo animates mid-lesson. The container moves as
    // one, so any sibling clones follow at their baked offsets.
    private void Mill_Pose(out Vector3 position, out Quaternion rotation, out Vector3 scale){
        Vector3 targetPosition = Vector3.zero;
        Quaternion targetRotation = Quaternion.identity;
        Vector3 targetScale = Vector3.one;

        Transform source = Mill_Sources != null && Mill_Sources.Length > 0 ? Mill_Sources[0] : null;

        if (source != null && Model.childCount > 0){
            // Solve the Model world pose W with W * clone_local == source_world. At bake
            // time clone_local equalled the source's world transform, so this is identity
            // until something moves — and exactly the correction once something has.
            Transform clone = Model.GetChild(0);
            Vector3 sourceScale = source.lossyScale;
            Vector3 cloneScale = clone.localScale;
            targetScale = new Vector3(sourceScale.x / cloneScale.x, sourceScale.y / cloneScale.y, sourceScale.z / cloneScale.z);
            targetRotation = source.rotation * Quaternion.Inverse(clone.localRotation);
            targetPosition = source.position - targetRotation * Vector3.Scale(targetScale, clone.localPosition)
                + source.rotation * residual;
        }

        Transform parent = Model.parent;
        Vector3 parentScale = parent.lossyScale;
        position = parent.InverseTransformPoint(targetPosition);
        rotation = Quaternion.Inverse(parent.rotation) * targetRotation;
        scale = new Vector3(targetScale.x / parentScale.x, targetScale.y / parentScale.y, targetScale.z / parentScale.z);
    }

    // The clone lands exactly on the geometry it was copied from, so the real part is
    // switched off for the demonstration — otherwise two identical coincident meshes
    // z-fight. It stays off once the part has settled: the clone has taken its place.
    public void Show_Mill_Source(bool visible){
        if (Mill_Sources == null)
            return;

        foreach (Transform source in Mill_Sources){
            if (source == null)
                continue;

            foreach (Renderer renderer in source.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = visible;
        }
    }

    // Once a part has settled, the real mill geometry takes back over and the copy is put
    // away. The two are pixel-identical at the landing pose so the swap is invisible, and
    // it matters: the originals are what the axis and milling demos animate, while a copy
    // parented to the parts table would just sit there while the machine moved around it.
    public void Show_Model(bool visible){
        if (Model != null)
            Model.gameObject.SetActive(visible);
    }

    public void Set_Glow(bool on, Color color){
        if (Glow == null)
            return;

        Glow.enabled = on;

        if (!on)
            return;

        block ??= new MaterialPropertyBlock();
        Glow.GetPropertyBlock(block);
        block.SetColor(BaseColorId, color);
        Glow.SetPropertyBlock(block);
    }
}
