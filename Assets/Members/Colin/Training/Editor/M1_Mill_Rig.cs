using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProMill8000;
using static Training_Builder_Core;

// Module 1 — the mill itself and everything that makes it move: placement, the
// training travel limits written onto its axes, the guard-door rig, the workholding
// that rides the table, the axis-legend indicators, and the Mill_Demo_Controller
// wiring. No animation runs here; this authors the parameters the runtime plays back.
//
// Motion is implemented in three runtime classes, none of which this file changes:
//   ProMill8000.AxisMovement    per-frame MoveTowards along one axis, drags dependents
//   ProMill8000.MillingAnimation  the plunge / square / retract cutting coroutine
//   Mill_Demo_Controller        sequences the above for the Axis_Demo / Milling_Demo steps
public partial class M1_Module_Builder{
    // Same axis-aligned orientation as the DigitalTwin instance (FBX is Z-up):
    // machine X (table) = world Z, machine Y (saddle) = world X, machine Z (spindle) = world Y.
    private static readonly Quaternion MillRotation = new Quaternion(-0.5f, -0.5f, 0.5f, 0.5f);
    private const float MillScale = 100f;

    // Vise and demo block poses, captured in-editor and locked so Training/3 rebuilds
    // keep them. These are absolute local transforms in unscaled wrapper space, not
    // offsets — the vise imports lying on its side and the block has to land under the
    // spindle for the milling demo to cut it, neither of which fell out of the node
    // bounds. To re-place after a model swap: move them by hand in the scene, read the
    // localPosition / localRotation back off the inspector, and bake them in here.
    private static readonly Vector3 ViceLocalPosition = new Vector3(-0.0589f, 1.1702f, 1.9683f);
    private static readonly Quaternion ViceLocalRotation = Quaternion.Euler(0f, 0f, 270f);
    private static readonly Vector3 BlockLocalPosition = new Vector3(-0.0859f, 1.2365f, 1.9087f);

    // ------------------------------------------------------------------
    // Motion parameters (the single source of truth for how far the mill moves)
    // ------------------------------------------------------------------

    // Rated travels from the Intelitek ProMill 8000 Quick Start guide (34-0000-8000
    // Rev-C) at model scale, where 1 world unit = 1 m. Three things read these: the
    // axis clamps baked into PM8000_Training.prefab, the distances the demo actually
    // drives, and the axis-demo prompt text in M1_Lesson_Content. They used to be
    // written out separately and had drifted — the demo drove Y 80 mm while the
    // prompt claimed 152 mm. Change a travel here and all three follow.
    private const float XTravelMm = 280f; // table left / right — machine X
    private const float YTravelMm = 152f; // table fore / aft   — machine Y
    private const float ZTravelMm = 270f; // spindle up / down  — machine Z

    // The table axes are centred on their origin, so the clamp is half-travel each way.
    private const float XHalfTravel = XTravelMm / 2000f;  // 0.14
    private const float YHalfTravel = YTravelMm / 2000f;  // 0.076
    // The spindle only travels down from its parked origin.
    private const float ZDropTravel = ZTravelMm / 1000f;  // 0.27

    // Training-only overrides: slow enough to narrate over.
    private const float TableSpeed = 0.05f;
    private const float SpindleSpeed = 0.1f;

    // The guard doors run unclamped — Item_Mill_Doors drives them to a fixed offset
    // rather than sweeping between limits.
    private const float DoorSpeed = 1f;
    private const float DoorSlideDistance = 0.25f;
    private const float DoorSeconds = 1f;

    // ------------------------------------------------------------------
    // The wired mill
    // ------------------------------------------------------------------

    // Everything downstream phases need off the mill. Machine-axis naming throughout;
    // note MillingAnimation names the same three axes in world terms (worktableX,
    // worktableZ, spindleY), and ReadMillAxes owns that mapping.
    private class M1_Mill_Rig{
        public GameObject Wrapper;      // "PM8000_Training" — unscaled, identity rotation
        public Transform Mill;          // the model instance: 100x, MillRotation
        public Bounds MillBounds;       // world bounds after grounding, before the clamp is removed
        public AxisMovement TableX;     // machine X — WB_XAxis_Drive
        public AxisMovement SaddleY;    // machine Y — WB_YAxis_Drive (carries the X stage)
        public AxisMovement SpindleZ;   // machine Z — SpindleMotor
        public MillingAnimation Milling;
        public Item_Mill_Doors Doors;
        public Transform Vice;          // null when the vice prefab is missing
        public Transform Block;
    }

    // The three axis-legend billboards the demo shows while each axis runs.
    private class M1_Axis_Indicators{
        public GameObject X;
        public GameObject Y;
        public GameObject Z;
    }

    // Instantiates the mill, grounds it, retunes its axes for training, adds the
    // guard-door rig and seats the workholding. Returns null after a logged error if
    // the model, its door nodes or its MillingAnimation rig are missing.
    private static M1_Mill_Rig BuildMillRig(){
        GameObject millPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Colin_Training_Paths.MillPrefabPath);
        if (millPrefab == null){ Debug.LogError($"M1_Mill_Rig: mill prefab missing at {Colin_Training_Paths.MillPrefabPath}"); return null; }

