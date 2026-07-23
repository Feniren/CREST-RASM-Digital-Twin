// using System.Collections;
// using UnityEngine;
// using UnityEngine.Events;

// public class Item_RFID_Sensor : Item_Parent{
// 	public Item_Conveyor_Belt ConveyorBeltReference;
// 	public Item_CNC_Machine CNCMachineReference;

// 	public Item_Parent TargetItem;
// 	public Item_Plate SensedPlate;

// 	public UnityEvent OnTargetFound;

// 	public Item_Slotted_Table ActiveTable { get; private set; }
// 	public bool IsProcessing { get; private set; }

// 	private readonly HashSet<Item_Slotted_Table> tablesInTrigger = new HashSet<Item_Slotted_Table>();

// 	public Item_RFID_Sensor(){
// 		Name = "RFID Sensor";
// 		Pickup = false;
// 		Quantity = 1;
// 	}

// 	public void OnTriggerEnter(Collider OverlappedCollider){
// 		GameObject OverlappedObject = OverlappedCollider.gameObject;

// 		if (!CNCMachineReference.ProcessingItem){
// 			if (OverlappedObject.GetComponent<Item_Plate>()){
// 				SensedPlate = OverlappedObject.GetComponent<Item_Plate>();

// 				if (TargetItem){
// 					if (SensedPlate.Item){
// 						if (SensedPlate.Item.GetComponentInParent<Item_Parent>().GetType() == TargetItem.GetType()){
// 							ConveyorBeltReference.ToggleMovement();

// 							TargetItem = null;

// 							OnTargetFound.Invoke();

// 							Invoke(nameof(PauseConveyorBelt), 3.0f);
// 						}
// 					}
// 				}
// 			}
// 		}

// 		HandleRackTriggerEnter(OverlappedCollider);
// 	}

// 	public void OnTriggerExit(Collider OverlappedCollider){
// 		GameObject OverlappedObject = OverlappedCollider.gameObject;

// 		if (OverlappedObject.GetComponent<Item_Plate>()){
// 			SensedPlate = null;
// 		}
// 	}

// 	public void PauseConveyorBelt(){
// 		ConveyorBeltReference.ToggleMovement();
// 	}

// 	private void HandleRackTriggerEnter(Collider other){
// 		if (rack == null)
// 			return;

// 		Debug.Log($"Item_RFID_Sensor: Trigger entered by '{other.name}'.", other.gameObject);

// 		if (ConveyorBeltReference == null)
// 		{
// 			Debug.LogError("Item_RFID_Sensor: Conveyor reference is not assigned.", this);
// 			return;
// 		}

// 		Item_Slotted_Table slottedTable = other.GetComponentInParent<Item_Slotted_Table>();
// 		if (slottedTable == null)
// 		{
// 			Debug.Log("Item_RFID_Sensor: Entering object is not part of a slotted table.", other.gameObject);
// 			return;
// 		}

// 		if (tablesInTrigger.Contains(slottedTable))
// 		{
// 			Debug.Log($"Item_RFID_Sensor: Table '{slottedTable.TableID}' is already in trigger set.", slottedTable);
// 			return;
// 		}

// 		tablesInTrigger.Add(slottedTable);

// 		if (IsProcessing)
// 		{
// 			Debug.Log("Item_RFID_Sensor: Already processing a table.", this);
// 			return;
// 		}

// 		Debug.Log($"Item_RFID_Sensor: Table '{slottedTable.TableID}' entered with task '{slottedTable.task}'.", slottedTable);

// 		if (slottedTable.task == RACK_TASK.NONE)
// 		{
// 			Debug.Log($"Item_RFID_Sensor: Table '{slottedTable.TableID}' has no active task. Ignoring.", slottedTable);
// 			return;
// 		}

// 		Spline_Animate spline = slottedTable.GetComponent<Spline_Animate>();
// 		if (spline == null)
// 			spline = slottedTable.GetComponentInParent<Spline_Animate>();

