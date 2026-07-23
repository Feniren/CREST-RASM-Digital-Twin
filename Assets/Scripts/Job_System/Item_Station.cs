using UnityEngine;

public abstract class Item_Station : Item_CNC_Machine{
	public Item_Slotted_Table ActiveTable;

	public abstract bool IsProcessing { get; }

	public abstract void ProcessItem(Item_Slotted_Table table, Job_Queue queue, Spline_Animate spline);
}
