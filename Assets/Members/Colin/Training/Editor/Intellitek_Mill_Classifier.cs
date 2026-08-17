using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ProMill8000;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Bakes ClassifyIntellitekMill.prefab: a fully-unpacked REGULAR prefab (PM8000-style) whose
// semantic group hierarchy is physically parented and editor-visible. Rebuild = instantiate
// IntellitekMill.prefab, unpack completely, classify the flat CAD nodes, reparent them under
// the scaffold, save over the same asset path (GUID stays stable). FBX mesh/material edits
// keep flowing via references; new/renamed/deleted FBX nodes and base-prefab component
// changes only land on a re-run — run Rebuild after such changes. Re-runs carry over the
// existing asset's root pose and AxisMovement values, so hand-tuned axis mappings survive.
// Classification sources, in order: explicit English names, ATC springs, fastener patterns
// (deferred to hardware buckets), cabinet CJK keywords, a name-join against
// reconstructedPM8000's classified tree (same source CAD assembly), then spatial bounds
// matching. Leftovers land in Unclassified.
public static class Intellitek_Mill_Classifier{
    private const string PrefabPath = "Assets/Game_Objects/ClassifyIntellitekMill.prefab";
    private const string BasePath = "Assets/Game_Objects/IntellitekMill.prefab";
    private const string PM8000Path = "Assets/Prefabs/reconstructedPM8000.prefab";
    private const string ReportPath = "Logs/IntellitekMill_Classification_Report.md";
    private const string RootName = "ClassifyIntellitekMill";

    private const string Body = "IntellitekMillBody";
    private const string Cabinet = "ElectronicsCabinet";
    private const string Enclosure = "Enclosure";
    private const string Unclassified = "Unclassified";

    // Three clear top-level assemblies (user restructure 2026-07-30): IntellitekMillBody =
    // every mechanical assembly of the machine itself; ElectronicsCabinet = stand + electrics
    // (this machine has no cast knee — the cabinet IS the stand); Enclosure = shell, with the
    // framework and doors as subgroups. PM8000's PB_Column_*/PB_Knee_* groups are gone; the
    // WB_/SM_/TC_/SB_ leaf names stay PM8000-compatible. Parents are listed before their
    // children; leaf names are unique.
    private static readonly string[] ScaffoldPaths = {
        Body,
        Body + "/Worktable_Base",
        Body + "/Worktable_Base/WB_Static",
        Body + "/Worktable_Base/WB_Spindle",
        Body + "/Worktable_Base/WB_XAxis_Drive",
        Body + "/Worktable_Base/WB_XAxis_Drive/WB_Clamp",
        Body + "/Worktable_Base/WB_YAxis_Drive",
        Body + "/Worktable_Base/WB_Hardware",
        Body + "/SpindleBase",
        Body + "/SpindleBase/SB_Static",
        Body + "/SpindleBase/SpindleMotor",
        Body + "/SpindleBase/SpindleMotor/SM_Static",
        Body + "/SpindleBase/SpindleMotor/SM_Rotating",
        Body + "/SpindleBase/SpindleMotor/SM_Hardware",
        Body + "/ToolChangeBody",
        Body + "/ToolChangeBody/TC_Static",
        Body + "/ToolChangeBody/TC_SwingArm",
        Body + "/ToolChangeBody/TC_SwingArm/TC_Carousel",
        Body + "/ToolChangeBody/TC_Hardware",
        Cabinet,
        Cabinet + "/Cabinet_Hardware",
        Enclosure,
        Enclosure + "/Enclosure_Frame",
        Enclosure + "/SlidingPanel",
        Enclosure + "/SlidingPanel/SlidingPanel_Moving",
        Enclosure + "/doors",
        Unclassified,
    };

    // Applied to every assignment AFTER all passes: the passes still classify against the
    // PM8000-style labels (keeping spatial seeds, hints, and the knee hardware bucket tight),
    // then these merges relabel the destination group. PB_Saddle_* → SlidingPanel: review
    // showed every "saddle" part on this machine sits on the sliding-panel plane (z≈-0.27
    // vertical) — the PM8000 join filed the panel's track/strut mechanism as saddle; the real
    // XY slide mechanics live in the WB_ groups.
    private static readonly Dictionary<string, string> FinalRemap = new Dictionary<string, string>{
        {"PB_Column_Structure", "Enclosure_Frame"}, {"PB_Column_Static", "Enclosure_Frame"},
        {"PB_Knee_Table", Cabinet}, {"PB_Knee_Static", Cabinet},
        {"PB_Knee_Hardware", "Cabinet_Hardware"},
        {"PB_XY_Saddle", "SlidingPanel"}, {"PB_Saddle_Static", "SlidingPanel"},
        {"PB_Saddle_Hardware", "SlidingPanel"}, {"PB_Saddle_Moving", "SlidingPanel_Moving"},
    };

