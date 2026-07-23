using System.Collections;
using UnityEngine;
using ProMill8000;

public class Item_Mill : Item_Station{
	public Transform ProcessPoint;
	[SerializeField] private MillingAnimation millingAnimation;
	public Item_Robot_Arm RobotArm;

	public Item_RFID_Sensor SensorReference;

	private bool _isProcessing;

	public override bool IsProcessing => _isProcessing;

	public Item_Mill(){
		Name = "Mill";
		Pickup = false;
		Quantity = 1;
	}

	public override void Start(){
		base.Start();
	}

	public override void ProcessItem(Item_Slotted_Table table, Job_Queue queue, Spline_Animate spline){
		if (_isProcessing) return;
		StartCoroutine(ProcessCoroutine(table, queue, spline));
	}

	private IEnumerator ProcessCoroutine(Item_Slotted_Table table, Job_Queue queue, Spline_Animate spline){
		_isProcessing = true;

		GameObject item = table.Item;

		// if no item, skip processing, keep job, resume spline
		if (item == null){
			spline.Play();
			_isProcessing = false;
			yield break;
		}

		// Take item from table
		table.Item = null;
		var itemCollider = item.GetComponent<Collider>(); //might not need to disable collider when transferring
		if (itemCollider != null)
			itemCollider.enabled = false;

		Transform clamp = ProcessPoint != null ? ProcessPoint : (millingAnimation != null ? millingAnimation.WorktableXTransform : null);

		// slotted table, robot arm, to process point (first half of animation)
		if (RobotArm != null && clamp != null){
			yield return RobotArm.StartCoroutine(RobotArm.TransferForward(item, clamp));
		} else {
			if (clamp != null){
				item.transform.SetParent(clamp, false);
				item.transform.localPosition = Vector3.zero;
			}
		}

		// Play milling animation and wait for it to finish
		if (millingAnimation != null){

			//add the capabilities to modify the item in the mill here

			millingAnimation.Play();
			yield return new WaitWhile(() => millingAnimation.IsPlaying);
		}

		// Re-enable item collider before returning
		if (itemCollider != null)
			itemCollider.enabled = true;

		// process point, to robot arm, to slotted table (second half of animation)
		if (RobotArm != null && table.AnchorPoint != null){
			yield return RobotArm.StartCoroutine(RobotArm.TransferReverse(item, table.AnchorPoint.transform));
			table.Item = item;
		} else {
			if (table.AnchorPoint != null){
				item.transform.SetParent(table.AnchorPoint.transform, false);
				item.transform.localPosition = Vector3.zero;
				table.Item = item;
			}
		}

		// Remove job and resume belt
		queue.jobPop();
		spline.Play();

		_isProcessing = false;
	}

	public override void Interact(Entity_Player PlayerReference){
		base.Interact(PlayerReference);
	}

	public override void AlternateInteract(Entity_Player PlayerReference){
	}
}
