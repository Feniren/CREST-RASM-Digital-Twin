using System.Collections;
using UnityEngine;

public enum RACK_TASK
{
    RETRIEVE,
    INSERT
}

public class RackScanner : MonoBehaviour
{
    [SerializeField] private Item_ASRS rack;
    [SerializeField] private float processDelay = 1.5f;

    public Item_Slotted_Table ActiveTable { get; private set; }
    public bool IsProcessing { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"RackScanner: Trigger entered by '{other.name}'.", other.gameObject);

        if (rack == null)
        {
            Debug.LogError("RackScanner: Rack reference is not assigned.", this);
            return;
        }

        if (IsProcessing)
        {
            Debug.Log("RackScanner: Already processing a table, ignoring new trigger.", this);
            return;
        }

        Item_Slotted_Table slottedTable = other.GetComponentInParent<Item_Slotted_Table>();
        if (slottedTable == null)
        {
            Debug.Log("RackScanner: Entering object is not part of a slotted table.", other.gameObject);
            return;
        }

        Spline_Animate spline = other.GetComponentInParent<Spline_Animate>();
        if (spline == null)
        {
            Debug.LogWarning($"RackScanner: Table '{slottedTable.TableID}' is missing Spline_Animate.", slottedTable);
            return;
        }

        if (!rack.NeedsRackReturn(slottedTable))
        {
            Debug.Log($"RackScanner: Table '{slottedTable.TableID}' slot is already occupied. Skipping.", slottedTable);
            return;
        }

        Debug.Log($"RackScanner: Slotted table '{slottedTable.TableID}' passed through scanner.", slottedTable);

        ActiveTable = slottedTable;
        IsProcessing = true;

        spline.Pause();
        StartCoroutine(ProcessRackReturn(slottedTable, spline));
    }

    private IEnumerator ProcessRackReturn(Item_Slotted_Table table, Spline_Animate spline)
    {
        yield return new WaitForSeconds(processDelay);

        if (table == null)
        {
            ClearProcessingState();
            yield break;
        }

        switch (table.task)
        {
            case RACK_TASK.RETRIEVE:
                rack.SlotRetrieve(table);
                Debug.Log($"RackScanner: Retrieved table '{table.TableID}' into rack.", table);
                break;

            case RACK_TASK.INSERT:
                rack.SlotInsert(table);
                Debug.Log($"RackScanner: Inserted table '{table.TableID}' into rack.", table);
                break;

            default:
                Debug.LogWarning($"RackScanner: Unsupported rack task on table '{table.TableID}'.", table);
                break;
        }

        ClearProcessingState();
    }

    private void ClearProcessingState()
    {
        ActiveTable = null;
        IsProcessing = false;
    }
}