    // PM8000 group node name -> our group name. Nodes below a mapped name inherit the deepest one.
    private static readonly Dictionary<string, string> PmCanonical = new Dictionary<string, string>{
        {"ProMill8000Body", Body},
        {"PB_Column_Structure", "PB_Column_Structure"},
        {"PB_Column_Static", "PB_Column_Static"},
        {"doors", "doors"}, {"Door1", "doors"}, {"Door2", "doors"},
        {"PB_Knee_Table", "PB_Knee_Table"}, {"PB_Knee_Static", "PB_Knee_Static"}, {"PB_Knee_Hardware", "PB_Knee_Hardware"},
        {"PB_XY_Saddle", "PB_XY_Saddle"}, {"PB_Saddle_Static", "PB_Saddle_Static"},
        {"PB_Saddle_Moving", "PB_Saddle_Moving"}, {"PB_Saddle_Hardware", "PB_Saddle_Hardware"},
        {"SpindleBase", "SpindleBase"}, {"SB_Static", "SB_Static"},
        {"SpindleMotor", "SpindleMotor"}, {"SM_Static", "SM_Static"}, {"SM_Rotating", "SM_Rotating"}, {"SM_Hardware", "SM_Hardware"},
        {"ToolChangeBody", "ToolChangeBody"}, {"TC_Static", "TC_Static"}, {"TC_SwingArm", "TC_SwingArm"},
        {"TC_Carousel", "TC_Carousel"}, {"TC_Hardware", "TC_Hardware"},
        {"Worktable_Base", "Worktable_Base"}, {"WB_Static", "WB_Static"}, {"WB_Spindle", "WB_Spindle"},
        {"WB_XAxis_Drive", "WB_XAxis_Drive"}, {"WB_Clamp", "WB_Clamp"},
        {"WB_YAxis_Drive", "WB_YAxis_Drive"}, {"WB_Hardware", "WB_Hardware"},
    };

    // Keyed by Normalize(Decode(name)): the hand-renamed English nodes plus review-confirmed
    // overrides for parts the join/spatial passes provably misplace (2026-07-30 review).
    private static readonly Dictionary<string, string> ExplicitNames = new Dictionary<string, string>{
        {"MillCover", Enclosure}, {"LeftMillDoor", Enclosure}, {"RightMillDoor", Enclosure},
        {"RightMillDoorBoltedFrame", Enclosure}, {"LeftSidePanel", Enclosure}, {"RightSidePanel", Enclosure},
        {"RightSidePanelFrame", Enclosure}, {"RightSideSlidingPanel", "SlidingPanel_Moving"},
        {"BackShelf", Enclosure}, {"BackShelfPlate", Enclosure}, {"BackShelfHolder", Enclosure},
        {"FrontLeftFoot", Enclosure},
        {"Cabinet", Cabinet}, {"LeftCabinetDoor", Cabinet}, {"RightCabinetDoor", Cabinet},
        {"IO board", Cabinet}, {"ElectricalPlug", Cabinet}, {"ElectricalWallPlug", Cabinet}, {"ElectricalCable", Cabinet},
        // Gas struts that lift the sliding side panel — the PM8000 join files their namesakes
        // under PB_Saddle_Moving, but here they hang on the panel plane. The rods and rod-end
        // fittings get re-homed into SlidingPanel_Moving by Close_Sliding_Panel's region rule.
        {"气缸体 MA25×", "SlidingPanel"}, {"气缸接头MA25×", "SlidingPanel"}, {"活塞杆 MA25×", "SlidingPanel"},
        // Cabinet-mounted gear the keyword table misses: door hinges (one right-side instance
        // rides along), DIN rails, control transformer, servo drivers, power switch (romanized
        // "kaiguan"), contactor, cable gland.
        {"CL40-M5铰链", Cabinet},
        {"U型导轨13", Cabinet}, {"U型导轨14", Cabinet},
        {"JBK5-", Cabinet},
        {"KT270-H-20驱动", Cabinet}, {"YKB2404MA驱动", Cabinet},
        {"kaig", Cabinet}, {"kaigan", Cabinet},
        {"KJ-2", Cabinet}, {"WF16-TP", Cabinet},
        // Head-mounted, non-rotating parts that spatial matching drops into SM_Rotating.
        {"PL-L型螺纹二通", "SM_Static"}, {"绿色平头", "SM_Static"}, {"磁石开关", "SM_Static"},
        // Mill-door mechanism: sliding-door rollers and electromagnetic interlocks belong with
        // the doors group (they move/lock the doors); cabinet door locks belong with the cabinet.
        {"滚轮架", "doors"}, {"电磁门开", "doors"}, {"电磁门开关", "doors"},
        {"门锁", Cabinet}, {"门锁KX319B", Cabinet},
    };

    // Cabinet-interior electricals (decoded names). Keywords shared with PM8000 (door locks,
    // valves, cylinders, lube pump, LED...) are deliberately absent — the name-join places those.
    private static readonly (string Keyword, string Group)[] CjkKeywords = {
        ("继电器", Cabinet),   // relay
        ("断路器", Cabinet),   // circuit breaker
        ("开关电源", Cabinet), // switching PSU
        ("线槽", Cabinet),     // wire duct
        ("接线排", Cabinet),   // terminal strip
        ("端子", Cabinet),     // terminal
        ("插座", Cabinet),     // socket
        ("电容", Cabinet),     // capacitor
        ("大风扇", Cabinet),   // cabinet fan
        ("主控板", Cabinet),   // main control board
        ("控制盒", Cabinet),   // control box
        ("航插", Cabinet),     // aviation connector
        ("航空插", Cabinet),
        ("大急停", Cabinet),   // main E-stop
        ("按钮", Cabinet),     // push button
    };

