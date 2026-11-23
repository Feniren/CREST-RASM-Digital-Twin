using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// Makes all four door handles on the ASRS prefab open their own door, and
// lets Module 1's door step accept any of them.
//
// The ASRS prefab has two door pairs:
//   36 side  LeftHingeJoint / RightHingeJoint  — already animated
//            (LeftDoorHinge / RightDoorHinge controllers), handles already
//            carry Door_Handle_Toggle. Re-checked and re-linked here.
//   U side   LeftHingeJointU / RightHingeJointU — no Animator, and their
//            handles only had Interactable_Handle (a physics drag handle that
//            needs a BoxCollider + Rigidbody they don't have, and does nothing
//            on click). RightHingeJointU was also parented under
//            LeftHingeJointU, so it would have swung with the left door.
//
// For the U side this builds its own clips + controllers in
// Assets/Members/Alan/DoorAnimations: the 36 clips can't be reused because
// they hold absolute angles for the 36 hinges' imported (-90, 0, 0) rotation.
// The U clips copy the 36 doors' timing and easing (RightDoorOpen) around the
// U hinges' own rotation; the close clip is the open clip mirrored in time,
// which Door_Handle_Toggle relies on to reverse a swing smoothly. Which way
// each U door swings (outward) is worked out from the geometry, checked
// against the known 36 doors.
//
// Shared scripts are reused, not changed: Interactable_Select (the project's
// click / VR-trigger select) drives Door_Handle_Toggle on every handle.
public static class ASRS_Door_Handles_Builder
{
    private const string PrefabPath = "Assets/Prefabs/ASRS Variant 1.prefab";
    private const string AlanFolder = "Assets/Members/Alan";
    private const string DoorFolder = AlanFolder + "/DoorAnimations";
    private const string LogTag = "[ASRS][Doors]";

    private struct Door
    {
        public string Hinge, Handle, Open, Close;
        public string Controller;    // existing controller (36 side), or null = build one (U side)
        public string NewController; // U side: name of the controller to build
    }

    private static readonly Door[] Doors =
    {
        new Door { Hinge = "LeftHingeJoint", Handle = "LeftDoorHandle36", Open = "LeftDoorOpen", Close = "LeftDoorClose", Controller = AlanFolder + "/LeftDoorHinge.controller" },
        new Door { Hinge = "RightHingeJoint", Handle = "RightDoorHandle36", Open = "RightDoorOpen", Close = "RightDoorClose", Controller = AlanFolder + "/RightDoorHinge.controller" },
        new Door { Hinge = "LeftHingeJointU", Handle = "LeftDoorHandle36u", Open = "LeftDoorUOpen", Close = "LeftDoorUClose", NewController = "LeftDoorHingeU" },
        new Door { Hinge = "RightHingeJointU", Handle = "RightDoorHandle36u", Open = "RightDoorUOpen", Close = "RightDoorUClose", NewController = "RightDoorHingeU" },
    };

    private const string TimingClipPath = AlanFolder + "/RightDoorOpen.anim"; // a clean 0 → -90 swing
    private const string KnownLeftOpenPath = AlanFolder + "/LeftDoorOpen.anim";
    private const string KnownRightOpenPath = AlanFolder + "/RightDoorOpen.anim";

    [MenuItem("ASRS/Wire All Door Handles (Prefab + Module 1 Door Step)")]
    public static void WireAll()
    {
        if (!WirePrefab())
            return;

        WireModule1DoorStep();
    }

    // ------------------------------------------------------------- prefab

