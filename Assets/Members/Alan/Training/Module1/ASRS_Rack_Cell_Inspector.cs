using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// What one storage cell holds right now, read from Item_ASRS.TableMap (the
// rack's own inventory) — never inferred from what happens to be visible.
public struct ASRS_Cell_Info
{
    public bool Valid;
    public string TableId;      // the cell's address, e.g. "010003"
    public int Row, Col;
    public int Number;          // 1-72
    public bool Occupied;
    public string StoredTableId; // TableID of the template sitting there (may differ from the address)
    public string Material;     // null when empty or the template holds nothing
}

// Lets the trainee point at any of the rack's 72 storage cells and see its
// address, occupancy and stored material — one shared panel and one shared
// highlight, plus a lightweight trigger target per cell built at the rack's
// saved slot anchors the first time it's needed.
//
// Steps (matched on Required Action Id):
//   "M1_InspectCells"      inspect N different cells to continue.
//   "M1_IdentifyMaterial"  find a cell storing the named material. The
//                          material is read at runtime from the cell named
//                          by Identify Reference Table Id; any cell holding
//                          the same material counts as correct.
// The quiz borrows the same targets for its rack-cell question through
// BeginExternalPick / EndExternalPick.
//
// Read-only: selecting cells never changes TableMap or moves anything.
public class ASRS_Rack_Cell_Inspector : ASRS_Module1_Step_Feature
{
    [Serializable]
    public class MaterialNote
    {
        [Tooltip("Matches the stored item's Item_Parent.Name (e.g. \"Epoxy Block\").")]
        public string materialName;
        [TextArea(2, 3)] public string description;
    }

    private enum Mode { None, Inspect, Identify, ExternalPick, Load }

    [Header("Rack")]
    [SerializeField] private Item_ASRS rack;

    [Header("Steps")]
    [SerializeField] private string inspectActionId = "M1_InspectCells";
    [SerializeField] private string identifyActionId = "M1_IdentifyMaterial";
    [SerializeField, Min(1)] private int requiredInspections = 3;
    [Tooltip("The material the trainee must find is whatever this cell stores at runtime. If it's empty, the first occupied cell is used instead (and a warning is logged).")]
    [SerializeField] private string identifyReferenceTableId = "010001";

    [Header("Load the ASRS (pointer insert)")]
    [Tooltip("On this step the trainee can point at the loaded slotted table to put it in their inventory, then point at its home cell to load it — only possible while the inventory holds a slotted table. Carrying it in by hand still works too.")]
    [SerializeField] private string loadActionId = "LoadASRS";
    [Tooltip("The loose lesson table. Empty = the table on the step that waits for 'LoadSlottedTable' (Required Loaded Slotted Table).")]
    [SerializeField] private Item_Slotted_Table lessonTable;
    [SerializeField] private string loadTableStepActionId = "LoadSlottedTable";

    [Header("Materials")]
    [SerializeField] private MaterialNote[] materialNotes = new MaterialNote[0];

    [Header("Cell targets")]
    [Tooltip("Used only if no rack slotted table has a BoxCollider to copy the cell size from.")]
    [SerializeField] private Vector3 fallbackCellSize = new Vector3(0.2f, 0.06f, 0.2f);
    [Tooltip("Targets are this much larger than a template, so the interact ray hits the cell before the template inside it.")]
    [SerializeField, Min(1f)] private float targetPadding = 1.15f;

    [Header("Highlight")]
    [Tooltip("Material for the selected-cell box, e.g. the markers' Green_Glass. Also the base for the faint cell outlines and the pointing box (tinted copies).")]
    [SerializeField] private Material highlightMaterial;

    [Header("Pointing")]
    [Tooltip("Show a faint box on every selectable cell while selection is on, so it's obvious what can be picked.")]
    [SerializeField] private bool showAllCells = true;
    [SerializeField, Range(0f, 1f)] private float allCellsAlpha = 0.07f;
    [Tooltip("Colour of the box on the cell a hand (or the screen centre, on desktop) is pointing at.")]
    [SerializeField] private Color pointingColor = new Color(0.38f, 0.85f, 1f, 0.45f);
    [SerializeField, Min(0.5f)] private float maxPointDistance = 10f;

    [Header("Panel")]
    [Tooltip("The panel moved next to the selected cell. Defaults to Root.")]
    [SerializeField] private Transform panel;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text cellText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button closeButton;
    [SerializeField] private float panelHeightAboveCell = 0.22f;
    [SerializeField] private float panelPullTowardViewer = 0.15f;

    private readonly ASRS_Module1_UI.Debouncer debouncer = new ASRS_Module1_UI.Debouncer();
    private readonly List<ASRS_Rack_Cell_Target> targets = new List<ASRS_Rack_Cell_Target>();
    private readonly HashSet<string> inspected = new HashSet<string>();

    private GameObject targetContainer;
    private Transform highlight;
    private ASRS_Rack_Cell_Target selected;
    private Mode mode;
    private bool completionReported;
    private string identifyMaterial;
    private Action<ASRS_Cell_Info> externalPickCallback;
    private string externalPrompt;

