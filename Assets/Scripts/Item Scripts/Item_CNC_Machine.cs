using UnityEngine;

public class Item_CNC_Machine : Item_Parent{
	public Item_RFID_Sensor SensorReference;
	public bool ProcessingItem;

	public Item_CNC_Machine(){
		ProcessingItem = false;
	}
}
