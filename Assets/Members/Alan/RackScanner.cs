// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public enum RACK_TASK
// {
//     NONE,
//     RETRIEVE,
//     INSERT
// }

// public class RackScanner : MonoBehaviour
// {
//     [SerializeField] private Item_ASRS rack;
//     [SerializeField] private Item_Conveyor_Belt conveyor;
//     [SerializeField] private float processDelay = 1.0f;

//     public Item_Slotted_Table ActiveTable { get; private set; }
//     public bool IsProcessing { get; private set; }

//     private readonly HashSet<Item_Slotted_Table> tablesInTrigger = new HashSet<Item_Slotted_Table>();

//     private void OnTriggerEnter(Collider other)
//     {
//         if (rack == null)
//         {
//             Debug.LogError("RackScanner: Rack reference is not assigned.", this);
//             return;
//         }

//         if (conveyor == null)
//         {
//             Debug.LogError("RackScanner: Conveyor reference is not assigned.", this);
//             return;
//         }

//         Item_Slotted_Table slottedTable = other.GetComponentInParent<Item_Slotted_Table>();
//         if (slottedTable == null)
//             return;

//         if (tablesInTrigger.Contains(slottedTable))
//             return;

//         tablesInTrigger.Add(slottedTable);

//         if (IsProcessing)
//             return;

//         if (slottedTable.task == RACK_TASK.NONE)
//             return;

//         Spline_Animate spline = slottedTable.GetComponent<Spline_Animate>();
//         if (spline == null)
//             spline = slottedTable.GetComponentInParent<Spline_Animate>();

//         if (spline == null)
//         {
//             Debug.LogWarning($"RackScanner: Table '{slottedTable.TableID}' is missing Spline_Animate.", slottedTable);
//             return;
//         }

//         if (slottedTable.task == RACK_TASK.INSERT && !rack.NeedsRackReturn(slottedTable))
//             return;

//         ActiveTable = slottedTable;
//         IsProcessing = true;

//         spline.Pause();
//         StartCoroutine(ProcessRackTask(slottedTable, spline));
//     }

//     private IEnumerator ProcessRackTask(Item_Slotted_Table table, Spline_Animate scannedSpline)
//     {
//         yield return new WaitForSeconds(processDelay);

//         if (table == null)
//         {
//             ClearProcessingState();
//             yield break;
//         }

//         switch (table.task)
//         {
//             case RACK_TASK.RETRIEVE:
//             {
//                 Item_Slotted_Table slot = table;
//                 Item_Slotted_Table retrieved = rack.RetrieveByID(slot.TableID);

//                 if (retrieved != null)
//                 {
//                     retrieved.transform.position = slot.transform.position;
//                     retrieved.transform.rotation = slot.transform.rotation;
//                     retrieved.gameObject.SetActive(true);

//                     Spline_Animate retrievedSpline = retrieved.GetComponent<Spline_Animate>();
//                     if (retrievedSpline == null)
//                         retrievedSpline = retrieved.GetComponentInParent<Spline_Animate>();

//                     if (retrievedSpline != null)
//                     {
//                         retrievedSpline.enabled = true;
//                     }

//                     float returnOffset = Mathf.Repeat(scannedSpline.NormalizedTime + scannedSpline.StartOffset, 1f);
//                     conveyor.AddPlate(retrieved.gameObject, returnOffset);

//                     retrieved.task = RACK_TASK.NONE;
//                     slot.task = RACK_TASK.NONE;
//                     slot.gameObject.SetActive(false);
//                 }
//                 else
//                 {
//                     Debug.LogWarning($"RackScanner: No stored table found for slot '{slot.TableID}'.", slot);
//                     slot.task = RACK_TASK.NONE;
//                 }
//                 break;
//             }

//             case RACK_TASK.INSERT:
//             {
//                 rack.SlotInsert(table);
//                 conveyor.RemovePlate(table.TableID);
//                 table.task = RACK_TASK.NONE;
//                 break;
//             }

//             case RACK_TASK.NONE:
//             default:
//                 break;
//         }

//         ClearProcessingState();
//     }

//     private void OnTriggerExit(Collider other)
//     {
//         Item_Slotted_Table slottedTable = other.GetComponentInParent<Item_Slotted_Table>();
//         if (slottedTable == null)
//             return;

//         tablesInTrigger.Remove(slottedTable);
//     }

//     private void ClearProcessingState()
//     {
//         ActiveTable = null;
//         IsProcessing = false;
//     }
// }