    private static bool WirePrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null)
        {
            Debug.LogError($"{LogTag} Couldn't open '{PrefabPath}'.");
            return false;
        }

        try
        {
            // RightHingeJointU must not ride on the left U door.
            Transform rightU = Find(root, "RightHingeJointU");
            Transform leftU = Find(root, "LeftHingeJointU");
            if (rightU != null && leftU != null && rightU.IsChildOf(leftU))
            {
                rightU.SetParent(root.transform, true);
                Debug.Log($"{LogTag} RightHingeJointU moved out of LeftHingeJointU (to the prefab root, same world pose) so the two U doors swing independently.");
            }

            float flip = OutwardCheck(root);
            bool ok = true;

            foreach (Door door in Doors)
            {
                Transform hinge = Find(root, door.Hinge);
                Transform handle = Find(root, door.Handle);
                if (hinge == null || handle == null)
                {
                    Debug.LogError($"{LogTag} '{door.Hinge}' or '{door.Handle}' not found in the prefab — skipped.");
                    ok = false;
                    continue;
                }

                if (!handle.IsChildOf(hinge))
                    Debug.LogWarning($"{LogTag} '{door.Handle}' isn't under '{door.Hinge}', so it won't move with its door.");

                RuntimeAnimatorController controller = door.Controller != null
                    ? AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(door.Controller)
                    : BuildUController(door, hinge, OutwardSign(root, hinge) * flip);

                if (controller == null)
                {
                    Debug.LogError($"{LogTag} No controller for '{door.Hinge}' — skipped.");
                    ok = false;
                    continue;
                }

                LinkHandle(hinge, handle, controller, door);
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"{LogTag} '{PrefabPath}' saved — all {Doors.Length} door handles open their own door (ASRSModule1, ASRSModule2 and RackmovementTest pick this up).");
            return ok;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void LinkHandle(Transform hinge, Transform handle, RuntimeAnimatorController controller, Door door)
    {
        Animator animator = hinge.GetComponent<Animator>();
        if (animator == null)
            animator = hinge.gameObject.AddComponent<Animator>();
        if (animator.runtimeAnimatorController != controller)
            animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        // Interactable_Handle is first in line for the project's select
        // raycast (GetComponentInParent<Interactable_Parent>) and does
        // nothing on click — the handle uses Interactable_Select instead.
        foreach (Interactable_Handle dragHandle in handle.GetComponents<Interactable_Handle>())
            Object.DestroyImmediate(dragHandle, true);

        if (handle.GetComponent<Interactable_Select>() == null)
            handle.gameObject.AddComponent<Interactable_Select>();

        Door_Handle_Toggle toggle = handle.GetComponent<Door_Handle_Toggle>();
        if (toggle == null)
            toggle = handle.gameObject.AddComponent<Door_Handle_Toggle>();

        SerializedObject so = new SerializedObject(toggle);
        so.FindProperty("doorAnimator").objectReferenceValue = animator;
        so.FindProperty("openStateName").stringValue = door.Open;
        so.FindProperty("closeStateName").stringValue = door.Close;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (handle.GetComponent<Collider>() == null)
            Debug.LogWarning($"{LogTag} '{door.Handle}' has no collider — the project's select raycast can't hit it (the lesson's pointer still can).", handle);

        Debug.Log($"{LogTag} {door.Handle} → {door.Hinge} ({controller.name}: '{door.Open}' / '{door.Close}').");
    }

    // ------------------------------------------------- U-side animations

    private static RuntimeAnimatorController BuildUController(Door door, Transform hinge, float swingSign)
    {
        EnsureFolder(DoorFolder);

        AnimationClip timing = AssetDatabase.LoadAssetAtPath<AnimationClip>(TimingClipPath);
        Vector3 baseEuler = hinge.localEulerAngles;
        float swing = 90f * swingSign;

        AnimationClip closed = LoadOrCreateClip($"{DoorFolder}/{door.NewController}_Closed.anim");
        AnimationClip open = LoadOrCreateClip($"{DoorFolder}/{door.Open}.anim");
        AnimationClip close = LoadOrCreateClip($"{DoorFolder}/{door.Close}.anim");

        Keyframe[] openKeys = OpenKeys(timing, baseEuler.y, swing);
        float stop = openKeys[openKeys.Length - 1].time;
        Keyframe[] closeKeys = openKeys.Reverse()
            .Select(k => new Keyframe(stop - k.time, k.value, -k.outTangent, -k.inTangent))
            .ToArray();

        WriteEuler(closed, baseEuler, new[] { new Keyframe(0f, baseEuler.y), new Keyframe(1f / 60f, baseEuler.y) });
        WriteEuler(open, baseEuler, openKeys);
        WriteEuler(close, baseEuler, closeKeys);

        string path = $"{DoorFolder}/{door.NewController}.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState child in machine.states.ToArray())
            machine.RemoveState(child.state);

        AnimatorState idle = machine.AddState("Idle");
        idle.motion = closed;
        machine.AddState(door.Open).motion = open;
        machine.AddState(door.Close).motion = close;
        machine.defaultState = idle;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log($"{LogTag} Built {path}: swings {swing:+0;-0}° about the vertical from its closed rotation {baseEuler}.", controller);
        return controller;
    }

    // The timing source's Y-angle swing, rescaled to 0 → 'swing' on top of 'baseY'.
    private static Keyframe[] OpenKeys(AnimationClip timing, float baseY, float swing)
    {
        AnimationCurve source = timing != null ? AnimationUtility.GetEditorCurve(timing, EulerBinding("y")) : null;
        if (source == null || source.length < 2 || Mathf.Abs(source.keys[source.length - 1].value - source.keys[0].value) < 1f)
        {
            Debug.LogWarning($"{LogTag} No usable swing in '{TimingClipPath}' — using a plain 7 s ease-in-out.");
            AnimationCurve fallback = AnimationCurve.EaseInOut(0f, baseY, 7f, baseY + swing);
            fallback.AddKey(8.083333f, baseY + swing);
            return fallback.keys;
        }

        float start = source.keys[0].value;
        float span = source.keys[source.length - 1].value - start;
        float scale = swing / span;
        return source.keys
            .Select(k => new Keyframe(k.time, baseY + (k.value - start) * scale, k.inTangent * scale, k.outTangent * scale))
            .ToArray();
    }

    private static void WriteEuler(AnimationClip clip, Vector3 baseEuler, Keyframe[] yKeys)
    {
        clip.ClearCurves();
        clip.frameRate = 60f;
        float end = yKeys[yKeys.Length - 1].time;

        AnimationUtility.SetEditorCurve(clip, EulerBinding("x"), AnimationCurve.Constant(0f, end, baseEuler.x));
        AnimationUtility.SetEditorCurve(clip, EulerBinding("y"), new AnimationCurve(yKeys));
        AnimationUtility.SetEditorCurve(clip, EulerBinding("z"), AnimationCurve.Constant(0f, end, baseEuler.z));

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
    }

    private static EditorCurveBinding EulerBinding(string axis)
    {
        return EditorCurveBinding.FloatCurve("", typeof(Transform), "localEulerAnglesRaw." + axis);
    }

    private static AnimationClip LoadOrCreateClip(string path)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip != null)
            return clip;

        clip = new AnimationClip();
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    // --------------------------------------------- which way is outward

    // +1 or -1: the swing about the vertical that carries the door AWAY from
    // the cabinet's centre.
    private static float OutwardSign(GameObject root, Transform hinge)
    {
        Bounds cabinet = RendererBounds(root.transform);
        Bounds door = RendererBounds(hinge);
        Vector3 axis = hinge.parent != null ? hinge.parent.up : Vector3.up;
        Vector3 offset = door.center - hinge.position;

        float Reach(float sign)
        {
            Vector3 swung = hinge.position + Quaternion.AngleAxis(sign * 90f, axis) * offset;
            return Vector3.ProjectOnPlane(swung - cabinet.center, axis).magnitude;
        }

        return Reach(1f) >= Reach(-1f) ? 1f : -1f;
    }

    // Checks the outward rule against the two 36 doors, whose open clips are
    // known to be right; returns -1 if the rule comes out backwards for them.
    private static float OutwardCheck(GameObject root)
    {
        int agree = 0, disagree = 0;
        foreach ((string hinge, string clip) in new[] { ("LeftHingeJoint", KnownLeftOpenPath), ("RightHingeJoint", KnownRightOpenPath) })
        {
            Transform t = Find(root, hinge);
            AnimationClip c = AssetDatabase.LoadAssetAtPath<AnimationClip>(clip);
            AnimationCurve y = c != null ? AnimationUtility.GetEditorCurve(c, EulerBinding("y")) : null;
            if (t == null || y == null || y.length < 2)
                continue;

            float known = Mathf.Sign(y.keys[y.length - 1].value - y.keys[0].value);
            if (OutwardSign(root, t) == known) agree++; else disagree++;
        }

        if (agree > 0 && disagree > 0)
            Debug.LogWarning($"{LogTag} The outward-swing check disagreed between the two 36 doors — check the U doors open outward; if not, re-run after swapping their direction.");

        if (disagree > agree)
        {
            Debug.Log($"{LogTag} Outward rule came out backwards for the 36 doors — flipped for the U doors.");
            return -1f;
        }
        return 1f;
    }

    private static Bounds RendererBounds(Transform t)
    {
        Renderer[] renderers = t.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(t.position, Vector3.zero);

        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers)
            b.Encapsulate(r.bounds);
        return b;
    }

    // ------------------------------------------------ Module 1 door step

    private static void WireModule1DoorStep()
    {
        SequenceManager sequenceManager = Object.FindFirstObjectByType<SequenceManager>();
        if (sequenceManager == null)
        {
            Debug.Log($"{LogTag} No SequenceManager in the open scene — prefab done; open ASRSModule1 and run this again to wire the door step.");
            return;
        }

        SerializedObject so = new SerializedObject(sequenceManager);
        SerializedProperty steps = so.FindProperty("steps");

        // Undo an earlier version of this command, which mistook the ASRS-unit
        // step (its target is the whole cabinet, handles included) for the
        // door step and replaced its target with the four handles.
        for (int i = 0; i < steps.arraySize; i++)
        {
            SerializedProperty step = steps.GetArrayElementAtIndex(i);
            if (!step.FindPropertyRelative("infoText").stringValue.StartsWith(AsrsUnitStepStart))
                continue;

            List<GameObject> current = Targets(step);
            if (current.Count == 0 || !current.All(t => t != null && t.GetComponent<Door_Handle_Toggle>() != null))
                continue;

            Item_ASRS unit = current[0].GetComponentInParent<Item_ASRS>(true);
            if (unit == null)
                continue;

            SetTargets(step, new List<GameObject> { unit.gameObject });
            step.FindPropertyRelative("anyTargetCompletes").boolValue = false;
            Debug.Log($"{LogTag} Step {i + 1} (the ASRS unit) repaired: its target is '{unit.name}' again, not the door handles.", unit);
        }

        for (int i = 0; i < steps.arraySize; i++)
        {
            SerializedProperty step = steps.GetArrayElementAtIndex(i);
            if (step.FindPropertyRelative("infoText").stringValue.StartsWith(AsrsUnitStepStart))
                continue;

            // The door step targets a handle itself — not something that
            // merely contains handles, like the whole ASRS.
            GameObject handleTarget = Targets(step).FirstOrDefault(t => t != null && t.GetComponent<Door_Handle_Toggle>() != null);
            Item_ASRS rack = handleTarget != null ? handleTarget.GetComponentInParent<Item_ASRS>(true) : null;
            if (rack == null)
                continue;
            SerializedProperty targets = step.FindPropertyRelative("targets");

            List<GameObject> handles = rack.GetComponentsInChildren<Door_Handle_Toggle>(true)
                .Select(h => h.gameObject).Distinct().ToList();

            targets.arraySize = handles.Count;
            for (int t = 0; t < handles.Count; t++)
                targets.GetArrayElementAtIndex(t).objectReferenceValue = handles[t];
            step.FindPropertyRelative("anyTargetCompletes").boolValue = true;

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(sequenceManager.gameObject.scene);
            Debug.Log($"{LogTag} Step {i + 1}: targets are now all {handles.Count} door handles on '{rack.name}' ({string.Join(", ", handles.Select(h => h.name))}); any one completes the step and opens its door. Save the scene.", sequenceManager);
            return;
        }

        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(sequenceManager.gameObject.scene);
        Debug.LogWarning($"{LogTag} No step targets a door handle in this scene — the door step wasn't changed.", sequenceManager);
    }

    private const string AsrsUnitStepStart = "This is the ASRS-36";

    private static List<GameObject> Targets(SerializedProperty step)
    {
        SerializedProperty targets = step.FindPropertyRelative("targets");
        List<GameObject> list = new List<GameObject>();
        for (int t = 0; t < targets.arraySize; t++)
            list.Add(targets.GetArrayElementAtIndex(t).objectReferenceValue as GameObject);
        return list;
    }

    private static void SetTargets(SerializedProperty step, List<GameObject> objects)
    {
        SerializedProperty targets = step.FindPropertyRelative("targets");
        targets.arraySize = objects.Count;
        for (int t = 0; t < objects.Count; t++)
            targets.GetArrayElementAtIndex(t).objectReferenceValue = objects[t];
    }

    // ------------------------------------------------------------ helpers

    private static Transform Find(GameObject root, string name)
    {
        return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        int slash = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }
}
