using Unity.VisualScripting;
using UnityEngine;

public class Slotted_Table : Item_Parent{
    public GameObject Item;
    public GameObject AnchorPoint;

    public override void Start(){
        base.Start();

        if (Item){
            Item.transform.SetParent(AnchorPoint.transform, true);
            Item.transform.position = AnchorPoint.transform.position;
        }
    }
}
