using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

public class Item_Conveyor_Belt : Item_Parent{
    public GameObject SplineReference;

    List<GameObject> SlottedTableList = new List<GameObject>();

    bool Active = true;
	//float BaseSpeed = 0.325f;

    public Item_Conveyor_Belt(){
        Name = "Conveyor Belt";
        Pickup = false;
        Quantity = 1;
    }

    public override void Start(){
        base.Start();

        GameObject[] SlottedTables = GameObject.FindGameObjectsWithTag("Slotted Table");

        for (int i = 0; i < SlottedTables.Length; i++){
            SlottedTableList.Add(SlottedTables[i]);
        }
    }

    public override void Interact(Entity_Player PlayerReference){
        base.Interact(PlayerReference);
    }

    public override void AlternateInteract(Entity_Player PlayerReference){
        base.AlternateInteract(PlayerReference);
    }

    public void AddPlate(GameObject SlottedTable, float Offset){
        Spline_Animate AnimateReference = SlottedTable.GetComponent<Spline_Animate>();
        
        AnimateReference.StartOffset = Offset;
        AnimateReference.Container = SplineReference.GetComponent<SplineContainer>();

        if (Active){
            AnimateReference.Play();
        }
    }

    public void RemovePlate(string PlateID){
        for (int i = 0; i < SlottedTableList.Count; i++){
            if (SlottedTableList[i].GetComponent<Item_Slotted_Table>().TableID == PlateID){
                SlottedTableList[i].GetComponent<Spline_Animate>().Pause();
                SlottedTableList[i].GetComponent<Spline_Animate>().Container = null;

                SlottedTableList.RemoveAt(i);
            }
        }
    }

    public void ToggleMovement(){
        if (Active){
            for (int i = 0; i < SlottedTableList.Count; i++){
                SlottedTableList[i].GetComponent<Spline_Animate>().Pause();
            }

            Active = !Active;
        }
        else{
            for (int i = 0; i < SlottedTableList.Count; i++){
                SlottedTableList[i].GetComponent<Spline_Animate>().Play();
            }

            Active = !Active;
        }
    }
}