    // Names that are semantic but position-ambiguous: spatially resolved within candidate groups.
    private static readonly (string Keyword, string[] Candidates)[] CjkHints = {
        ("伺服电机", new[]{ "WB_XAxis_Drive", "WB_YAxis_Drive", "SpindleMotor", "PB_Column_Static", "SM_Static" }), // axis servos
        ("导轨", new[]{ "PB_Column_Static", "PB_Saddle_Static", "WB_Static", "PB_Knee_Static" }), // guide rails
    };

    // Where spatially-resolved fasteners get filed, mirroring PM8000's per-assembly hardware merge.
    private static readonly Dictionary<string, string> HardwareBucket = new Dictionary<string, string>{
        {"PB_Knee_Static", "PB_Knee_Hardware"}, {"PB_Knee_Table", "PB_Knee_Hardware"},
        {"PB_Saddle_Static", "PB_Saddle_Hardware"}, {"PB_Saddle_Moving", "PB_Saddle_Hardware"}, {"PB_XY_Saddle", "PB_Saddle_Hardware"},
        {"WB_Static", "WB_Hardware"}, {"WB_Spindle", "WB_Hardware"}, {"WB_XAxis_Drive", "WB_Hardware"},
        {"WB_Clamp", "WB_Hardware"}, {"WB_YAxis_Drive", "WB_Hardware"}, {"Worktable_Base", "WB_Hardware"},
        {"SM_Static", "SM_Hardware"}, {"SM_Rotating", "SM_Hardware"}, {"SpindleMotor", "SM_Hardware"},
        {"TC_Static", "TC_Hardware"}, {"TC_SwingArm", "TC_Hardware"}, {"TC_Carousel", "TC_Hardware"}, {"ToolChangeBody", "TC_Hardware"},
    };

    // \X0 terminator is optional-and-lenient: many FBX names drop the trailing backslash or
    // truncate the run entirely (e.g. "\X2\592798CE6247\X0  usemtl ...", "KT270-H-20\X2\9A7152A8").
    // Requiring the full "\X0\" left ~45 CJK names undecoded, which dumped cabinet electricals
    // into PB_Knee_Static via the spatial fallback.
    private static readonly Regex X2Escape = new Regex(@"\\X2\\([0-9A-Fa-f]+)(?:\\X0\\?)?");
    private static readonly Regex X1Escape = new Regex(@"\\X\\([0-9A-Fa-f]{2})");
    private static readonly Regex GbFastener = new Regex(@"^GBT?\s?\d");
    private static readonly Regex MFastener = new Regex(@"^M$|^M\d+$|^M\d+[-\s]\d+\w*$");

    private class Assignment{
        public Transform T;
        public string Group;
        public string Pass;
        public float Distance = -1f;
    }

    private class Axis_Snapshot{
        public int Axis;
        public float Speed;
        public bool EnableLimits;
        public float MinOffset;
        public float MaxOffset;
    }

    [MenuItem("Training/9 Intellitek Classify - Report Only")]
    public static void Report_Only(){ Run(false); }

    [MenuItem("Training/9 Intellitek Classify - Rebuild")]
    public static void Rebuild(){ Run(true); }

    private static void Run(bool save){
        if (Application.isPlaying){
            Debug.LogError("Intellitek_Mill_Classifier: exit play mode first.");
            return;
        }

        GameObject baseAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BasePath);
        if (baseAsset == null){
            Debug.LogError($"Intellitek_Mill_Classifier: {BasePath} not found.");
            return;
        }

        Dictionary<string, string> pmMap = Build_PM8000_Name_Map(out List<string> ambiguous);

        // Carry root pose + drive tuning from the current asset (pre-bake variant or a
        // previously baked prefab) so re-runs don't lose hand-set values.
        GameObject current = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Dictionary<string, Axis_Snapshot> tuned = Snapshot_Axes(current);

