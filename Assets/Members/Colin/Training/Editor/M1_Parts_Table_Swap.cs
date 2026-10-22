using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Rebuilds the Module 1 parts-table display models from the mill standing in the open
// scene, so the table shows the machine the trainee is actually looking at. Each Part_
// object gets a plain copy (no prefab link) of the mill node it names, kept at the mill
// part's own size — the glide demonstration must not change a part's size mid-flight —
// stood on the table top, with the Table_Part glow shell resized around it. Re-run after
// the mill model changes.
public static class M1_Parts_Table_Swap{
    private const string PartsRootName = "Parts_Table_Items";
    private const string MillRootName = "ClassifyIntellitekMill";
    private const string GlowName = "Glow_Shell";
    private const string ModelName = "Model";
    // The glow shell sits 15% proud of the part, as the original table was authored.
    private const float GlowPadding = 1.15f;

    // Which mill nodes each table part shows. The guard door is both panels — the
    // guard_door marker box covers the pair. Names resolve inside the training wrapper,
    // so the Vice, which hangs off the wrapper rather than the mill, is reachable too.
    private static readonly (string Part, string[] Nodes)[] Sources = {
        ("Part_spindle_motor", new[]{ "SM_Rotating" }),
        ("Part_spindle_head", new[]{ "SM_Static" }),
        ("Part_vice", new[]{ "Vice" }),
        ("Part_guard_door", new[]{ "LeftMillDoor", "RightMillDoor" }),
        ("Part_emergency_stop", new[]{ @"\X2\59276025505C\X0  usemtl color_bfbfbf" }),
        ("Part_power_on", new[]{ "kaig" })
    };

    [MenuItem("Training/9 Parts Table - Rebuild From Mill")]
    public static void RebuildFromMill(){
        if (Application.isPlaying){
            Debug.LogError("M1_Parts_Table_Swap: exit play mode — edits made now are discarded.");
            return;
        }

        GameObject partsObject = GameObject.Find(PartsRootName);

        if (partsObject == null){
            Debug.LogError($"M1_Parts_Table_Swap: no {PartsRootName} in open scene.");
            return;
        }

        GameObject millObject = GameObject.Find(MillRootName);

        if (millObject == null){
            Debug.LogError($"M1_Parts_Table_Swap: no {MillRootName} in open scene.");
            return;
        }

        Transform mill = millObject.transform;
        Transform searchRoot = mill.parent != null ? mill.parent : mill;
        Undo.SetCurrentGroupName("Rebuild Parts Table");
        int group = Undo.GetCurrentGroup();

        foreach ((string partName, string[] nodeNames) in Sources)
            RebuildPart(partsObject.transform, searchRoot, partName, nodeNames);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(partsObject.scene);
    }

    private static void RebuildPart(Transform partsRoot, Transform searchRoot, string partName, string[] nodeNames){
        Transform part = partsRoot.Find(partName);

        // Which parts the table shows is an authoring choice — a mapping entry with no
        // Part_ object just means that one was taken off the table, so skip it quietly.
        if (part == null){
            Debug.Log($"M1_Parts_Table_Swap: {partName} not on the table — skipped.");
            return;
        }

        Transform glow = part.Find(GlowName);

        if (glow == null){
            Debug.LogError($"M1_Parts_Table_Swap: {partName} has no {GlowName}.");
            return;
        }

        var sources = new List<Transform>();

        foreach (string nodeName in nodeNames){
            Transform node = FindChild(searchRoot, nodeName);

            if (node == null){
                Debug.LogError($"M1_Parts_Table_Swap: no mill node '{nodeName}' for {partName} — part left as it was.");
                return;
            }

            sources.Add(node);
        }

        for (int i = part.childCount - 1; i >= 0; i--)
            if (part.GetChild(i) != glow)
                Undo.DestroyObjectImmediate(part.GetChild(i).gameObject);

        // Everything the part shows hangs off one container, so the table pose is a single
        // transform and Table_Part_Glide can animate the set — however many clones it is —
        // by moving just this. Built at world identity: the clones are copied in world
        // space, so at identity they sit exactly on the machine, and that pose IS the
        // glide's mill end. The fit below then carries the container to the table.
        GameObject modelObject = new GameObject(ModelName);
        Transform model = modelObject.transform;
        model.SetParent(part, false);
        model.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        model.localScale = Reciprocal(part.lossyScale);

        foreach (Transform source in sources){
            GameObject clone = Object.Instantiate(source.gameObject, model, true);
            clone.name = source.name + "(Clone)";
            Strip(clone);
        }

        Bounds bounds = CombinedBounds(model);
        Vector3 size = bounds.size;

        // A clone at world p lands at model.position + p — place the container so the set
        // ends up centred on the part and standing on the table rather than buried in it.
        Vector3 stood = new Vector3(part.position.x, part.position.y + size.y * 0.5f, part.position.z);
        model.position = stood - bounds.center;
        Undo.RegisterCreatedObjectUndo(modelObject, "Rebuild Parts Table");

        Undo.RecordObject(glow, "Rebuild Parts Table");
        glow.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
        glow.localScale = size * GlowPadding;

        Wire(part, model, sources, partName);
        Debug.Log($"M1_Parts_Table_Swap: {partName} <- {string.Join(" + ", nodeNames)} (size {size.x:F3} x {size.y:F3} x {size.z:F3}).");
    }

    // The table copies are display props: the doors in particular arrive carrying their
    // physics rig and interactables. Reverse order so dependents go before what they
    // require (joint and grab interactable before the Rigidbody).
    private static void Strip(GameObject clone){
        Component[] components = clone.GetComponentsInChildren<Component>(true);

        for (int i = components.Length - 1; i >= 0; i--){
            Component component = components[i];

            if (component == null || component is Transform || component is MeshFilter || component is MeshRenderer)
                continue;

            Object.DestroyImmediate(component);
        }
    }

    // Table_Part drives the glide, so it needs the container to move and the mill nodes
    // whose renderers have to give way to the clone standing in for them.
    private static void Wire(Transform part, Transform model, List<Transform> sources, string partName){
        Table_Part tablePart = part.GetComponent<Table_Part>();

        if (tablePart == null){
            Debug.LogWarning($"M1_Parts_Table_Swap: {partName} has no Table_Part — it will not glide.");
            return;
        }

        SerializedObject so = new SerializedObject(tablePart);
        so.FindProperty("Model").objectReferenceValue = model;
        SerializedProperty millSources = so.FindProperty("Mill_Sources");
        millSources.arraySize = sources.Count;

        for (int i = 0; i < sources.Count; i++)
            millSources.GetArrayElementAtIndex(i).objectReferenceValue = sources[i];

        so.ApplyModifiedProperties();
    }

    private static Vector3 Reciprocal(Vector3 scale){
        return new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
    }

    private static Bounds CombinedBounds(Transform root){
        Bounds bounds = new Bounds();
        bool started = false;

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>()){
            if (!started){
                bounds = renderer.bounds;
                started = true;
                continue;
            }

            bounds.Encapsulate(renderer.bounds);
        }

        return bounds;
    }

    private static Transform FindChild(Transform root, string name){
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name)
                return t;

        return null;
    }
}
