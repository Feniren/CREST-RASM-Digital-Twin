using UnityEngine;

// Same pickup/carry contract as Item_Epoxy_Block (Pickup = true, no other
// special behavior of its own) — behaves identically within the ASRS
// system; only its visual geometry/material differ.
public class Item_SlimGoldTube : Item_Parent{
    public Item_SlimGoldTube(){
        Name = "Slim Gold Tube";
        Pickup = true;
        Quantity = 1;
    }
}