        Scene preview = EditorSceneManager.NewPreviewScene();
        try{
            var root = (GameObject)PrefabUtility.InstantiatePrefab(baseAsset, preview);
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = RootName;
            if (current != null){
                root.transform.localPosition = current.transform.localPosition;
                root.transform.localRotation = current.transform.localRotation;
                root.transform.localScale = current.transform.localScale;
            }

            // The flat FBX children — snapshot (with order) before the scaffold is added.
            List<Transform> members = root.transform.Cast<Transform>().ToList();
            Dictionary<Transform, int> order = members.Select((t, i) => (t, i)).ToDictionary(p => p.t, p => p.i);

            Dictionary<string, Transform> groups = Build_Scaffold(root);

            var assigned = new Dictionary<Transform, Assignment>();
            var deferredFasteners = new List<Transform>();
            var deferredSpatial = new List<(Transform T, string[] Hint)>();

            void Assign(Transform t, string group, string pass, float distance = -1f){
                assigned[t] = new Assignment{ T = t, Group = group, Pass = pass, Distance = distance };
            }

            foreach (Transform m in members){
                string decoded = Decode(m.name);
                string key = Normalize(decoded);

                if (ExplicitNames.TryGetValue(key, out string explicitGroup)){ Assign(m, explicitGroup, "explicit"); continue; }
                if (m.name.StartsWith("Spring") || m.name.StartsWith("WideSpring")){ Assign(m, "TC_Carousel", "springs"); continue; }
                if (Is_Fastener(decoded)){ deferredFasteners.Add(m); continue; }

                var keyword = CjkKeywords.FirstOrDefault(k => decoded.Contains(k.Keyword));
                if (keyword.Keyword != null){ Assign(m, keyword.Group, "cjk"); continue; }

                var hint = CjkHints.FirstOrDefault(h => decoded.Contains(h.Keyword));
                if (hint.Keyword != null){ deferredSpatial.Add((m, hint.Candidates)); continue; }

                if (pmMap.TryGetValue(key, out string joinGroup)){ Assign(m, joinGroup, "pm-join"); continue; }

                deferredSpatial.Add((m, null));
            }

            // Spatial: geometry is world-baked (node transforms are identity), so only
            // Renderer.bounds carries position — transform.position is useless here.
            Bounds machine = default;
            bool machineSeeded = false;
            foreach (Transform m in members){
                if (!Try_Renderer_Bounds(m, out Bounds b)) continue;
                if (!machineSeeded){ machine = b; machineSeeded = true; }
                else machine.Encapsulate(b);
            }
            float diag = machine.size.magnitude;

            (Dictionary<string, Bounds> Bounds, Dictionary<string, List<Vector3>> Points) Seed(){
                var seededBounds = new Dictionary<string, Bounds>();
                var seededPoints = new Dictionary<string, List<Vector3>>();
                foreach (Assignment a in assigned.Values){
                    if (a.Group == Unclassified || !Try_Renderer_Bounds(a.T, out Bounds b)) continue;
                    if (seededBounds.TryGetValue(a.Group, out Bounds gb)){ gb.Encapsulate(b); seededBounds[a.Group] = gb; }
                    else seededBounds[a.Group] = b;
                    if (!seededPoints.TryGetValue(a.Group, out List<Vector3> pts))
                        seededPoints[a.Group] = pts = new List<Vector3>();
                    pts.Add(b.center);
                }
                return (seededBounds, seededPoints);
            }

            string Spatial_Resolve((Dictionary<string, Bounds> Bounds, Dictionary<string, List<Vector3>> Points) seeded,
                                   Transform t, string[] hint, out float distance){
                distance = -1f;
                if (!Try_Renderer_Bounds(t, out Bounds b)) return null;
                Vector3 c = b.center;

                List<string> candidates = hint?.Where(seeded.Points.ContainsKey).ToList();
                if (candidates == null || candidates.Count == 0) candidates = seeded.Points.Keys.ToList();

                // Containment only against compact groups — the shell groups' AABBs envelop the
                // whole machine and would drain everything.
                var containing = candidates
                    .Where(g => g != Enclosure && g != Cabinet)
                    .Where(g => { Bounds e = seeded.Bounds[g]; e.Expand(diag * 0.02f); return e.Contains(c); })
                    .ToList();
                if (containing.Count == 1){ distance = 0f; return containing[0]; }

                // Nearest already-assigned MEMBER, not group centroid — large groups (column,
                // shells) have centers far from their own edges, which starved the extremities.
                string best = null;
                float bestSqr = float.MaxValue;
                foreach (string g in candidates){
                    foreach (Vector3 p in seeded.Points[g]){
                        float d = (p - c).sqrMagnitude;
                        if (d < bestSqr){ bestSqr = d; best = g; }
                    }
                }
                distance = Mathf.Sqrt(bestSqr);
                return distance <= diag * 0.10f ? best : null;
            }

            var bounds = Seed();
            foreach ((Transform t, string[] hintCandidates) in deferredSpatial){
                string g = Spatial_Resolve(bounds, t, hintCandidates, out float d);
                if (g != null) Assign(t, g, "spatial", d);
                else Assign(t, Unclassified, "unmatched", d);
            }

            // Re-seed so fasteners see the spatially-enriched bounds, then file them into the
            // owning assembly's hardware bucket (PM8000 parity).
            bounds = Seed();
            foreach (Transform t in deferredFasteners){
                string g = Spatial_Resolve(bounds, t, null, out float d);
                if (g == null){ Assign(t, Unclassified, "unmatched", d); continue; }
                Assign(t, HardwareBucket.TryGetValue(g, out string bucket) ? bucket : g, "fastener", d);
            }

            foreach (Assignment a in assigned.Values)
                if (FinalRemap.TryGetValue(a.Group, out string merged))
                    a.Group = merged;

            // Physically parent members under their groups — canonical group order, original
            // sibling order within each group (stable diffs). Legal here because the instance
            // is fully unpacked; groups keep local identity so this is a world-space no-op.
            foreach (string groupName in ScaffoldPaths.Select(p => p.Split('/').Last())){
                foreach (Transform t in assigned.Values
                         .Where(a => a.Group == groupName)
                         .Select(a => a.T)
                         .OrderBy(t => order[t]))
                    t.SetParent(groups[groupName], true);
            }

            Close_Sliding_Panel(members, groups);

            // Axis-mapping guesses mirror PM8000 (machine X->world X, machine Y->world Z,
            // machine Z->world Y) but this model bakes a different orientation — verify with
            // "Training/9 Intellitek - Jog Drives" before trusting them. Carried values from
            // the previous asset win over the guesses.
            Ensure_Axis(groups["WB_XAxis_Drive"], MovementAxis.X, tuned);
            Ensure_Axis(groups["WB_YAxis_Drive"], MovementAxis.Z, tuned);
            Ensure_Axis(groups["SpindleMotor"], MovementAxis.Y, tuned);

            bool saved = false;
            if (save)
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out saved);