// 		if (spline == null)
// 		{
// 			Debug.LogWarning($"Item_RFID_Sensor: Table '{slottedTable.TableID}' is missing Spline_Animate.", slottedTable);
// 			return;
// 		}

// 		if (slottedTable.task == RACK_TASK.INSERT && !rack.NeedsRackReturn(slottedTable))
// 		{
// 			Debug.Log($"Item_RFID_Sensor: Table '{slottedTable.TableID}' slot is already occupied. Skipping insert.", slottedTable);
// 			return;
// 		}

// 		ActiveTable = slottedTable;
// 		IsProcessing = true;

// 		spline.Pause();
// 		StartCoroutine(ProcessRackTask(slottedTable, spline));
// 	}

// 	private IEnumerator ProcessRackTask(Item_Slotted_Table table, Spline_Animate scannedSpline)
// 	{
// 		yield return new WaitForSeconds(processDelay);

// 		if (table == null)
// 		{
// 			ClearProcessingState();
// 			yield break;
// 		}

// 		switch (table.task)
// 		{
// 			case RACK_TASK.RETRIEVE:
// 			{
// 				Item_Slotted_Table slot = table;
// 				Item_Slotted_Table retrieved = rack.RetrieveByID(slot.TableID);

// 				if (retrieved != null)
// 				{
// 					retrieved.transform.position = slot.transform.position;
// 					retrieved.transform.rotation = slot.transform.rotation;
// 					retrieved.gameObject.SetActive(true);

// 					Spline_Animate retrievedSpline = retrieved.GetComponent<Spline_Animate>();
// 					if (retrievedSpline == null)
// 						retrievedSpline = retrieved.GetComponentInParent<Spline_Animate>();

// 					if (retrievedSpline != null)
// 					{
// 						retrievedSpline.enabled = true;
// 					}

// 					float returnOffset = Mathf.Repeat(scannedSpline.NormalizedTime + scannedSpline.StartOffset, 1f);
// 					ConveyorBeltReference.AddPlate(retrieved.gameObject, returnOffset);

// 					retrieved.task = RACK_TASK.NONE;
// 					slot.task = RACK_TASK.NONE;
// 					slot.gameObject.SetActive(false);

// 					Debug.Log($"Item_RFID_Sensor: Retrieved table '{retrieved.TableID}' into slot '{slot.TableID}' and returned it at offset {returnOffset}.", retrieved);
// 				}
// 				else
// 				{
// 					Debug.LogWarning($"Item_RFID_Sensor: No stored table found for slot '{slot.TableID}'.", slot);
// 					slot.task = RACK_TASK.NONE;
// 				}
// 				break;
// 			}

// 			case RACK_TASK.INSERT:
// 			{
// 				rack.SlotInsert(table);
// 				ConveyorBeltReference.RemovePlate(table.TableID);
// 				table.task = RACK_TASK.NONE;

// 				Debug.Log($"Item_RFID_Sensor: Inserted table '{table.TableID}' into rack.", table);
// 				break;
// 			}

// 			case RACK_TASK.NONE:
// 			default:
// 			{
// 				Debug.Log($"Item_RFID_Sensor: No processing needed for table '{table.TableID}'.", table);
// 				break;
// 			}
// 		}

// 		ClearProcessingState();
// 	}

// 	public void OnTriggerExit(Collider other)
// 	{
// 		Item_Slotted_Table slottedTable = other.GetComponentInParent<Item_Slotted_Table>();
// 		if (slottedTable == null)
// 			return;

// 		if (tablesInTrigger.Contains(slottedTable))
// 		{
// 			tablesInTrigger.Remove(slottedTable);
// 			Debug.Log($"Item_RFID_Sensor: Table '{slottedTable.TableID}' exited scanner.", slottedTable);
// 		}
// 	}

// 	private void ClearProcessingState()
// 	{
// 		ActiveTable = null;
// 		IsProcessing = false;
// 	}
// }
