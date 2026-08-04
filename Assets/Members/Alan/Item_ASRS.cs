using System;
using System.Collections.Generic;
using UnityEngine;

public class Item_ASRS : Item_Parent
{
    public RACK_TASK task;
    public Item_Slotted_Table item = null;
    public Item_Epoxy_Block material = null;
    public Spline_Animate splineAnimate;
    public Item_Conveyor_Belt conveyor;

    [Tooltip("Default/fallback material prefab. Used for any row with no entry in MaterialList (or once MaterialList runs out).")]
    public GameObject EpoxyBlockPrefab;

    [Tooltip("Materials loaded onto the rack by row: index 0 -> row 1, index 1 -> row 2, etc. " +
             "A row past the end of this list, or with a blank entry, falls back to EpoxyBlockPrefab.")]
    public List<GameObject> MaterialList = new List<GameObject>();

    [Header("Rack Generation")]
    [Tooltip("Item_Slotted_Table prefab (e.g. SlottedTableSingleBuffer) instantiated for every rack slot by GenerateRackTables(). Only used when the rack has no slotted tables yet.")]
    public GameObject SlottedTablePrefab;

    [Tooltip("Local-space position of slot 1 (row 1, col 1), relative to this ASRS transform.")]
    public Vector3 RackOrigin = Vector3.zero;

    [Tooltip("Local-space offset applied per column, left-to-right.")]
    public Vector3 ColumnStep = new Vector3(0.5f, 0f, 0f);

    [Tooltip("Local-space offset applied per row, bottom-to-top.")]
    public Vector3 RowStep = new Vector3(0f, 0.3f, 0f);

    [Tooltip("Local rotation (Euler) applied to every generated table.")]
    public Vector3 TableEulerAngles = Vector3.zero;

    // Matches the row/col bounds enforced in GetIndex().
    private const int RackRows = 12;
    private const int RackCols = 6;

    public Dictionary<int, Item_Slotted_Table> TableMap = new Dictionary<int, Item_Slotted_Table>();
	// public List<Machine_Job> Jobs = new List<Machine_Job>();
	public List<Item_Slotted_Table> TableList = new List<Item_Slotted_Table>();

    // Reverse index: material GameObject -> the rack slot its table currently
    // occupies. Kept in sync wherever TableMap itself changes, so "where is
    // this material" is an O(1) lookup instead of scanning every slot.
    private readonly Dictionary<GameObject, int> materialLocations = new Dictionary<GameObject, int>();

    private readonly Dictionary<int, Vector3> anchorPositions = new Dictionary<int, Vector3>();
    private readonly Dictionary<int, Quaternion> anchorRotations = new Dictionary<int, Quaternion>();

    public Item_ASRS()
    {
        Name = "ASRS";
        Pickup = false;
        Quantity = 1;
    }

    public override void Start()
    {
        base.Start();

        anchorPositions.Clear();
        anchorRotations.Clear();
        TableMap.Clear();
        materialLocations.Clear();

        if (GetComponentsInChildren<Item_Slotted_Table>(true).Length == 0)
        {
            GenerateRackTables();
        }

        if (conveyor == null)
        {
            Debug.LogError("ASRS: conveyor reference is not assigned.", this);
        }

        HashSet<string> conveyorTableIDs = new HashSet<string>();
        List<GameObject> conveyorList = conveyor != null ? conveyor.GetSlottedTableList() : new List<GameObject>();

        foreach (GameObject obj in conveyorList)
        {
            if (obj == null)
                continue;

            Item_Slotted_Table table = obj.GetComponent<Item_Slotted_Table>();
            if (table == null)
                continue;

            if (!string.IsNullOrWhiteSpace(table.TableID))
                conveyorTableIDs.Add(table.TableID);
        }

        foreach (Item_Slotted_Table table in GetComponentsInChildren<Item_Slotted_Table>(true))
        {
            if (table == null || string.IsNullOrWhiteSpace(table.TableID))
                continue;

            if (!int.TryParse(table.TableID, out int rawTableID))
            {
                Debug.LogWarning($"Invalid TableID: {table.TableID}", table);
                continue;
            }

            int index;
            try
            {
                index = GetIndex(rawTableID);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message, table);
                continue;
            }

            int row = rawTableID / 10000;
            bool isOnConveyor = conveyorTableIDs.Contains(table.TableID);
            bool isVacantRow = row == 5;

            anchorPositions[index] = table.transform.position;
            anchorRotations[index] = table.transform.rotation;

            if (isVacantRow)
            {
                TableMap[index] = null;
                table.Item = null;

                SetTableVisibility(table.gameObject, false);
            }
            else if (isOnConveyor)
            {
                TableMap[index] = null;

                SetTableVisibility(table.gameObject, false);
            }
            else
            {
                TableMap[index] = table;

                if (table.Item != null)
                    materialLocations[table.Item] = index;

                SetTableVisibility(table.gameObject, true);
            }
        }