    // Own pointer: same buttons the project's select uses (index trigger on
    // either controller, left mouse), but it looks along the ray for the
    // nearest CELL rather than stopping at the first collider — the rack's
    // glass, door and frame sit in front of every cell.
    private InputAction leftTrigger, rightTrigger, mouseClick;
    private Transform leftHand, rightHand;
    private readonly RaycastHit[] rayHits = new RaycastHit[32];
    private Mesh cubeMesh;
    private Material allCellsMaterial, pointingMaterial;
    private Transform leftPointBox, rightPointBox;
    private ASRS_Rack_Cell_Target hoveredByLeft, hoveredByRight;

    protected override string LogTag => ASRS_Module1_Log.Cells;

    public Item_ASRS Rack => rack;
    public string InspectActionId => inspectActionId;
    public string IdentifyActionId => identifyActionId;
    public string IdentifyReferenceTableId => identifyReferenceTableId;
    public MaterialNote[] MaterialNotes => materialNotes;
    public int TargetCount => targets.Count;

    protected override void Awake()
    {
        base.Awake();
        if (panel == null && root != null)
            panel = root.transform;

        ASRS_Module1_UI.Bind(closeButton, debouncer, ClearSelection);

        Player_Controller player = FindFirstObjectByType<Player_Controller>();
        if (player != null)
        {
            leftHand = player.LeftHand != null ? player.LeftHand.transform : null;
            rightHand = player.RightHand != null ? player.RightHand.transform : null;
        }

        leftTrigger = new InputAction("M1 Cell Select Left", InputActionType.Button, "<XRController>{LeftHand}/triggerPressed");
        rightTrigger = new InputAction("M1 Cell Select Right", InputActionType.Button, "<XRController>{RightHand}/triggerPressed");
        mouseClick = new InputAction("M1 Cell Select Mouse", InputActionType.Button, "<Mouse>/leftButton");
        leftTrigger.performed += _ => SelectAlong(HandRay(leftHand));
        rightTrigger.performed += _ => SelectAlong(HandRay(rightHand));
        mouseClick.performed += _ => { if (!XRActive) SelectAlong(ScreenCentreRay()); };
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        leftTrigger?.Dispose();
        rightTrigger?.Dispose();
        mouseClick?.Dispose();
        if (allCellsMaterial != null) Destroy(allCellsMaterial);
        if (pointingMaterial != null) Destroy(pointingMaterial);
    }

    private static bool XRActive => UnityEngine.XR.XRSettings.isDeviceActive;

    // Only while cells are selectable: show which cell each hand points at.
    private void Update()
    {
        if (targetContainer == null || !targetContainer.activeSelf)
            return;

        if (XRActive)
        {
            hoveredByLeft = PointBox(leftPointBox, Pick(HandRay(leftHand)));
            hoveredByRight = PointBox(rightPointBox, Pick(HandRay(rightHand)));
        }
        else
        {
            hoveredByLeft = PointBox(leftPointBox, Pick(ScreenCentreRay()));
            hoveredByRight = PointBox(rightPointBox, null);
        }

        // With nothing selected, say what's under the pointer.
        if (selected == null)
        {
            ASRS_Rack_Cell_Target hovered = hoveredByLeft != null ? hoveredByLeft : hoveredByRight;
            string text;
            if (mode == Mode.Load)
                text = hovered == null ? "Aim at the slotted table or an outlined empty cell — what you point at lights up blue."
                     : hovered == tableTarget ? "Pointing at the loaded <b>slotted table</b> — pull the trigger to put it in your inventory."
                     : $"Pointing at empty cell <b>{hovered.TableId}</b> — pull the trigger to load the template here.";
            else
                text = hovered != null
                    ? $"Pointing at cell <b>{hovered.TableId}</b> — pull the trigger to select it."
                    : "No cell selected. Aim at the rack — the cell you're pointing at lights up blue.";
            ASRS_Module1_UI.SetText(cellText, text);
        }
    }

    private Ray? HandRay(Transform hand)
    {
        return hand != null ? new Ray(hand.position, hand.forward) : (Ray?)null;
    }

    private Ray? ScreenCentreRay()
    {
        Camera viewer = Camera.main;
        return viewer != null ? new Ray(viewer.transform.position, viewer.transform.forward) : (Ray?)null;
    }

    // Nearest cell along the ray, ignoring everything that isn't a cell.
    private ASRS_Rack_Cell_Target Pick(Ray? ray)
    {
        if (ray == null || targetContainer == null || !targetContainer.activeSelf)
            return null;

        int count = Physics.RaycastNonAlloc(ray.Value, rayHits, maxPointDistance, ~0, QueryTriggerInteraction.Collide);
        ASRS_Rack_Cell_Target best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            if (rayHits[i].distance >= bestDistance)
                continue;

            ASRS_Rack_Cell_Target cell = rayHits[i].collider.GetComponent<ASRS_Rack_Cell_Target>();
            if (cell == null || cell.Inspector != this)
                continue;

            best = cell;
            bestDistance = rayHits[i].distance;
        }

