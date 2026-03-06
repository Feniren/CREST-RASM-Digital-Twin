using UnityEngine;
using System.Collections.Generic;

public class Item_CIM_Conveyor_Control_Box : Item_Parent{
    List<Spline_Animate> SlottedTableList = new List<Spline_Animate>();

    bool Active = true;
    float BaseSpeed = 0.325f;

    public Item_CIM_Conveyor_Control_Box(){
        Name = "CIM Conveyor Control Box";
        Pickup = false;
        Quantity = 1;
    }

    public override void Start(){
        base.Start();

        GameObject[] SlottedTables = GameObject.FindGameObjectsWithTag("Slotted Table");

        for (int i = 0; i < SlottedTables.Length; i++){
            SlottedTableList.Add(SlottedTables[i].GetComponent<Spline_Animate>());
        }
    }

    public override void Interact(Entity_Player PlayerReference){
        base.Interact(PlayerReference);
    }

    public override void AlternateInteract(Entity_Player PlayerReference){
        if (Active){
            for (int i = 0; i < SlottedTableList.Count; i++){
                SlottedTableList[i].Pause();
            }

            Active = !Active;
        }
        else{
            for (int i = 0; i < SlottedTableList.Count; i++){
                SlottedTableList[i].Play();
            }

            Active = !Active;
        }
    }
}
