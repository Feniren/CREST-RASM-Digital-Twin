using Unity.VisualScripting;
using UnityEngine;
using TMPro;

public class e_Item_Slotted_Table : Item_Parent{
    public GameObject Item;
    public GameObject AnchorPoint;
    public GameObject Text;
    public string TableID;

	private Engraving OwnEngraving = new Engraving();

	public e_Item_Slotted_Table(){
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

    public override void AlternateInteract(Entity_Player PlayerReference){
        if (Item == null){
            if (PlayerReference.gameObject.GetComponent<Player_Controller>().ItemInstance){
                Item = PlayerReference.gameObject.GetComponent<Player_Controller>().ItemInstance;

                PlayerReference.gameObject.GetComponent<Player_Controller>().ItemInstance = null;

                Item.transform.SetParent(AnchorPoint.transform, false);
                Item.transform.position = AnchorPoint.transform.position;
                Item.transform.rotation = AnchorPoint.transform.rotation;
            }
        }
    }

	public void ApplyEngraving(string EngraveText){
		Debug.Log("I'm getting engraved");
		OwnEngraving.Receive(EngraveText);
		Debug.Log("I'm engraved with '" + OwnEngraving.text_data + "'");
	}
}
