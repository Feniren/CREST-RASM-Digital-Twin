using Unity.VisualScripting;
using UnityEngine;
using TMPro;

public class Item_Slotted_Table : Item_Parent{
    public GameObject Item;
    public GameObject AnchorPoint;
    public GameObject Text;
    public string TableID;

    public Item_Slotted_Table(){
        Name = "Slotted Table";
        Pickup = false;
        Quantity = 1;
    }

    public override void Start(){
        base.Start();

        Text.GetComponent<TextMeshPro>().SetText(TableID);

        if (Item){
            Item.transform.SetParent(AnchorPoint.transform, true);
            Item.transform.position = AnchorPoint.transform.position;
        }
    }
}
