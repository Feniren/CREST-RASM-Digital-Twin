using UnityEngine;

public class Conveyor_Scanner : MonoBehaviour {
	public Machine_Job_Type TargetJobType;
	public Item_Mill TargetMill;

	void OnTriggerEnter(Collider other){
		var table = other.GetComponent<Item_Slotted_Table>();
		if (table == null) return;

		var queue = other.GetComponent<Job_Queue>();
		if (queue == null) return;

		if (queue.jobPeek() != TargetJobType) return;
		if (table.Item == null) return;
		if (TargetMill == null || TargetMill.IsProcessing) return;

		var spline = other.GetComponent<Spline_Animate>();
		if (spline == null) return;

		spline.Pause();
		TargetMill.ProcessItem(table, queue, spline);
	}
}