            Write_Report(assigned.Values.ToList(), ambiguous, members.Count, diag, save, saved);

            var passCounts = assigned.Values.GroupBy(a => a.Pass).ToDictionary(g => g.Key, g => g.Count());
            int Count(string pass) => passCounts.TryGetValue(pass, out int n) ? n : 0;
            Debug.Log($"Intellitek_Mill_Classifier: {(save ? (saved ? "BAKED" : "SAVE FAILED") : "dry run")} — " +
                      $"explicit {Count("explicit")}, springs {Count("springs")}, cjk {Count("cjk")}, " +
                      $"pm-join {Count("pm-join")}, spatial {Count("spatial")}, fasteners {Count("fastener")}, " +
                      $"unclassified {Count("unmatched")} / total {members.Count}. Report: {ReportPath}");
        }
        finally{
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    [MenuItem("Training/9 Intellitek - Spawn Classified Mill")]
    public static void Spawn_Classified_Mill(){
        GameObject existing = GameObject.Find(RootName);
        if (existing != null){
            var leaves = new HashSet<string>(ScaffoldPaths.Select(p => p.Split('/').Last()));
            string counts = string.Join("  ", existing.GetComponentsInChildren<Transform>(true)
                .Where(t => leaves.Contains(t.name))
                .Select(t => $"{t.name}:{t.childCount}"));
            Debug.Log($"Intellitek mill group child counts: {counts}");
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null){
            Debug.LogError($"Intellitek_Mill_Classifier: {PrefabPath} not found.");
            return;
        }

        if (Application.isPlaying)
            UnityEngine.Object.Instantiate(prefab, Vector3.zero, prefab.transform.rotation);
        else{
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = Vector3.zero;
        }

        Debug.Log("Intellitek_Mill_Classifier: spawned ClassifyIntellitekMill at origin. Invoke again for group counts.");
    }

    private static bool _jogOut;

    [MenuItem("Training/9 Intellitek - Jog Drives")]
    public static void Jog_Drives(){
        if (!Application.isPlaying){
            Debug.LogError("Intellitek_Mill_Classifier: Jog Drives is play-mode only.");
            return;
        }

        GameObject mill = GameObject.Find(RootName);
        if (mill == null){
            Debug.LogError("Intellitek_Mill_Classifier: no ClassifyIntellitekMill in scene — spawn the mill first.");
            return;
        }
        if (!Try_Renderer_Bounds(mill.transform, out Bounds b)){
            Debug.LogError("Intellitek_Mill_Classifier: mill has no renderers.");
            return;
        }

        float delta = b.size.magnitude * 0.03f;
        AxisMovement[] drives = mill.GetComponentsInChildren<AxisMovement>(true)
            .Where(a => a.name == "WB_XAxis_Drive" || a.name == "WB_YAxis_Drive" || a.name == "SpindleMotor")
            .ToArray();

        foreach (AxisMovement drive in drives){
            if (_jogOut) drive.ResetToOrigin();
            else drive.MoveBy(delta);
        }
        _jogOut = !_jogOut;

        Debug.Log($"Intellitek_Mill_Classifier: {(_jogOut ? $"jogged +{delta:F4}" : "reset to origin")} — " +
                  string.Join(", ", drives.Select(d => d.name)));
    }

    // Review pass: dumps a per-group listing (decoded names, centers, spatial-outlier flags)
    // and one isolated screenshot per populated group from two angles, so misgrouped parts
    // can be spotted by name or as visual islands. Reads the ASSET (current ground truth);
    // the temp instance and camera are destroyed afterwards — don't save the scene if it
    // shows as dirtied.
    [MenuItem("Training/9 Intellitek - Review Groups")]
    public static void Review_Groups(){
        if (Application.isPlaying){
            Debug.LogError("Intellitek_Mill_Classifier: exit play mode first.");
            return;
        }
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null){
            Debug.LogError($"Intellitek_Mill_Classifier: {PrefabPath} not found.");
            return;
        }

        var leaves = new HashSet<string>(ScaffoldPaths.Select(p => p.Split('/').Last()));
        var offset = new Vector3(0f, 500f, 0f); // clear of the real scene content

        var mill = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var camGo = new GameObject("Intellitek_Review_Camera");
        RenderTexture rt = null;
        try{
            mill.transform.position += offset;

            var byName = mill.GetComponentsInChildren<Transform>(true)
                .Where(t => leaves.Contains(t.name))
                .ToDictionary(t => t.name);
            List<(Transform Group, List<Transform> Parts)> groups = ScaffoldPaths
                .Select(p => p.Split('/').Last())
                .Select(n => (byName[n], byName[n].Cast<Transform>().Where(c => !leaves.Contains(c.name)).ToList()))
                .ToList();

            Try_Renderer_Bounds(mill.transform, out Bounds machine);
            float diag = machine.size.magnitude;

            Directory.CreateDirectory(ReviewDir);

            // --- listing with nearest-neighbor outlier flags ---
            var sb = new StringBuilder();
            sb.AppendLine("# IntellitekMill Group Review Listing");
            sb.AppendLine();
            sb.AppendLine($"Machine diagonal {diag:F3}. Centers are root-relative. ⚠ = nearest same-group");
            sb.AppendLine("part is farther than 4% of the diagonal (spatial outlier candidate).");
            int flagged = 0;
            foreach ((Transform group, List<Transform> parts) in groups){
                sb.AppendLine();
                sb.AppendLine($"## {Group_Path(group, mill.transform)} ({parts.Count} parts)");
                var centers = parts.Select(p => Try_Renderer_Bounds(p, out Bounds b) ? (Vector3?)b.center : null).ToList();
                for (int i = 0; i < parts.Count; i++){
                    string decoded = Decode(parts[i].name);
                    string line = $"- {decoded}";
                    if (centers[i].HasValue){
                        Vector3 c = centers[i].Value - offset;
                        line += $"  [c=({c.x:F2},{c.y:F2},{c.z:F2})]";
                        float nearest = float.MaxValue;
                        for (int j = 0; j < parts.Count; j++)
                            if (j != i && centers[j].HasValue)
                                nearest = Mathf.Min(nearest, (centers[j].Value - centers[i].Value).magnitude);
                        if (nearest != float.MaxValue && nearest > diag * 0.04f){
                            line += $"  ⚠ {nearest / diag * 100f:F1}%";
                            flagged++;
                        }
                    }
                    sb.AppendLine(line);
                }
            }
            File.WriteAllText(Path.Combine(ReviewDir, "Group_Listing.md"), sb.ToString(), new UTF8Encoding(false));

            // --- isolated screenshots, fixed framing so position-in-frame = position-on-machine ---
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.15f, 0.15f, 0.18f);
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = diag * 6f;
            rt = new RenderTexture(1280, 960, 24);

            void Aim(Vector3 dir){
                cam.transform.position = machine.center + dir.normalized * diag * 1.15f;
                cam.transform.LookAt(machine.center);
            }
            void Shoot(string name){
                Aim(new Vector3(1f, 0.7f, -1f));
                Render_To_File(cam, rt, Path.Combine(ReviewDir, name + "_A.png"));
                Aim(new Vector3(-1f, 0.7f, 1f));
                Render_To_File(cam, rt, Path.Combine(ReviewDir, name + "_B.png"));
            }

            Shoot("_all");

            foreach ((Transform _, List<Transform> parts) in groups)
                foreach (Transform p in parts)
                    p.gameObject.SetActive(false);
            foreach ((Transform group, List<Transform> parts) in groups){
                if (parts.Count == 0) continue;
                foreach (Transform p in parts) p.gameObject.SetActive(true);
                Shoot(group.name);
                foreach (Transform p in parts) p.gameObject.SetActive(false);
            }

            Debug.Log($"Intellitek_Mill_Classifier: review written to {ReviewDir} — listing + shots for " +
                      $"{groups.Count(g => g.Parts.Count > 0)} groups, {flagged} spatial outlier flags.");
        }
        finally{
            if (rt != null){ rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
            UnityEngine.Object.DestroyImmediate(camGo);
            UnityEngine.Object.DestroyImmediate(mill);
        }
    }

    private const string ReviewDir = "Logs/IntellitekReview";

    private static string Group_Path(Transform group, Transform root){
        string path = group.name;
        for (Transform t = group.parent; t != null && t != root; t = t.parent)
            path = t.name + "/" + path;
        return path;
    }

    private static void Render_To_File(Camera cam, RenderTexture rt, string path){
        var request = new UniversalRenderPipeline.SingleCameraRequest{ destination = rt };
        if (RenderPipeline.SupportsRenderRequest(cam, request))
            RenderPipeline.SubmitRenderRequest(cam, request);
        else{
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
        }

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = previous;
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
    }

    // The CAD assembly exports the right-side sliding panel raised (service position), which
    // reads as a plate floating above the machine. Two jobs: (1) everything that travels with
    // the panel — panel, strut rods, rod-end fittings/brackets/nuts — is reparented into
    // SlidingPanel_Moving so the whole moving set can be animated as one; (2) the set is then
    // dropped by the frame-to-panel offset so the panel bakes closed and the rods retract
    // into their cylinders. Only the MA25×7xx struts belong to the panel — WB_Clamp has
    // unrelated MA40 cylinder parts. Geometry is world-baked: plain position offsets.
    private static void Close_Sliding_Panel(List<Transform> members, Dictionary<string, Transform> groups){
        Transform panel = null, frame = null;
        var movers = new HashSet<Transform>();

        // The machine tops out at y≈0.93, so anything above y=1.0 is the raised assembly:
        // the panel, the extended strut rods, and the rod-end brackets/nuts/washers (generic
        // fastener names — a name match can't catch them). The rod-end fittings sit lower.
        foreach (Transform m in members){
            if (m.name == "RightSideSlidingPanel") panel = m;
            else if (m.name == "RightSidePanelFrame"){ frame = m; continue; }
            if (!Try_Renderer_Bounds(m, out Bounds b)) continue;
            if (b.center.y > 1.0f) movers.Add(m);
            else if (b.center.y > 0.6f && Decode(m.name).StartsWith("气缸接头MA25×7"))
                movers.Add(m); // upper (rod-end) fittings ride with the panel; lower anchors stay
        }

        if (panel == null || frame == null ||
            !Try_Renderer_Bounds(panel, out Bounds pb) || !Try_Renderer_Bounds(frame, out Bounds fb)){
            Debug.LogWarning("Intellitek_Mill_Classifier: sliding panel or its frame not found — panel left as exported.");
            return;
        }

        movers.Add(panel);
        Transform moving = groups["SlidingPanel_Moving"];
        foreach (Transform t in movers)
            if (t.parent != moving)
                t.SetParent(moving, true);

        float deltaY = fb.center.y - pb.center.y;
        if (Mathf.Abs(deltaY) < 0.05f)
            return; // FBX already exports it closed

        foreach (Transform t in movers)
            t.position += new Vector3(0f, deltaY, 0f);

        Debug.Log($"Intellitek_Mill_Classifier: closed sliding panel — grouped and moved {movers.Count} parts by {deltaY:F3} in Y.");
    }

    // Always creates fresh groups — never adopts an FBX node that happens to share a name.
    private static Dictionary<string, Transform> Build_Scaffold(GameObject root){
        var groups = new Dictionary<string, Transform>();

        foreach (string path in ScaffoldPaths){
            string[] parts = path.Split('/');
            Transform parent = parts.Length == 1 ? root.transform : groups[parts[parts.Length - 2]];
            var group = new GameObject(parts[parts.Length - 1]).transform;
            group.SetParent(parent, false);
            groups[parts[parts.Length - 1]] = group;
        }

        return groups;
    }

    private static Dictionary<string, Axis_Snapshot> Snapshot_Axes(GameObject asset){
        var snapshots = new Dictionary<string, Axis_Snapshot>();
        if (asset == null)
            return snapshots;

        foreach (AxisMovement drive in asset.GetComponentsInChildren<AxisMovement>(true)){
            var s = new SerializedObject(drive);
            snapshots[drive.name] = new Axis_Snapshot{
                Axis = s.FindProperty("axis").enumValueIndex,
                Speed = s.FindProperty("speed").floatValue,
                EnableLimits = s.FindProperty("enableLimits").boolValue,
                MinOffset = s.FindProperty("minOffset").floatValue,
                MaxOffset = s.FindProperty("maxOffset").floatValue,
            };
            if (s.FindProperty("dependents").arraySize > 0)
                Debug.LogWarning($"Intellitek_Mill_Classifier: {drive.name} has dependents — those references are not carried across a rebuild, re-add them by hand.");
        }

        return snapshots;
    }

    private static void Ensure_Axis(Transform group, MovementAxis axisGuess, Dictionary<string, Axis_Snapshot> tuned){
        var movement = group.gameObject.AddComponent<AxisMovement>();
        var serialized = new SerializedObject(movement);
        if (tuned.TryGetValue(group.name, out Axis_Snapshot s)){
            serialized.FindProperty("axis").enumValueIndex = s.Axis;
            serialized.FindProperty("speed").floatValue = s.Speed;
            serialized.FindProperty("enableLimits").boolValue = s.EnableLimits;
            serialized.FindProperty("minOffset").floatValue = s.MinOffset;
            serialized.FindProperty("maxOffset").floatValue = s.MaxOffset;
        }
        else{
            serialized.FindProperty("axis").enumValueIndex = (int)axisGuess;
            serialized.FindProperty("speed").floatValue = 0.5f;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Dictionary<string, string> Build_PM8000_Name_Map(out List<string> ambiguous){
        var map = new Dictionary<string, string>();
        var conflicts = new Dictionary<string, HashSet<string>>();

        GameObject pm = AssetDatabase.LoadAssetAtPath<GameObject>(PM8000Path);
        if (pm == null)
            throw new InvalidOperationException($"{PM8000Path} not found.");

        void Walk(Transform node, string group){
            string plain = Regex.Replace(node.name, @"\.\d{1,3}$", "");
            if (plain == "MillController")
                return;

            if (PmCanonical.TryGetValue(plain, out string mapped)){
                group = mapped;
            }
            else if (group != null){
                string key = Normalize(Decode(node.name));
                if (key.Length > 0){
                    if (map.TryGetValue(key, out string previous) && previous != group){
                        if (!conflicts.TryGetValue(key, out HashSet<string> set))
                            conflicts[key] = set = new HashSet<string>{ previous };
                        set.Add(group);
                    }
                    else{
                        map[key] = group;
                    }
                }
            }

            foreach (Transform child in node)
                Walk(child, group);
        }

        foreach (Transform child in pm.transform)
            Walk(child, null);

        foreach (string key in conflicts.Keys)
            map.Remove(key);

        ambiguous = conflicts.Select(c => $"{c.Key} → {string.Join(" / ", c.Value)}").OrderBy(s => s).ToList();
        return map;
    }

    // FBX names carry STEP escapes: \X2\<UTF-16BE hex>\X0\ runs and \X\hh single bytes.
    private static string Decode(string raw){
        string s = X2Escape.Replace(raw, match => {
            string hex = match.Groups[1].Value;
            var sb = new StringBuilder();
            for (int i = 0; i + 4 <= hex.Length; i += 4)
                sb.Append((char)ushort.Parse(hex.Substring(i, 4), NumberStyles.HexNumber));
            return sb.ToString();
        });
        return X1Escape.Replace(s, match => ((char)byte.Parse(match.Groups[1].Value, NumberStyles.HexNumber)).ToString());
    }

    // Strip Blender duplicate suffixes (.001 and letter-adjacent 001), OBJ usemtl leakage.
    private static string Normalize(string decoded){
        string n = decoded.Trim();
        n = Regex.Replace(n, @"\s+usemtl\s+\S+$", "", RegexOptions.IgnoreCase);
        n = Regex.Replace(n, @"\.\d{1,3}$", "");
        n = Regex.Replace(n, @"(?<=\D)\d{3}$", "");
        return n.Trim();
    }

    private static bool Is_Fastener(string decoded){
        string n = decoded.Trim();
        if (GbFastener.IsMatch(n) || MFastener.IsMatch(n))
            return true;
        return n.Contains("螺钉") || n.Contains("螺母") || n.Contains("垫圈");
    }

    private static bool Try_Renderer_Bounds(Transform t, out Bounds bounds){
        Renderer[] renderers = t.GetComponentsInChildren<Renderer>(true);
        bounds = default;
        if (renderers.Length == 0)
            return false;

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return true;
    }

    private static void Write_Report(List<Assignment> assignments, List<string> ambiguous, int total, float diag, bool save, bool saved){
        var sb = new StringBuilder();
        sb.AppendLine("# IntellitekMill Classification Report");
        sb.AppendLine();
        sb.AppendLine($"Generated {DateTime.Now:yyyy-MM-dd HH:mm} — mode: {(save ? (saved ? "bake (saved)" : "bake (SAVE FAILED)") : "report only (no save)")}.");
        sb.AppendLine($"Members classified: {assignments.Count}/{total}. Machine bounds diagonal: {diag:F4}.");
        sb.AppendLine();
        sb.AppendLine("The prefab is a fully-unpacked regular prefab — the group hierarchy is editor-visible.");
        sb.AppendLine("FBX mesh/material edits flow automatically; new/renamed/deleted FBX nodes and");
        sb.AppendLine("IntellitekMill.prefab component changes require re-running Rebuild.");
        sb.AppendLine();
        sb.AppendLine("Axis-mapping guesses on WB_XAxis_Drive (X), WB_YAxis_Drive (Z), SpindleMotor (Y) are");
        sb.AppendLine("UNVERIFIED — run `Training/9 Intellitek - Jog Drives` in play mode and correct the axis");
        sb.AppendLine("fields in the inspector (hand-set values survive re-runs).");
        sb.AppendLine();
        sb.AppendLine("Do not translate/rotate Enclosure or ElectronicsCabinet at runtime while door physics is");
        sb.AppendLine("live — the door joints are world-anchored. Show/hide is safe.");
        sb.AppendLine();

        sb.AppendLine("## Per-pass counts");
        sb.AppendLine();
        foreach (var pass in assignments.GroupBy(a => a.Pass).OrderByDescending(g => g.Count()))
            sb.AppendLine($"- {pass.Key}: {pass.Count()}");
        sb.AppendLine();

        sb.AppendLine("## Per-group counts");
        sb.AppendLine();
        foreach (var group in assignments.GroupBy(a => a.Group).OrderByDescending(g => g.Count()))
            sb.AppendLine($"- {group.Key}: {group.Count()}");
        sb.AppendLine();

        sb.AppendLine("## Spatial assignment distances (fraction of machine diagonal)");
        sb.AppendLine();
        var spatial = assignments.Where(a => (a.Pass == "spatial" || a.Pass == "fastener") && a.Distance > 0f).ToList();
        if (spatial.Count == 0){
            sb.AppendLine("(none — all spatial assignments were containment hits or there were none)");
        }
        else{
            foreach (var bucket in spatial.GroupBy(a => Mathf.Min(10, Mathf.FloorToInt(a.Distance / diag * 100f))).OrderBy(g => g.Key))
                sb.AppendLine($"- {bucket.Key}–{bucket.Key + 1}%: {bucket.Count()}");
        }
        sb.AppendLine();

        sb.AppendLine("## Ambiguous PM8000 names (dropped from the name-join, resolved spatially)");
        sb.AppendLine();
        if (ambiguous.Count == 0) sb.AppendLine("(none)");
        foreach (string entry in ambiguous)
            sb.AppendLine($"- {entry}");
        sb.AppendLine();

        sb.AppendLine("## Unclassified");
        sb.AppendLine();
        var unmatched = assignments.Where(a => a.Group == Unclassified).OrderBy(a => a.T.name).ToList();
        if (unmatched.Count == 0) sb.AppendLine("(none)");
        foreach (Assignment a in unmatched)
            sb.AppendLine($"- {a.T.name}  (decoded: {Decode(a.T.name)}, nearest {(a.Distance < 0f ? "n/a" : (a.Distance / diag * 100f).ToString("F1") + "%")})");

        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(ReportPath, sb.ToString(), new UTF8Encoding(false));
    }
}
