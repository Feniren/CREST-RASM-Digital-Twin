using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

public class Item_Conveyor_Belt : Item_Parent{
    public GameObject SplineReference;

    List<GameObject> SlottedTableList = new List<GameObject>();
    public List<GameObject> GetSlottedTableList()
    {
        return SlottedTableList;
    }
    bool Active = true;
    float BaseSpeed = 0.325f;

    public Item_Conveyor_Belt(){
        Name = "Conveyor Belt";
        Pickup = false;
        Quantity = 1;
    }

    public override void Start(){
        base.Start();

        foreach (Item_Slotted_Table table in GetComponentsInChildren<Item_Slotted_Table>(true)){
            SlottedTableList.Add(table.gameObject);
        }
    }

    public override void Interact(Entity_Player PlayerReference){
        base.Interact(PlayerReference);
    }

    public override void AlternateInteract(Entity_Player PlayerReference){
        base.AlternateInteract(PlayerReference);
    }

    public void AddPlate(GameObject slottedTable, float offset)
    {
        if (slottedTable == null) return;

        Spline_Animate animateReference = slottedTable.GetComponent<Spline_Animate>();
        if (animateReference == null) return;

        animateReference.StartOffset = offset;
        animateReference.Container = SplineReference.GetComponent<SplineContainer>();

        if (!SlottedTableList.Contains(slottedTable))
        {
            SlottedTableList.Add(slottedTable);
        }

        if (Active)
        {
            animateReference.Play();
        }
        else
        {
            animateReference.Pause();
        }
    }

    public void RemovePlate(string plateID)
    {
        for (int i = SlottedTableList.Count - 1; i >= 0; i--)
        {
            Item_Slotted_Table table = SlottedTableList[i].GetComponent<Item_Slotted_Table>();
            if (table != null && table.TableID == plateID)
            {
                Spline_Animate animate = SlottedTableList[i].GetComponent<Spline_Animate>();
                if (animate != null)
                {
                    animate.Pause();
                    animate.Container = null;
                }

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