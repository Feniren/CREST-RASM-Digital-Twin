using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item_RFID_Sensor : Item_Parent{
	public Item_Conveyor_Belt ConveyorBeltReference;
	public Item_ASRS rack;
	[Tooltip("Optional — if assigned, a RETRIEVE task is carried physically by the gripper (grab off the shelf, carry to this sensor, place on the conveyor) instead of being teleported instantly.")]
	public ASRS_Gripper_Controller gripper;
	public float processDelay = 1.0f;

	[Header("Lesson (optional)")]
	[Tooltip("Leave empty to use this sensor standalone with no lesson gating.")]
	public SequenceManager sequenceManager;
	[Tooltip("Fired once a table that rode the conveyor in is fully stored in the rack (an INSERT completes) — the 'conveyor round trip' the Module 2 lesson's last step waits on.")]
	public string conveyorReturnedActionId = "conveyor_returned";

	public Item_Slotted_Table ActiveTable { get; private set; }
	public bool IsProcessing { get; private set; }

	private readonly HashSet<Item_Slotted_Table> tablesInTrigger = new HashSet<Item_Slotted_Table>();

	public Item_RFID_Sensor(){
		Name = "RFID Sensor";
		Pickup = false;
		Quantity = 1;
	}

	public void OnTriggerEnter(Collider OverlappedCollider){
		HandleRackTriggerEnter(OverlappedCollider);
	}

	private void HandleRackTriggerEnter(Collider other){
		if (rack == null)
			return;

		// Debug.Log($"Item_RFID_Sensor: Trigger entered by '{other.name}'.", other.gameObject);

		if (ConveyorBeltReference == null)
		{
			Debug.LogError("Item_RFID_Sensor: Conveyor reference is not assigned.", this);
			return;
		}

		Item_Slotted_Table slottedTable = other.GetComponentInParent<Item_Slotted_Table>();
		if (slottedTable == null)
		{
			// Debug.Log("Item_RFID_Sensor: Entering object is not part of a slotted table.", other.gameObject);
			return;
		}

		if (tablesInTrigger.Contains(slottedTable))
		{
			// Debug.Log($"Item_RFID_Sensor: Table '{slottedTable.TableID}' is already in trigger set.", slottedTable);
			return;
		}

		tablesInTrigger.Add(slottedTable);

		if (IsProcessing)
		{
			// Debug.Log("Item_RFID_Sensor: Already processing a table.", this);
			return;
		}

		// Debug.Log($"Item_RFID_Sensor: Table '{slottedTable.TableID}' entered with task '{slottedTable.task}'.", slottedTable);

		if (slottedTable.task == RACK_TASK.NONE)
		{
			// Debug.Log($"Item_RFID_Sensor: Table '{slottedTable.TableID}' has no active task. Ignoring.", slottedTable);
			return;
		}

		// Don't consume a table for a lesson step the trainee hasn't reached
		// yet — otherwise tables riding the conveyor during earlier steps get
		// scanned and stored away before the trainee ever gets to the step
		// that's meant to demonstrate this, leaving none left by then.
		if (sequenceManager != null && !sequenceManager.HasReachedStep(conveyorReturnedActionId))
		{
			// Debug.Log($"Item_RFID_Sensor: Lesson hasn't reached '{conveyorReturnedActionId}' yet — letting '{slottedTable.TableID}' pass through untouched.", slottedTable);
			return;
		}

		Spline_Animate spline = slottedTable.GetComponent<Spline_Animate>();
		if (spline == null)
			spline = slottedTable.GetComponentInParent<Spline_Animate>();

		if (spline == null)
		{
			Debug.LogWarning($"Item_RFID_Sensor: Table '{slottedTable.TableID}' is missing Spline_Animate.", slottedTable);
			return;
		}

		if (slottedTable.task == RACK_TASK.INSERT && !rack.NeedsRackReturn(slottedTable))
		{
			// Debug.Log($"Item_RFID_Sensor: Table '{slottedTable.TableID}' slot is already occupied. Skipping insert.", slottedTable);
			return;
		}

		ActiveTable = slottedTable;
		IsProcessing = true;

		// Pause the whole belt for the entire time this table is being
		// processed — resumed in ClearProcessingState() once the task is
		// genuinely finished (including the full gripper carry for a
		// RETRIEVE), not on a fixed timer.
		if (ConveyorBeltReference != null)
			ConveyorBeltReference.PauseMovement();

		spline.Pause();
		StartCoroutine(ProcessRackTask(slottedTable, spline));
	}

	private IEnumerator ProcessRackTask(Item_Slotted_Table table, Spline_Animate scannedSpline)
	{
		yield return new WaitForSeconds(processDelay);

		if (table == null)
		{
			ClearProcessingState();
			yield break;
		}

		// Set true whenever a coroutine below takes over responsibility for
		// eventually calling ClearProcessingState() itself (the gripper
		// carry, or the auto-retrieve-after-insert demo) — IsProcessing must
		// stay true for the whole physical action, not just this one frame,
		// or a second table could enter the scanner and start a conflicting
		// arm command while the arm is still mid-carry on this one.
		bool deferClear = false;

		switch (table.task)
		{
			case RACK_TASK.RETRIEVE:
			{
				Vector3 dropPosition = table.transform.position;
				Quaternion dropRotation = table.transform.rotation;
				float returnOffset = Mathf.Repeat(scannedSpline.NormalizedTime + scannedSpline.StartOffset, 1f);
				string tableId = table.TableID;

				table.task = RACK_TASK.NONE;
				table.gameObject.SetActive(false);

				deferClear = true;
				StartCoroutine(RetrieveAndReturnToConveyor(tableId, dropPosition, dropRotation, returnOffset));
				break;
			}

			case RACK_TASK.INSERT:
			{
				Vector3 entryPosition = table.transform.position;
				Quaternion entryRotation = table.transform.rotation;
				float entryOffset = Mathf.Repeat(scannedSpline.NormalizedTime + scannedSpline.StartOffset, 1f);
				string insertedId = table.TableID;

				rack.SlotInsert(table);
				ConveyorBeltReference.RemovePlate(table.TableID);
				table.task = RACK_TASK.NONE;

				// This is the "conveyor round trip" the Module 2 lesson's
				// last step waits on: a table rode the conveyor in, got
				// scanned, and is now actually stored in the rack.
				bool onConveyorReturnedStep = sequenceManager != null && sequenceManager.IsOnStep(conveyorReturnedActionId);

				if (sequenceManager != null)
					sequenceManager.NotifyAction(conveyorReturnedActionId);

				// Specifically while that step is active, show the trainee
				// the other half of the cycle too: pull the same table
				// straight back out of the rack and return it to the
				// conveyor, instead of needing a second table separately
				// staged with a RETRIEVE task.
				if (onConveyorReturnedStep)
				{
					deferClear = true;
					StartCoroutine(DelayThenRetrieve(insertedId, entryPosition, entryRotation, entryOffset));
				}

				// Debug.Log($"Item_RFID_Sensor: Inserted table '{table.TableID}' into rack.", table);
				break;
			}

			case RACK_TASK.NONE:
			default:
			{
				// Debug.Log($"Item_RFID_Sensor: No processing needed for table '{table.TableID}'.", table);
				break;
			}
		}

		if (!deferClear)
			ClearProcessingState();
	}

	private IEnumerator DelayThenRetrieve(string tableId, Vector3 dropPosition, Quaternion dropRotation, float returnOffset)
	{
		// Let the trainee actually see it sitting in the rack for a moment
		// before immediately pulling it back out.
		yield return new WaitForSeconds(processDelay);
		yield return RetrieveAndReturnToConveyor(tableId, dropPosition, dropRotation, returnOffset);
	}

	// Shared by a normal RETRIEVE trigger and the auto-demo after an insert:
	// pulls 'tableId' out of the rack and carries it (via the gripper, if
	// wired) to dropPosition/dropRotation, then hands it to the conveyor.
	// Always finishes by clearing processing state, whichever path called it.
	private IEnumerator RetrieveAndReturnToConveyor(string tableId, Vector3 dropPosition, Quaternion dropRotation, float returnOffset)
	{
		Item_Slotted_Table retrieved = rack.RetrieveByID(tableId);

		if (retrieved == null)
		{
			Debug.LogWarning($"Item_RFID_Sensor: No stored table found for '{tableId}'.", this);
			ClearProcessingState();
			yield break;
		}

		// rack.RetrieveByID() already re-enabled the retrieved table's own
		// Spline_Animate — pause it again immediately so the gripper (not
		// the spline) drives its movement for the carry.
		Spline_Animate retrievedSpline = retrieved.GetComponent<Spline_Animate>();
		if (retrievedSpline == null)
			retrievedSpline = retrieved.GetComponentInParent<Spline_Animate>();
		if (retrievedSpline != null)
			retrievedSpline.enabled = false;

		bool started = gripper != null && gripper.RetrieveToPoint(retrieved, dropPosition, dropRotation, () =>
		{
			if (retrievedSpline != null)
				retrievedSpline.enabled = true;

			ConveyorBeltReference.AddPlate(retrieved.gameObject, returnOffset);
			retrieved.task = RACK_TASK.NONE;

			ClearProcessingState();
		});

		if (!started)
		{
			// No gripper wired (or it's busy) — fall back to the original
			// instant placement so retrieval still works.
			retrieved.transform.position = dropPosition;
			retrieved.transform.rotation = dropRotation;
			if (retrievedSpline != null)
				retrievedSpline.enabled = true;

			ConveyorBeltReference.AddPlate(retrieved.gameObject, returnOffset);
			retrieved.task = RACK_TASK.NONE;

			ClearProcessingState();
		}
	}

	public void OnTriggerExit(Collider other)
	{
		Item_Slotted_Table slottedTable = other.GetComponentInParent<Item_Slotted_Table>();
		if (slottedTable == null)
			return;

		if (tablesInTrigger.Contains(slottedTable))
		{
			tablesInTrigger.Remove(slottedTable);
			// Debug.Log($"Item_RFID_Sensor: Table '{slottedTable.TableID}' exited scanner.", slottedTable);
		}
	}

	private void ClearProcessingState()
	{
		ActiveTable = null;
		IsProcessing = false;

		if (ConveyorBeltReference != null)
			ConveyorBeltReference.ResumeMovement();
	}
}
