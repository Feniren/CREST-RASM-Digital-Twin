using UnityEngine;

public class Conveyor_Scanner : MonoBehaviour {
	public Machine_Job_Type TargetJobType;
	public Item_Station TargetStation;

	void OnTriggerEnter(Collider other){
		var table = other.GetComponent<Item_Slotted_Table>();
		if (table == null) return;

		var queue = other.GetComponent<Job_Queue>();
		if (queue == null) return;

		if (queue.Jobs.Count == 0 || queue.jobPeek() != TargetJobType) return;
		if (table.Item == null) return;
		if (TargetStation == null || TargetStation.IsProcessing) return;

		var spline = other.GetComponent<Spline_Animate>();
		if (spline == null) return;

		spline.Pause();
		TargetStation.ProcessItem(table, queue, spline);
	}
}
