using UnityEngine;

// A thin machined-polymer fixture pad — same pickup/carry contract as
// Item_Epoxy_Block (Pickup = true, no other special behavior of its own).
public class Item_Workpiece_GreenInsert : Item_Parent{
    public Item_Workpiece_GreenInsert(){
        Name = "Green Insert";
        Pickup = true;
        Quantity = 1;
    }
}