        LoadEpoxyBlocks();
    }

    private void SetTableVisibility(GameObject obj, bool isVisible)
    {
        MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer r in renderers)
        {
            r.enabled = isVisible;
        }
    }

    public override void Interact(Entity_Player PlayerReference)
    {
        base.Interact(PlayerReference);
    }

    public override void AlternateInteract(Entity_Player PlayerReference)
    {
    }

    // O(1) reverse lookup: given a material's GameObject, find the rack slot
    // index and the Item_Slotted_Table it's currently sitting on (if any).
    public bool TryFindMaterial(GameObject material, out Item_Slotted_Table table, out int slotIndex)
    {
        table = null;
        slotIndex = -1;

        if (material == null || !materialLocations.TryGetValue(material, out slotIndex))
            return false;

        return TableMap.TryGetValue(slotIndex, out table) && table != null;
    }

    // Builds the full 12x6 rack of Item_Slotted_Table instances from SlottedTablePrefab,
    // laid out from RackOrigin using ColumnStep/RowStep. TableID follows the same
    // row*10000+col scheme GetIndex() parses, so the tables it creates slot straight
    // into TableMap once Start() scans them.
    [ContextMenu("Generate Rack Tables")]
    public void GenerateRackTables()
    {
        if (SlottedTablePrefab == null)
        {
            Debug.LogError("Item_ASRS: SlottedTablePrefab is not assigned.", this);
            return;
        }

        for (int row = 1; row <= RackRows; row++)
        {
            for (int col = 1; col <= RackCols; col++)
            {
                int tableId = row * 10000 + col;

                GameObject instance = Instantiate(SlottedTablePrefab, transform);
                instance.name = $"SlottedTable_{tableId:D6}";
                instance.transform.SetLocalPositionAndRotation(
                    RackOrigin + ColumnStep * (col - 1) + RowStep * (row - 1),
                    Quaternion.Euler(TableEulerAngles));

                if (!instance.TryGetComponent(out Item_Slotted_Table table))
                {
                    Debug.LogError("Item_ASRS: SlottedTablePrefab has no Item_Slotted_Table component.", instance);
                    continue;
                }

                table.TableID = tableId.ToString();
            }
        }

        Debug.Log($"Item_ASRS: Generated {RackRows * RackCols} rack tables.");
    }

    // Instantiates a material onto every currently-empty table, then hands it to
    // Item_Slotted_Table.SetItem() — reusing its existing anchor-snap, collider,
    // and kinematic/grab-listener setup instead of duplicating it here.
    public void LoadEpoxyBlocks()
    {
        if (EpoxyBlockPrefab == null)
        {
            Debug.LogError("Item_ASRS: EpoxyBlockPrefab is not assigned.", this);
            return;
        }

        foreach (var kvp in TableMap)
        {
            Item_Slotted_Table table = kvp.Value;

            if (table == null || table.Item != null || table.AnchorPoint == null)
                continue;

            GameObject block = Instantiate(GetMaterialForTable(table));
            table.Item = block;
            table.SetItem();

            materialLocations[block] = kvp.Key;
        }
    }

    // Row 1 draws MaterialList[0], row 2 draws MaterialList[1], etc. Falls back to
    // EpoxyBlockPrefab once the row runs past the list, or where an entry was left blank.
    private GameObject GetMaterialForTable(Item_Slotted_Table table)
    {
        if (!string.IsNullOrWhiteSpace(table.TableID) && int.TryParse(table.TableID, out int rawTableID))
        {
            int rowIndex = (rawTableID / 10000) - 1;

            if (rowIndex >= 0 && rowIndex < MaterialList.Count && MaterialList[rowIndex] != null)
                return MaterialList[rowIndex];
        }

        return EpoxyBlockPrefab;
    }

    private int CountVacantSlots()
    {
        int vacant = 0;

        foreach (var kvp in TableMap)
        {
            if (kvp.Value == null)
                vacant++;
        }

        return vacant;
    }

    public Item_Slotted_Table SlotRetrieve(Item_Slotted_Table requestTable)
    {
        if (!TryGetTableIndex(requestTable, out int index))
            return null;

        if (!TableMap.TryGetValue(index, out Item_Slotted_Table storedTable) || storedTable == null)
        {
            Debug.LogError("No table in rack slot to retrieve.");
            return null;
        }

        TableMap[index] = null;

        if (storedTable.Item != null)
            materialLocations.Remove(storedTable.Item);

        storedTable.gameObject.SetActive(true);
        SetTableVisibility(storedTable.gameObject, true);

        Spline_Animate splineAnimate = storedTable.GetComponent<Spline_Animate>();
        if (splineAnimate == null)
            splineAnimate = storedTable.GetComponentInParent<Spline_Animate>();

        if (splineAnimate != null)
        {
            splineAnimate.enabled = true;
        }

        Debug.Log($"Retrieved table {storedTable.TableID} from rack.");

        return storedTable;
    }

    public void SlotInsert(Item_Slotted_Table target)
    {
        if (target == null)
        {
            Debug.LogError("SlotInsert: target table is null.");
            return;
        }

        if (!TryGetTableIndex(target, out int index))
            return;

        if (TableMap.TryGetValue(index, out Item_Slotted_Table occupied) && occupied != null)
        {
            Debug.LogWarning($"SlotInsert: Rack slot at index {index} is already occupied.", target);
            return;
        }

        if (!anchorPositions.TryGetValue(index, out Vector3 anchorPosition) ||
            !anchorRotations.TryGetValue(index, out Quaternion anchorRotation))
        {
            Debug.LogError($"SlotInsert: Missing saved anchor transform for index {index}.", target);
            return;
        }

        target.transform.SetParent(transform, true);
        target.transform.position = anchorPosition;
        target.transform.rotation = anchorRotation;

        Spline_Animate splineAnimate = target.GetComponent<Spline_Animate>();
        if (splineAnimate == null)
            splineAnimate = target.GetComponentInParent<Spline_Animate>();

        if (splineAnimate != null)
        {
            splineAnimate.Pause();
            splineAnimate.Container = null;
            splineAnimate.enabled = false;
        }

        TableMap[index] = target;

        if (target.Item != null)
            materialLocations[target.Item] = index;

        target.gameObject.SetActive(true);
        SetTableVisibility(target.gameObject, true);

        Debug.Log($"SlotInsert: Table '{target.TableID}' placed into rack slot {index}.");
    }

    public bool NeedsRackReturn(Item_Slotted_Table target)
    {
        if (target == null)
            return false;

        if (!TryGetTableIndex(target, out int index))
            return false;

        return !TableMap.TryGetValue(index, out Item_Slotted_Table occupied) || occupied == null;
    }

    public int GetIndex(int tableId)
    {
        int row = tableId / 10000;
        int col = tableId % 10;

        if (row < 1 || row > 12)
            throw new ArgumentOutOfRangeException(nameof(tableId), $"Row value {row} is out of range. Must be 1-12.");

        if (col < 1 || col > 6)
            throw new ArgumentOutOfRangeException(nameof(tableId), $"Column value {col} is out of range. Must be 1-6.");

        return (row - 1) * 6 + (col - 1);
    }

    private bool TryGetTableIndex(Item_Slotted_Table table, out int index)
    {
        index = -1;

        if (table == null || string.IsNullOrWhiteSpace(table.TableID))
        {
            Debug.LogError("ASRS: Table is null or missing TableID.", table);
            return false;
        }

        if (!int.TryParse(table.TableID, out int rawTableId))
        {
            Debug.LogError($"ASRS: Could not parse TableID '{table.TableID}'.", table);
            return false;
        }

        try
        {
            index = GetIndex(rawTableId);
            return true;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Debug.LogError($"ASRS: Invalid TableID '{table.TableID}'. {ex.Message}", table);
            return false;
        }
    }

    public Item_Slotted_Table RetrieveByID(string tableID)
    {
        if (string.IsNullOrWhiteSpace(tableID))
            return null;

        int index = GetIndex(int.Parse(tableID));

        if (!TableMap.TryGetValue(index, out Item_Slotted_Table storedTable) || storedTable == null)
        {
            Debug.LogWarning($"ASRS: No table available for {tableID}");
            return null;
        }

        TableMap[index] = null;

        if (storedTable.Item != null)
            materialLocations.Remove(storedTable.Item);

        storedTable.gameObject.SetActive(true);
        SetTableVisibility(storedTable.gameObject, true);

        Spline_Animate splineAnimate = storedTable.GetComponent<Spline_Animate>();
        if (splineAnimate == null)
            splineAnimate = storedTable.GetComponentInParent<Spline_Animate>();

        if (splineAnimate != null)
        {
            splineAnimate.enabled = true;
        }

        Debug.Log($"ASRS: Retrieved table '{storedTable.TableID}'");

        return storedTable;
    }
}