        M1_Mill_Rig rig = new M1_Mill_Rig();
        rig.Wrapper = new GameObject(Colin_Training_Paths.MillWrapperName);
        GameObject mill = (GameObject)PrefabUtility.InstantiatePrefab(millPrefab);
        rig.Mill = mill.transform;
        rig.Mill.SetParent(rig.Wrapper.transform, false);
        rig.Mill.localRotation = MillRotation;
        rig.Mill.localScale = Vector3.one * MillScale;

        // Ground the mill and center it in front of the player spawn.
        Bounds bounds = RendererBounds(rig.Mill);
        rig.Mill.position += new Vector3(-bounds.center.x, -bounds.min.y, 1.9f - bounds.center.z);
        // Re-measured while WB_Clamp is still present: the axis indicators are placed
        // off these bounds and must not shift when the clamp goes.
        rig.MillBounds = RendererBounds(rig.Mill);

        // WB_Clamp removed from the cell — the vise provides workholding (validated
        // in-editor). Removed after grounding so the mill position is unchanged.
        Transform clamp = FindChild(rig.Mill, "WB_Clamp");
        if (clamp != null)
            Object.DestroyImmediate(clamp.gameObject);

        Transform doors = FindChild(rig.Mill, "doors");
        Transform door1 = FindChild(rig.Mill, "Door1");
        Transform door2 = FindChild(rig.Mill, "Door2");

        if (doors == null || door1 == null || door2 == null){
            Debug.LogError("M1_Mill_Rig: mill door nodes not found — check model hierarchy names.");
            return null;
        }

        if (!ReadMillAxes(rig)) return null;

