using UnityEngine;

// Same pickup/carry contract as Item_Epoxy_Block (Pickup = true, no other
// special behavior of its own) — this item is meant to behave identically
// within the ASRS system; only its visual geometry/material differ.
public class Item_MetalCylinder : Item_Parent{
    public Item_MetalCylinder(){
        Name = "Metal Cylinder";
        Pickup = true;
        Quantity = 1;
    }
}
