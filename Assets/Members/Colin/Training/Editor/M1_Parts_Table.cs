using UnityEditor;
using UnityEngine;
using static Training_Builder_Core;

// Module 1 — the reference parts table: a desk to the trainee's right carrying
// shrunk, inert duplicates of the parts the lesson asks them to identify, lit one at
// a time by Table_Part_Display during the guided tour.
public partial class M1_Module_Builder{
    // The quiz parts minus the electronics cabinet, which is the whole lower-body node
    // and reads as nothing useful at table scale. Each id is resolved through the
    // Markers table (the vise uses the live vice prefab). Order is grid slot order.
    private static readonly string[] TablePartIds = {
        MarkerSpindleMotor, MarkerSpindleHead, MarkerVice, MarkerGuardDoor, MarkerEmergencyStop
    };

    private const float TablePartSize = 0.16f; // largest world dimension of each clone
    private const int TablePartColumns = 3;

    private static void BuildPartsTable(Transform mill, Transform vice, Lesson_Sequencer sequencer, GameObject manager){
        GameObject tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Colin_Training_Paths.DeskPrefabPath);
        if (tablePrefab == null){ Debug.LogWarning($"M1_Parts_Table: parts table prefab missing at {Colin_Training_Paths.DeskPrefabPath} — skipped."); return; }

        // To the trainee's right, clear of the mill and the left-side prompt panel.
        // Grounded on the floor; X/Z hand-tunable (see plan Open items).
        GameObject table = InstantiateProp(tablePrefab, null, "Parts_Table", new Vector3(1.25f, 0f, 1.3f), Quaternion.Euler(270f, 0f, 0f));
        if (table == null) return;
        Bounds tb = RendererBounds(table.transform);
        table.transform.position += new Vector3(0f, -tb.min.y, 0f); // sit on the floor
        tb = RendererBounds(table.transform);

        Vector3[] slots = GridSlots(tb, TablePartIds.Length, TablePartSize);
        GameObject partsRoot = new GameObject("Parts_Table_Items");
        Material glow = AssetDatabase.LoadAssetAtPath<Material>(GlowMaterialPath);

        for (int i = 0; i < TablePartIds.Length; i++){
            string id = TablePartIds[i];
            Transform source = ResolvePartSource(id, mill, vice);

            if (source == null){
                Debug.LogWarning($"M1_Parts_Table: table part source not found for '{id}' — skipped.");
                continue;
            }

            BuildTablePart(partsRoot.transform, id, source, slots[i], glow);
        }

        Table_Part_Display display = manager.AddComponent<Table_Part_Display>();
        SetRef(display, "Sequencer", sequencer);
        SetRef(display, "PartsRoot", partsRoot.transform);
    }

    // Evenly-spaced slots on the table top, laid out in up to TablePartColumns
    // columns along the longer horizontal axis and rows along the shorter. margin is
    // the clone footprint kept clear of the table edges. Slot Y is the table top.
    private static Vector3[] GridSlots(Bounds table, int count, float margin){
        float topY = table.max.y;
        bool xIsLong = table.size.x >= table.size.z;
        float longCenter = xIsLong ? table.center.x : table.center.z;
        float shortCenter = xIsLong ? table.center.z : table.center.x;
        float usableLong = Mathf.Max(0.01f, (xIsLong ? table.size.x : table.size.z) - margin);
        float usableShort = Mathf.Max(0.01f, (xIsLong ? table.size.z : table.size.x) - margin);

        int cols = Mathf.Min(TablePartColumns, count);
        int rows = Mathf.CeilToInt(count / (float)cols);
        Vector3[] slots = new Vector3[count];

        for (int i = 0; i < count; i++){
            int col = i % cols;
            int row = i / cols;
            // A single column or row centres rather than pinning to one edge.
            float tc = cols == 1 ? 0.5f : (float)col / (cols - 1);
            float tr = rows == 1 ? 0.5f : (float)row / (rows - 1);
            float alongLong = (longCenter - usableLong / 2f) + tc * usableLong;
            float alongShort = (shortCenter - usableShort / 2f) + tr * usableShort;

            slots[i] = xIsLong
                ? new Vector3(alongLong, topY, alongShort)
                : new Vector3(alongShort, topY, alongLong);
        }

        return slots;
    }

    private static Transform ResolvePartSource(string id, Transform mill, Transform vice){
        return id == MarkerVice ? vice : ResolveMarkerNode(id, mill);
    }

    private static void BuildTablePart(Transform parent, string id, Transform source, Vector3 slot, Material glow){
        GameObject wrapper = new GameObject("Part_" + id);
        wrapper.transform.SetParent(parent, false);
        wrapper.transform.position = slot;

        // World-accurate copy: the source node lives under the 100x mill, so
        // match its world transform before normalizing (Instantiate keeps only
        // the source's local values).
        GameObject clone = (GameObject)Object.Instantiate(source.gameObject);
        clone.transform.SetPositionAndRotation(source.position, source.rotation);
        clone.transform.localScale = source.lossyScale;
        SanitizeClone(clone);

        // Uniform-scale so the largest dimension reads at TablePartSize.
        Bounds b = RendererBounds(clone.transform);
        float maxDim = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        if (maxDim > 0.0001f)
            clone.transform.localScale *= TablePartSize / maxDim;

        // Seat on the slot: centered in X/Z, resting on the table top.
        b = RendererBounds(clone.transform);
        clone.transform.position += new Vector3(slot.x - b.center.x, slot.y - b.min.y, slot.z - b.center.z);
        clone.transform.SetParent(wrapper.transform, true);

        // Glow shell, disabled until this is the current part (mirrors the Glow_Shell
        // on the Component_Marker prefab, built in Training_Builder_Core). The wrapper
        // has unit world scale, so the shell's local scale is its world size.
        b = RendererBounds(clone.transform);
        GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shell.name = "Glow_Shell";
        Object.DestroyImmediate(shell.GetComponent<BoxCollider>());
        shell.transform.SetParent(wrapper.transform, true);
        shell.transform.position = b.center;
        shell.transform.rotation = Quaternion.identity;
        shell.transform.localScale = b.size * 1.15f;
        MeshRenderer shellRenderer = shell.GetComponent<MeshRenderer>();
        shellRenderer.sharedMaterial = glow;
        shellRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        shellRenderer.enabled = false;

        Table_Part part = wrapper.AddComponent<Table_Part>();
        part.Part_Id = id;
        SetRef(part, "Glow", shellRenderer);
    }

    // Strip the clone to inert display geometry: scripts first (some require a
    // collider), then colliders and bodies. Keeps Transform/MeshFilter/MeshRenderer.
    private static void SanitizeClone(GameObject clone){
        foreach (MonoBehaviour mb in clone.GetComponentsInChildren<MonoBehaviour>(true))
            if (mb != null) Object.DestroyImmediate(mb);
        foreach (Collider col in clone.GetComponentsInChildren<Collider>(true))
            if (col != null) Object.DestroyImmediate(col);
        foreach (Rigidbody rb in clone.GetComponentsInChildren<Rigidbody>(true))
            if (rb != null) Object.DestroyImmediate(rb);
    }
}
