using Unity.VisualScripting;
using UnityEngine;

public class Item_Slotted_Table : Item_Parent{
    public GameObject Item;
    public GameObject AnchorPoint;

    public Item_Slotted_Table(){
        Name = "Slotted Table";
        Pickup = false;
        Quantity = 1;
    }

    public override void Start(){
        base.Start();

        if (Item){
            Item.transform.SetParent(AnchorPoint.transform, true);
            Item.transform.position = AnchorPoint.transform.position;
        }
    }
}