        ApplyTrainingTravelLimits(rig);
        BuildDoorRig(rig, doors, door1, door2);
        SeatWorkholding(rig);
        RegisterAxisRiders(rig);
        return rig;
    }

    // The prefab already carries the tested mill rig: MillingAnimation on
    // MillController driving WB_XAxis_Drive (machine X = world X), WB_YAxis_Drive
    // (machine Y = world Z, carries the X stage as a dependent) and SpindleMotor
    // (machine Z = world Y). Reuse it instead of duplicating axis components on the
    // static assemblies. The world-named fields below are MillingAnimation's; this is
    // the only place the world/machine axis-letter mismatch is crossed.
    private static bool ReadMillAxes(M1_Mill_Rig rig){
        rig.Milling = rig.Mill.GetComponentInChildren<MillingAnimation>();
        if (rig.Milling == null){ Debug.LogError("M1_Mill_Rig: no MillingAnimation in the mill prefab — check the prefab rig."); return false; }

        SerializedObject millingSO = new SerializedObject(rig.Milling);
        rig.TableX = millingSO.FindProperty("worktableX").objectReferenceValue as AxisMovement;
        rig.SaddleY = millingSO.FindProperty("worktableZ").objectReferenceValue as AxisMovement;
        rig.SpindleZ = millingSO.FindProperty("spindleY").objectReferenceValue as AxisMovement;

        if (rig.TableX == null || rig.SaddleY == null || rig.SpindleZ == null){
            Debug.LogError("M1_Mill_Rig: MillingAnimation axes not wired in the mill prefab — check the prefab rig.");
            return false;
        }

        return true;
    }

    // Slow the axes for instruction and clamp them to the real travels.
    private static void ApplyTrainingTravelLimits(M1_Mill_Rig rig){
        ConfigureAxis(rig.TableX, TableSpeed, -XHalfTravel, XHalfTravel, true);
        ConfigureAxis(rig.SaddleY, TableSpeed, -YHalfTravel, YHalfTravel, true);
        ConfigureAxis(rig.SpindleZ, SpindleSpeed, -ZDropTravel, 0f, true);
    }

    private static void BuildDoorRig(M1_Mill_Rig rig, Transform doors, Transform door1, Transform door2){
        AxisMovement door1Axis = AddAxis(door1.gameObject, MovementAxis.Z, DoorSpeed, 0f, 0f, false);
        AxisMovement door2Axis = AddAxis(door2.gameObject, MovementAxis.Z, DoorSpeed, 0f, 0f, false);

        rig.Doors = doors.gameObject.AddComponent<Item_Mill_Doors>();
        SetRef(rig.Doors, "leftDoor", door2Axis);
        SetRef(rig.Doors, "rightDoor", door1Axis);
        SetVal(rig.Doors, "slideDistance", DoorSlideDistance);
    }

    // Vise and demo block, parented to the unscaled wrapper rather than the 100x
    // rotated mill subtree so the baked poses stay readable.
    private static void SeatWorkholding(M1_Mill_Rig rig){
        GameObject vicePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Colin_Training_Paths.VicePrefabPath);

        if (vicePrefab != null){
            GameObject vice = (GameObject)PrefabUtility.InstantiatePrefab(vicePrefab);
            vice.name = Colin_Training_Paths.ViceName;
            vice.transform.SetParent(rig.Wrapper.transform, false);
            rig.Vice = vice.transform;
        }

        GameObject blockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Colin_Training_Paths.DemoBlockPrefabPath);
        GameObject block = (GameObject)PrefabUtility.InstantiatePrefab(blockPrefab);
        block.transform.SetParent(rig.Wrapper.transform, false);
        rig.Block = block.transform;

        if (rig.Vice != null){
            rig.Vice.localPosition = ViceLocalPosition;
            rig.Vice.localRotation = ViceLocalRotation;
        }

        rig.Block.localPosition = BlockLocalPosition;
    }

    // Make the workholding ride table motion.
    private static void RegisterAxisRiders(M1_Mill_Rig rig){
        List<Object> riders = new List<Object>();
        if (rig.Vice != null) riders.Add(rig.Vice);
        riders.Add(rig.Block);

        // Append, never replace — SaddleY.dependents already carries the X stage from
        // the source prefab. Overwriting it makes the Y demo move the saddle while the
        // table stays behind, with no error and no warning.
        AppendRefArray(rig.TableX, "dependents", riders);
        AppendRefArray(rig.SaddleY, "dependents", riders);
    }

    // Saved before the markers, indicators, lesson scaffold and parts table exist, so
    // the prefab holds only the mill rig plus vise and block. M2 instantiates this.
    private static void SaveMillRigPrefab(M1_Mill_Rig rig){
        PrefabUtility.SaveAsPrefabAssetAndConnect(rig.Wrapper, Colin_Training_Paths.MillTrainingPrefabPath, InteractionMode.AutomatedAction);
    }

    // ------------------------------------------------------------------
    // Demo playback wiring
    // ------------------------------------------------------------------

    private static M1_Axis_Indicators BuildAxisIndicators(Bounds millBounds){
        GameObject root = new GameObject("Axis_Indicators");
        float y = millBounds.max.y * 0.8f;
        float frontZ = millBounds.min.z - 0.2f;

        return new M1_Axis_Indicators{
            X = CreateIndicator(root.transform, "X — table left / right", new Color(1f, 0.3f, 0.3f), new Vector3(-0.7f, y, frontZ)),
            Y = CreateIndicator(root.transform, "Y — table fore / aft", new Color(0.3f, 1f, 0.4f), new Vector3(0f, y, frontZ)),
            Z = CreateIndicator(root.transform, "Z — spindle up / down", new Color(0.35f, 0.55f, 1f), new Vector3(0.7f, y, frontZ))
        };
    }

    // Field names below are matched by string against Mill_Demo_Controller's private
    // [SerializeField]s — a rename there surfaces as a SetRef/SetVal error on the next
    // Training/3, not as a compile error.
    //
    // The travel distances are written here rather than left to the controller's field
    // initializers, which is what let them drift out of step with the axis clamps.
    private static void WireMillDemo(Mill_Demo_Controller demo, M1_Mill_Rig rig, M1_Axis_Indicators indicators){
        SetRef(demo, "WorktableX", rig.TableX);
        SetRef(demo, "SaddleY", rig.SaddleY);
        SetRef(demo, "SpindleZ", rig.SpindleZ);
        SetRef(demo, "Milling", rig.Milling);
        SetRef(demo, "Doors", rig.Doors);
        SetRef(demo, "XIndicator", indicators.X);
        SetRef(demo, "YIndicator", indicators.Y);
        SetRef(demo, "ZIndicator", indicators.Z);
        SetVal(demo, "XTravel", XHalfTravel);
        SetVal(demo, "YTravel", YHalfTravel);
        SetVal(demo, "ZTravel", ZDropTravel);
        SetVal(demo, "DoorSeconds", DoorSeconds);
    }

    // ------------------------------------------------------------------
    // AxisMovement plumbing (its tuning fields are private and serialized)
    // ------------------------------------------------------------------

    private static AxisMovement AddAxis(GameObject target, MovementAxis axis, float speed, float min, float max, bool enableLimits){
        AxisMovement movement = target.AddComponent<AxisMovement>();
        SerializedObject so = new SerializedObject(movement);
        so.FindProperty("axis").enumValueIndex = (int)axis;
        so.ApplyModifiedPropertiesWithoutUndo();
        ConfigureAxis(movement, speed, min, max, enableLimits);
        return movement;
    }

    // enableLimits deliberately has no default. The guard doors pass false, and letting
    // it default to true would clamp them to zero offset so they never open — silently,
    // because the milling demo waits on MillingAnimation rather than on door motion, and
    // the broken axis would then propagate into M2 through the saved prefab.
    private static void ConfigureAxis(AxisMovement movement, float speed, float min, float max, bool enableLimits){
        SerializedObject so = new SerializedObject(movement);
        so.FindProperty("speed").floatValue = speed;
        so.FindProperty("enableLimits").boolValue = enableLimits;
        so.FindProperty("minOffset").floatValue = min;
        so.FindProperty("maxOffset").floatValue = max;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