        return best;
    }

    private void SelectAlong(Ray? ray)
    {
        ASRS_Rack_Cell_Target cell = Pick(ray);
        if (cell == null || !debouncer.Accept())
            return;

        if (mode == Mode.Load)
            HandleLoadPick(cell);
        else
            SelectCell(cell);
    }

    private ASRS_Rack_Cell_Target PointBox(Transform box, ASRS_Rack_Cell_Target cell)
    {
        if (box == null)
            return cell;

        bool show = cell != null && cell != selected;
        if (show)
            PlaceBox(box, cell, 1.04f);
        box.gameObject.SetActive(show);
        return cell;
    }

    public override bool HandlesAction(string actionId)
    {
        return actionId == inspectActionId || actionId == identifyActionId || actionId == loadActionId;
    }

    protected override void OnFeatureEnter(string actionId)
    {
        if (!EnsureTargets())
            return;

        completionReported = false;
        inspected.Clear();
        ClearSelection();
        SetTargetsActive(true);
        PlacePanelOverRack();

        if (actionId == inspectActionId)
        {
            mode = Mode.Inspect;
            ASRS_Module1_UI.SetText(feedbackText, "");
            RefreshInspectPrompt();
            ASRS_Module1_Log.Info(LogTag, $"Cell inspection started — {requiredInspections} distinct cell(s) required.", this);
            return;
        }

        if (actionId == loadActionId)
        {
            BeginLoad();
            return;
        }

        mode = Mode.Identify;
        if (!TryResolveMaterial(identifyReferenceTableId, out identifyMaterial, out string source))
        {
            // No stored material anywhere means the rack failed to load its
            // inventory — a scene problem the validator reports. Don't trap
            // the trainee on an exercise that can't be answered.
            ASRS_Module1_Log.Error(LogTag, "No stored materials found in the rack — skipping the identification exercise. Check Item_ASRS MaterialList / EpoxyBlockPrefab.", this);
            completionReported = true;
            CompleteStep(identifyActionId);
            return;
        }

        ASRS_Module1_UI.SetText(promptText, $"Find a cell that stores: <b>{identifyMaterial}</b>");
        ASRS_Module1_UI.SetText(feedbackText, "Aim your hand's ray at a cell and pull the index-finger trigger to check it.");
        ASRS_Module1_Log.Info(LogTag, $"Identification exercise: find '{identifyMaterial}' (reference cell {source}).", this);
    }

    protected override void OnFeatureExit(string actionId)
    {
        if (mode == Mode.Load)
            EndLoad();

        mode = Mode.None;
        ClearSelection();
        SetTargetsActive(false);
    }

    // ---------------------------------------------- Load the ASRS (pointer)

    private ASRS_Rack_Cell_Target tableTarget;   // pick-up box around the loose lesson table
    private Item_Slotted_Table stowedTable;      // the lesson table while it's "in the inventory"
    private GameObject snapshotHolder;           // inactive copy, in case the table is destroyed some other way
    private Item_Slotted_Table snapshotTable;

    private const string InventoryHint = "Point at the loaded slotted table and pull the trigger to put it in your inventory.";

    private void BeginLoad()
    {
        mode = Mode.Load;
        stowedTable = null;

        if (lessonTable == null)
            lessonTable = FindLessonTable();

        if (lessonTable == null)
        {
            ASRS_Module1_Log.Warn(LogTag, "No loose slotted table found (needs Pickup on, not inside the rack, ideally TableID 05000x) — pointer loading is unavailable; carrying the table in by hand still works.", this);
            ASRS_Module1_UI.SetText(promptText, "Carry the slotted table to its empty cell in the rack.");
            SetTargetsActive(false);
            return;
        }

        // Only empty cells are offered as destinations.
        foreach (ASRS_Rack_Cell_Target cell in targets)
            cell.gameObject.SetActive(!GetCellInfo(cell.TableId).Occupied);

        TakeSnapshot();
        ShowTableTarget(lessonTable.gameObject.activeInHierarchy && !IsInRack(lessonTable));
        RefreshLoadPrompt();
        ASRS_Module1_UI.SetText(feedbackText, "");
        ASRS_Module1_Log.Info(LogTag, $"Load step: lesson table '{lessonTable.name}' (home cell {lessonTable.TableID}); slotted tables in inventory: {InventoryCount()}.", this);
    }

    private void EndLoad()
    {
        foreach (ASRS_Rack_Cell_Target cell in targets)
            cell.gameObject.SetActive(true);

        ShowTableTarget(false);

        // A table still "in the inventory" when the step ends (it shouldn't
        // be — the step only ends by loading it) comes back where it was.
        if (stowedTable != null)
            stowedTable.gameObject.SetActive(true);
        stowedTable = null;

        if (snapshotHolder != null)
            Destroy(snapshotHolder);
        snapshotHolder = null;
        snapshotTable = null;
    }

    private void HandleLoadPick(ASRS_Rack_Cell_Target target)
    {
        if (target == tableTarget)
        {
            PickUpLessonTable();
            return;
        }

        string tableName = lessonTable != null ? lessonTable.Name : "Slotted Table";
        if (InventoryCount() < 1)
        {
            ASRS_Module1_UI.SetText(feedbackText, $"<color=#FFB366>You need a slotted table in your inventory first.</color> {InventoryHint}");
            return;
        }

        string home = lessonTable != null ? lessonTable.TableID : null;
        if (target.TableId != home)
        {
            ASRS_Module1_UI.SetText(feedbackText, $"<color=#FFB366>Cell {target.TableId} isn't this template's home — its RFID/TableID is {home}.</color>");
            ASRS_Module1_Log.Info(LogTag, $"Load: rejected cell {target.TableId} (table belongs in {home}).", this);
            return;
        }

        Item_Slotted_Table table = TableToLoad();
        if (table == null)
        {
            ASRS_Module1_Log.Error(LogTag, "Load: no lesson table instance to place (destroyed with no snapshot).", this);
            ASRS_Module1_UI.SetText(feedbackText, "<color=#FF8866>The slotted table couldn't be found — see the Console.</color>");
            return;
        }

        table.transform.SetParent(null, true);
        table.gameObject.SetActive(true);

        if (!rack.NeedsRackReturn(table))
        {
            ASRS_Module1_UI.SetText(feedbackText, $"<color=#FFB366>Cell {home} is already occupied.</color>");
            return;
        }

        rack.SlotInsert(table);
        if (!IsInRack(table))
        {
            ASRS_Module1_Log.Warn(LogTag, $"Load: SlotInsert didn't record table {table.TableID} in TableMap — step not completed.", this);
            return;
        }

        if (table.TryGetComponent(out Rigidbody body))
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
        }

        Inventory inventory = PlayerInventory();
        inventory.RemoveFromInventory(tableName, 1);
        stowedTable = null;

        ShowTableTarget(false);
        foreach (ASRS_Rack_Cell_Target cell in targets)
            cell.gameObject.SetActive(false);
        ShowHighlight(target);

        ASRS_Module1_UI.SetText(feedbackText, $"<color=#66FF88>Loaded — the template is stored in cell {home}.</color>");
        ASRS_Module1_Log.Info(LogTag, $"Load: table {home} taken from the inventory and confirmed in the rack.", this);

        if (!completionReported)
        {
            completionReported = true;
            CompleteStepAfterHold(loadActionId);
        }
    }

    private void PickUpLessonTable()
    {
        Inventory inventory = PlayerInventory();
        if (inventory == null || lessonTable == null)
        {
            ASRS_Module1_UI.SetText(feedbackText, "<color=#FF8866>No player inventory found — see the Console.</color>");
            ASRS_Module1_Log.Error(LogTag, "Load: no Entity_Player/Inventory in the scene.", this);
            return;
        }

        inventory.AddToInventory(lessonTable.Name, 1);
        stowedTable = lessonTable;
        lessonTable.gameObject.SetActive(false); // kept, not destroyed — it keeps its TableID and the block on it
        ShowTableTarget(false);
        RefreshLoadPrompt();
        ASRS_Module1_UI.SetText(feedbackText, $"<color=#66FF88>Slotted table added to your inventory.</color>");
        ASRS_Module1_Log.Info(LogTag, $"Load: lesson table '{lessonTable.TableID}' put in the inventory ({InventoryCount()} now).", this);
    }

    // The same table the trainee loaded, wherever it is now.
    private Item_Slotted_Table TableToLoad()
    {
        if (stowedTable != null)
            return stowedTable;

        if (lessonTable != null && !IsInRack(lessonTable))
            return lessonTable; // still in the world; the inventory slot came from elsewhere

        // Picked up some other way (e.g. Interact, which destroys the object):
        // bring back the copy taken when this step began.
        return snapshotTable;
    }

    private void RefreshLoadPrompt()
    {
        string home = lessonTable != null ? lessonTable.TableID : "?";
        int count = InventoryCount();
        ASRS_Module1_UI.SetText(promptText, count < 1
            ? $"Slotted tables in inventory: 0.  {InventoryHint}"
            : $"Slotted tables in inventory: {count}.  Point at the template's home cell <b>{home}</b> (outlined) and pull the trigger to load it.");
    }

    private void TakeSnapshot()
    {
        if (snapshotHolder != null || lessonTable == null)
            return;

        // Copied under an inactive holder so the copy never wakes up (no
        // Start, no physics) unless it's actually needed.
        snapshotHolder = new GameObject("LessonTableSnapshot");
        snapshotHolder.SetActive(false);
        snapshotHolder.transform.SetParent(transform, false);
        GameObject copy = Instantiate(lessonTable.gameObject, lessonTable.transform.position, lessonTable.transform.rotation, snapshotHolder.transform);
        copy.name = lessonTable.name;
        snapshotTable = copy.GetComponent<Item_Slotted_Table>();
    }

    private void ShowTableTarget(bool show)
    {
        if (!show)
        {
            if (tableTarget != null) tableTarget.gameObject.SetActive(false);
            return;
        }

        if (tableTarget == null)
        {
            GameObject go = new GameObject("LessonTableTarget") { layer = 0 };
            go.transform.SetParent(targetContainer.transform, false);
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            tableTarget = go.AddComponent<ASRS_Rack_Cell_Target>();
            tableTarget.Init(this, "");
        }

        // World-aligned box around the table and whatever sits on it.
        Bounds bounds = new Bounds(lessonTable.transform.position, Vector3.zero);
        foreach (Renderer r in lessonTable.GetComponentsInChildren<Renderer>())
            if (r.enabled && !(r is ParticleSystemRenderer))
                bounds.Encapsulate(r.bounds);

        Transform t = tableTarget.transform;
        t.SetPositionAndRotation(bounds.center, Quaternion.identity);
        t.localScale = DivideScale(Vector3.one, targetContainer.transform.lossyScale);
        BoxCollider collider = tableTarget.GetComponent<BoxCollider>();
        collider.center = Vector3.zero;
        collider.size = Vector3.Max(bounds.size * 1.15f, Vector3.one * 0.08f);
        tableTarget.gameObject.SetActive(true);
    }

    // 1. the table on the "load the epoxy block" step, if that step exists;
    // 2. otherwise the loose, carryable slotted table in the scene (not one
    //    of the rack's own), preferring one whose home is in the vacant row.
    public Item_Slotted_Table FindLessonTable()
    {
        if (sequenceManager != null && sequenceManager.Steps != null)
            foreach (SequenceManager.Step step in sequenceManager.Steps)
                if (step.requiredActionId == loadTableStepActionId && step.RequiredLoadedSlottedTable != null)
                    return step.RequiredLoadedSlottedTable;

        return FindLooseTable(rack);
    }

    public static Item_Slotted_Table FindLooseTable(Item_ASRS rack)
    {
        Item_Slotted_Table best = null;
        foreach (Item_Slotted_Table table in FindObjectsByType<Item_Slotted_Table>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!table.Pickup || table.GetComponentInParent<Item_ASRS>(true) != null)
                continue;

            bool vacantHome = ASRS_Rack_Addressing.TryParse(table.TableID, out int row, out _) && row == ASRS_Rack_Addressing.VacantRow;
            if (vacantHome)
                return table;

            if (best == null)
                best = table;
        }

        return best;
    }

    private bool IsInRack(Item_Slotted_Table table)
    {
        if (table == null || rack == null || !int.TryParse(table.TableID, out int raw))
            return false;

        int index = ASRS_Rack_Addressing.ToIndex(raw);
        return index >= 0 && rack.TableMap.TryGetValue(index, out Item_Slotted_Table stored) && stored == table;
    }

    private static Inventory PlayerInventory()
    {
        Entity_Player player = FindFirstObjectByType<Entity_Player>();
        Inventory inventory = player != null ? player.InventoryReference : null;
        if (inventory != null && inventory.StaticInventory == null)
            inventory.StaticInventory = new List<KeyValuePair<string, int>>(); // not loaded from a save yet
        return inventory;
    }

    private int InventoryCount()
    {
        Inventory inventory = PlayerInventory();
        return inventory != null ? inventory.Find(lessonTable != null ? lessonTable.Name : "Slotted Table") : 0;
    }

    // Read straight from the rack's inventory — no caching, so it's always
    // current after an insert or retrieve.
    public ASRS_Cell_Info GetCellInfo(string tableId)
    {
        ASRS_Cell_Info info = new ASRS_Cell_Info { TableId = tableId };

        if (rack == null || !ASRS_Rack_Addressing.TryParse(tableId, out info.Row, out info.Col))
            return info;

        int index = ASRS_Rack_Addressing.ToIndex(ASRS_Rack_Addressing.ToRawId(info.Row, info.Col));
        info.Valid = true;
        info.Number = index + 1;

        if (rack.TableMap.TryGetValue(index, out Item_Slotted_Table table) && table != null)
        {
            info.Occupied = true;
            info.StoredTableId = table.TableID;
            info.Material = MaterialNameOf(table.Item);
        }

        return info;
    }

    // The material stored at 'preferredTableId', or the first occupied
    // cell's material (in 1-72 order) if that cell is empty.
    public bool TryResolveMaterial(string preferredTableId, out string material, out string sourceTableId)
    {
        ASRS_Cell_Info preferred = GetCellInfo(preferredTableId);
        if (!string.IsNullOrEmpty(preferred.Material))
        {
            material = preferred.Material;
            sourceTableId = preferredTableId;
            return true;
        }

        ASRS_Module1_Log.Warn(LogTag, $"Reference cell {preferredTableId} holds no material — falling back to the first occupied cell.", this);

        foreach (string id in ASRS_Rack_Addressing.AllTableIds())
        {
            ASRS_Cell_Info info = GetCellInfo(id);
            if (string.IsNullOrEmpty(info.Material))
                continue;

            material = info.Material;
            sourceTableId = id;
            return true;
        }

        material = null;
        sourceTableId = null;
        return false;
    }

    public string DescribeMaterial(string material)
    {
        if (string.IsNullOrEmpty(material) || materialNotes == null)
            return null;

        foreach (MaterialNote note in materialNotes)
            if (note != null && string.Equals(note.materialName, material, StringComparison.OrdinalIgnoreCase))
                return note.description;

        return null;
    }

    public static bool SameMaterial(string a, string b)
    {
        return !string.IsNullOrEmpty(a) && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

    // Lets the quiz use the cell targets on its own step. The callback runs
    // once per selection; the quiz ends it with EndExternalPick().
    public bool BeginExternalPick(string prompt, Action<ASRS_Cell_Info> onPicked)
    {
        if (!EnsureTargets())
            return false;

        mode = Mode.ExternalPick;
        externalPrompt = prompt;
        externalPickCallback = onPicked;
        ClearSelection();
        SetTargetsActive(true);
        if (root != null) root.SetActive(true);
        PlacePanelOverRack();
        ASRS_Module1_UI.SetText(promptText, prompt);
        ASRS_Module1_UI.SetText(feedbackText, "");
        return true;
    }

    public void EndExternalPick()
    {
        if (mode != Mode.ExternalPick)
            return;

        mode = Mode.None;
        externalPickCallback = null;
        ClearSelection();
        SetTargetsActive(false);
        if (root != null && !IsShowing) root.SetActive(false);
    }

    public void SelectCell(ASRS_Rack_Cell_Target target)
    {
        if (target == null || mode == Mode.None)
            return;

        // Re-selecting the open cell closes it.
        if (target == selected && mode == Mode.Inspect)
        {
            ClearSelection();
            return;
        }

        selected = target;
        ASRS_Cell_Info info = GetCellInfo(target.TableId);
        ShowHighlight(target);
        PlacePanelAt(target);
        ASRS_Module1_UI.SetText(cellText, FormatCell(info));
        ASRS_Module1_Log.Info(LogTag, $"Selected cell {info.TableId} (#{info.Number}) — {(info.Occupied ? "occupied, " + (info.Material ?? "no material") : "vacant")}.", target);

        switch (mode)
        {
            case Mode.Inspect:
                inspected.Add(info.TableId);
                RefreshInspectPrompt();
                if (!completionReported && inspected.Count >= requiredInspections)
                {
                    completionReported = true;
                    ASRS_Module1_UI.SetText(feedbackText, "<color=#66FF88>Nice — you've inspected enough cells.</color>");
                    CompleteStepAfterHold(inspectActionId);
                }
                break;

            case Mode.Identify:
                if (completionReported)
                    break;

                if (SameMaterial(info.Material, identifyMaterial))
                {
                    completionReported = true;
                    ASRS_Module1_UI.SetText(feedbackText, $"<color=#66FF88>Correct — cell {info.TableId} stores {info.Material}.</color>");
                    ASRS_Module1_Log.Info(LogTag, $"Identification correct at {info.TableId}.", this);
                    CompleteStepAfterHold(identifyActionId);
                }
                else
                {
                    string holds = !info.Occupied ? "is empty" : info.Material != null ? $"holds {info.Material}" : "holds an empty template";
                    ASRS_Module1_UI.SetText(feedbackText, $"<color=#FFB366>Not that one — cell {info.TableId} {holds}. Look for {identifyMaterial}.</color>");
                    ASRS_Module1_Log.Info(LogTag, $"Identification wrong at {info.TableId} ({holds}).", this);
                }
                break;

            case Mode.ExternalPick:
                externalPickCallback?.Invoke(info);
                break;
        }
    }

    public void ClearSelection()
    {
        selected = null;
        if (highlight != null) highlight.gameObject.SetActive(false);
        ASRS_Module1_UI.SetText(cellText, "No cell selected.");
    }

    public void ShowFeedback(string text) => ASRS_Module1_UI.SetText(feedbackText, text);

    private void RefreshInspectPrompt()
    {
        ASRS_Module1_UI.SetText(promptText, $"Aim your hand's ray at a storage cell (the ray turns solid white) and pull the index-finger trigger.  ({Mathf.Min(inspected.Count, requiredInspections)} of {requiredInspections} inspected)");
    }

    private string FormatCell(ASRS_Cell_Info info)
    {
        if (!info.Valid)
            return $"Cell {info.TableId}: not a valid rack address.";

        string text = $"<b>Cell {info.TableId}</b>   #{info.Number} of {ASRS_Rack_Addressing.CellCount}   ·   {ASRS_Rack_Addressing.SideName(info.Row)}\n" +
                      $"Row {info.Row}, column {info.Col}\n";

        if (!info.Occupied)
            return text + "Status: <color=#FFB366>Vacant</color> — no template stored here.";

        text += "Status: <color=#66FF88>Occupied</color>\n" + $"Template: {info.StoredTableId}\n";

        if (info.Material == null)
            return text + "Material: none (empty template)";

        string description = DescribeMaterial(info.Material);
        return text + $"Material: <b>{info.Material}</b>" + (string.IsNullOrEmpty(description) ? "" : $"\n<size=85%>{description}</size>");
    }

    private static string MaterialNameOf(GameObject item)
    {
        if (item == null)
            return null;

        Item_Parent part = item.GetComponent<Item_Parent>();
        if (part != null && !string.IsNullOrWhiteSpace(part.Name))
            return part.Name;

        return item.name.Replace("(Clone)", "").Trim();
    }

    private bool EnsureTargets()
    {
        if (targetContainer != null)
            return true;

        if (rack == null)
        {
            ASRS_Module1_Log.Error(LogTag, "No Item_ASRS assigned — cell inspection is unavailable.", this);
            return false;
        }

        // Copy the template's own collider box so each target matches the
        // real cell; anchors are template positions/rotations saved by
        // Item_ASRS.Start(), so the box sits exactly on the slot.
        Vector3 center = Vector3.zero;
        Vector3 size = fallbackCellSize;
        Vector3 scale = Vector3.one;
        Quaternion templateRotation = rack.transform.rotation;
        foreach (Item_Slotted_Table table in rack.GetComponentsInChildren<Item_Slotted_Table>(true))
        {
            if (!table.TryGetComponent(out BoxCollider box))
                continue;

            center = box.center;
            size = box.size;
            scale = table.transform.lossyScale;
            templateRotation = table.transform.rotation;
            break;
        }

        size *= targetPadding;
        GrowToRowHeight(ref center, ref size, scale, templateRotation);

        targetContainer = new GameObject("RackCellTargets");
        targetContainer.transform.SetParent(transform, false);
        targetContainer.SetActive(false);

        int missing = 0;
        foreach (string id in ASRS_Rack_Addressing.AllTableIds())
        {
            if (!rack.TryGetSlotAnchor(int.Parse(id), out Vector3 position, out Quaternion rotation))
            {
                missing++;
                continue;
            }

            GameObject go = new GameObject("Cell_" + id) { layer = 0 }; // Default: the interact raycast's mask
            go.transform.SetParent(targetContainer.transform, false);
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = DivideScale(scale, targetContainer.transform.lossyScale);

            BoxCollider trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true; // never blocks a carried template or the arm
            trigger.center = center;
            trigger.size = size;

            ASRS_Rack_Cell_Target target = go.AddComponent<ASRS_Rack_Cell_Target>();
            target.Init(this, id);
            targets.Add(target);

            if (showAllCells && AllCellsMaterial() != null)
            {
                // Faint outline so every selectable cell is visible.
                Transform outline = MakeBox("Outline", go.transform, AllCellsMaterial());
                outline.localPosition = center;
                outline.localRotation = Quaternion.identity;
                outline.localScale = size;
            }
        }

        if (missing > 0)
            ASRS_Module1_Log.Warn(LogTag, $"{missing} cell(s) have no saved anchor in Item_ASRS — they can't be inspected.", rack);

        ASRS_Module1_Log.Info(LogTag, $"Built {targets.Count} cell target(s) on '{rack.name}'.", this);
        return targets.Count > 0;
    }

    // A template's own collider is thin; the part standing on it is taller,
    // and a ray aimed at the part would hit the part (not selectable) first.
    // Grow the target upward to ~90% of the rack's row pitch, measured from
    // the saved anchors of 010001 and 020001, along whichever of the
    // template's local axes points most nearly up. The bottom stays put.
    private void GrowToRowHeight(ref Vector3 center, ref Vector3 size, Vector3 scale, Quaternion rotation)
    {
        if (!rack.TryGetSlotAnchor(10001, out Vector3 row1, out _) || !rack.TryGetSlotAnchor(20001, out Vector3 row2, out _))
            return;

        int upAxis = 0;
        float best = 0f, sign = 1f;
        for (int axis = 0; axis < 3; axis++)
        {
            Vector3 local = Vector3.zero;
            local[axis] = 1f;
            float d = Vector3.Dot(rotation * local, Vector3.up);
            if (Mathf.Abs(d) > best)
            {
                best = Mathf.Abs(d);
                upAxis = axis;
                sign = Mathf.Sign(d);
            }
        }

        float axisScale = Mathf.Abs(scale[upAxis]) > 0.00001f ? Mathf.Abs(scale[upAxis]) : 1f;
        float wanted = Mathf.Abs(row2.y - row1.y) * 0.9f / axisScale;
        if (wanted <= size[upAxis])
            return;

        center[upAxis] += sign * (wanted - size[upAxis]) * 0.5f;
        size[upAxis] = wanted;
    }

    private void SetTargetsActive(bool active)
    {
        if (targetContainer != null)
            targetContainer.SetActive(active);

        if (active)
        {
            if (leftPointBox == null && PointingMaterial() != null)
            {
                leftPointBox = MakeBox("PointingBox_Left", transform, PointingMaterial());
                rightPointBox = MakeBox("PointingBox_Right", transform, PointingMaterial());
            }

            leftTrigger.Enable();
            rightTrigger.Enable();
            mouseClick.Enable();
        }
        else
        {
            leftTrigger?.Disable();
            rightTrigger?.Disable();
            mouseClick?.Disable();
            hoveredByLeft = hoveredByRight = null;
            if (leftPointBox != null) leftPointBox.gameObject.SetActive(false);
            if (rightPointBox != null) rightPointBox.gameObject.SetActive(false);
        }
    }

    private void ShowHighlight(ASRS_Rack_Cell_Target target)
    {
        if (highlight == null)
        {
            if (highlightMaterial == null)
                ASRS_Module1_Log.Warn(LogTag, "No highlight material assigned — using the default material.", this);
            highlight = MakeBox("SelectedCellHighlight", transform, highlightMaterial);
        }

        PlaceBox(highlight, target, 1f);
        highlight.gameObject.SetActive(true);
    }

    // Fits 'box' (a child of this inspector) over a cell's trigger volume.
    private void PlaceBox(Transform box, ASRS_Rack_Cell_Target target, float grow)
    {
        BoxCollider cellBox = target.GetComponent<BoxCollider>();
        Transform t = target.transform;
        box.SetPositionAndRotation(t.TransformPoint(cellBox.center), t.rotation);
        box.localScale = DivideScale(Vector3.Scale(t.lossyScale, cellBox.size) * grow, box.parent.lossyScale);
    }

    // A visual-only cube (no collider, no shadows).
    private Transform MakeBox(string boxName, Transform parent, Material material)
    {
        if (cubeMesh == null)
        {
            GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            primitive.GetComponent<Collider>().enabled = false;
            cubeMesh = primitive.GetComponent<MeshFilter>().sharedMesh;
            Destroy(primitive);
        }

        GameObject go = new GameObject(boxName, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.GetComponent<MeshFilter>().sharedMesh = cubeMesh;

        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        if (material != null)
            renderer.sharedMaterial = material;

        go.SetActive(false);
        go.SetActive(parent != transform); // outlines show with their cell; the others when placed
        return go.transform;
    }

    private Material AllCellsMaterial()
    {
        if (allCellsMaterial == null && highlightMaterial != null)
            allCellsMaterial = TintedCopy(highlightMaterial, new Color(1f, 1f, 1f, allCellsAlpha), "M1 Cell Outline");
        return allCellsMaterial;
    }

    private Material PointingMaterial()
    {
        if (pointingMaterial == null && highlightMaterial != null)
            pointingMaterial = TintedCopy(highlightMaterial, pointingColor, "M1 Cell Pointing");
        return pointingMaterial;
    }

    // A runtime copy of the (transparent URP) highlight material in another
    // colour — one shared copy per look, not one per cell.
    private static Material TintedCopy(Material source, Color color, string copyName)
    {
        Material copy = new Material(source) { name = copyName };
        if (copy.HasProperty("_BaseColor")) copy.SetColor("_BaseColor", color);
        if (copy.HasProperty("_Color")) copy.SetColor("_Color", color);
        return copy;
    }

    private void PlacePanelAt(ASRS_Rack_Cell_Target target)
    {
        BoxCollider box = target.GetComponent<BoxCollider>();
        PlacePanel(target.transform.TransformPoint(box.center) + Vector3.up * panelHeightAboveCell);
    }

    private void PlacePanelOverRack()
    {
        if (targets.Count == 0)
            return;

        Bounds bounds = new Bounds(targets[0].transform.position, Vector3.zero);
        foreach (ASRS_Rack_Cell_Target target in targets)
            bounds.Encapsulate(target.transform.position);

        PlacePanel(new Vector3(bounds.center.x, bounds.max.y + panelHeightAboveCell, bounds.center.z), 0.4f);
    }

    // Faces the panel toward the viewer (yaw only) and nudges it their way
    // so it isn't buried inside the rack.
    private void PlacePanel(Vector3 position, float extraPull = 0f)
    {
        if (panel == null)
            return;

        Camera viewer = Camera.main;
        if (viewer == null)
        {
            panel.position = position;
            return;
        }

        Vector3 away = position - viewer.transform.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f)
            away = viewer.transform.forward;

        away.Normalize();
        panel.position = position - away * (panelPullTowardViewer + extraPull);
        panel.rotation = Quaternion.LookRotation(away, Vector3.up);
    }

    private static Vector3 DivideScale(Vector3 a, Vector3 b)
    {
        return new Vector3(
            Mathf.Approximately(b.x, 0f) ? a.x : a.x / b.x,
            Mathf.Approximately(b.y, 0f) ? a.y : a.y / b.y,
            Mathf.Approximately(b.z, 0f) ? a.z : a.z / b.z);
    }
}
