using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RACK_TASK
{
    NONE,
    RETRIEVE,
    INSERT
}

public class RackScanner : MonoBehaviour
{
    [SerializeField] private Item_ASRS rack;
    [SerializeField] private Item_Conveyor_Belt conveyor;
    [SerializeField] private float processDelay = 1.0f;

    // Adjust this in Inspector if needed so retrieved tables re-enter at the correct spline position
    [SerializeField] private float retrieveReturnOffset = 0f;

    public Item_Slotted_Table ActiveTable { get; private set; }
    public bool IsProcessing { get; private set; }

    private readonly HashSet<Item_Slotted_Table> tablesInTrigger = new HashSet<Item_Slotted_Table>();

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"RackScanner: Trigger entered by '{other.name}'.", other.gameObject);

        if (rack == null)
        {
            Debug.LogError("RackScanner: Rack reference is not assigned.", this);
            return;
        }

        if (conveyor == null)
        {
            Debug.LogError("RackScanner: Conveyor reference is not assigned.", this);
            return;
        }

        Item_Slotted_Table slottedTable = other.GetComponentInParent<Item_Slotted_Table>();
        if (slottedTable == null)
        {
            Debug.Log("RackScanner: Entering object is not part of a slotted table.", other.gameObject);
            return;
        }

        if (tablesInTrigger.Contains(slottedTable))
        {
            Debug.Log($"RackScanner: Table '{slottedTable.TableID}' is already in trigger set.", slottedTable);
            return;
        }

        tablesInTrigger.Add(slottedTable);

        if (IsProcessing)
        {
            Debug.Log("RackScanner: Already processing a table.", this);
            return;
        }

        Debug.Log($"RackScanner: Table '{slottedTable.TableID}' entered with task '{slottedTable.task}'.", slottedTable);

        if (slottedTable.task == RACK_TASK.NONE)
        {
            Debug.Log($"RackScanner: Table '{slottedTable.TableID}' has no active task. Ignoring.", slottedTable);
            return;
        }

        Spline_Animate spline = slottedTable.GetComponent<Spline_Animate>();
        if (spline == null)
            spline = slottedTable.GetComponentInParent<Spline_Animate>();

        if (spline == null)
        {
            Debug.LogWarning($"RackScanner: Table '{slottedTable.TableID}' is missing Spline_Animate.", slottedTable);
            return;
        }

        if (slottedTable.task == RACK_TASK.INSERT && !rack.NeedsRackReturn(slottedTable))
        {
            Debug.Log($"RackScanner: Table '{slottedTable.TableID}' slot is already occupied. Skipping insert.", slottedTable);
            return;
        }

        ActiveTable = slottedTable;
        IsProcessing = true;

        spline.Pause();
        StartCoroutine(ProcessRackTask(slottedTable));
    }

    private IEnumerator ProcessRackTask(Item_Slotted_Table table)
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
            {
                Item_Slotted_Table slot = table;
                Item_Slotted_Table retrieved = rack.RetrieveByID(slot.TableID);

                if (retrieved != null)
                {
                    retrieved.transform.position = slot.transform.position;
                    retrieved.transform.rotation = slot.transform.rotation;
                    retrieved.gameObject.SetActive(true);

                    Spline_Animate retrievedSpline = retrieved.GetComponent<Spline_Animate>();
                    if (retrievedSpline == null)
                        retrievedSpline = retrieved.GetComponentInParent<Spline_Animate>();

                    if (retrievedSpline != null)
                    {
                        retrievedSpline.enabled = true;
                    }

                    conveyor.AddPlate(retrieved.gameObject, retrieveReturnOffset);

                    retrieved.task = RACK_TASK.NONE;
                    slot.task = RACK_TASK.NONE;

                    slot.gameObject.SetActive(false);

                    Debug.Log($"RackScanner: Retrieved table '{retrieved.TableID}' into slot '{slot.TableID}' and returned it to conveyor.", retrieved);
                }
                else
                {
                    Debug.LogWarning($"RackScanner: No stored table found for slot '{slot.TableID}'.", slot);
                    slot.task = RACK_TASK.NONE;
                }
                break;
            }

            case RACK_TASK.INSERT:
            {
                rack.SlotInsert(table);
                conveyor.RemovePlate(table.TableID);
                table.task = RACK_TASK.NONE;

                Debug.Log($"RackScanner: Inserted table '{table.TableID}' into rack.", table);
                break;
            }

            case RACK_TASK.NONE:
            default:
            {
                Debug.Log($"RackScanner: No processing needed for table '{table.TableID}'.", table);
                break;
            }
        }

        ClearProcessingState();
    }

    private void OnTriggerExit(Collider other)
    {
        Item_Slotted_Table slottedTable = other.GetComponentInParent<Item_Slotted_Table>();
        if (slottedTable == null)
            return;

        if (tablesInTrigger.Contains(slottedTable))
        {
            tablesInTrigger.Remove(slottedTable);
            Debug.Log($"RackScanner: Table '{slottedTable.TableID}' exited scanner.", slottedTable);
        }
    }

    private void ClearProcessingState()
    {
        ActiveTable = null;
        IsProcessing = false;
    }
}