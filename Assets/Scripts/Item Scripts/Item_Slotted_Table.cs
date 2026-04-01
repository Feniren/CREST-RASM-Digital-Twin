using Unity.VisualScripting;
using UnityEngine;
using System.Collections;
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
            Item.transform.rotation = Quaternion.LookRotation(AnchorPoint.transform.forward);
        }

        StartCoroutine(DrawLine());
    }
    
    IEnumerator DrawLine(){
        while (true){
            Debug.DrawLine(AnchorPoint.transform.position, AnchorPoint.transform.position + (AnchorPoint.transform.forward * 0.5f), Color.aliceBlue, 3.0f, false);

            yield return new WaitForSeconds(3.0f);
        }
    }

    public override void AlternateInteract(Entity_Player PlayerReference){
        if (Item == null){
            if (PlayerReference.gameObject.GetComponent<Player_Controller>().ItemInstance){
                Item = PlayerReference.gameObject.GetComponent<Player_Controller>().ItemInstance;

                PlayerReference.gameObject.GetComponent<Player_Controller>().ItemInstance = null;

                Item.transform.SetParent(AnchorPoint.transform, false);
                Item.transform.position = AnchorPoint.transform.position;
                Item.transform.rotation = Quaternion.LookRotation(AnchorPoint.transform.forward);
            }
        }
    }
}
