using Unity.VisualScripting;
using UnityEngine;
using System.Collections;
using TMPro;

public class Item_Plate : Item_Parent{
    public GameObject Item;
    public GameObject AnchorPoint;
    public GameObject Text;
    public string PlateID;

    public Item_Plate(){
        Name = "Plate";
        Pickup = false;
        Quantity = 1;
    }

    public override void Start(){
        base.Start();

        Text.GetComponent<TextMeshPro>().SetText(PlateID);

        if (Item){
			SetItem();
        }

        StartCoroutine(DrawLine());
    }

	public void OnTriggerEnter(Collider OverlappedCollider){
		if (Item == null){
			if (OverlappedCollider.gameObject.GetComponentInParent<Item_Parent>()){
				Item_Parent OverlappedItem = OverlappedCollider.gameObject.GetComponentInParent<Item_Parent>();

				if (OverlappedItem.Pickup){
					if (OverlappedItem.gameObject.transform.parent == null){
						Item = OverlappedItem.gameObject;

						SetItem();
					}
				}
			}
		}
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

				SetItem();
            }
        }
    }

	public void SetItem(){
		if (Item){
			GetComponent<Collider>().enabled = false;

			Item.GetComponent<Rigidbody>().isKinematic = true;
			Item.transform.SetParent(AnchorPoint.transform, true);
			Item.transform.position = AnchorPoint.transform.position;
			Item.transform.rotation = Quaternion.LookRotation(AnchorPoint.transform.forward);
			Item.GetComponent<Item_Parent>().OnGrabbed.AddListener(UnSetItem);
		}
	}

	public void UnSetItem(){
		GetComponent<Collider>().enabled = true;

		Item.GetComponent<Item_Parent>().OnGrabbed.RemoveListener(UnSetItem);

		Item = null;
	}
}
