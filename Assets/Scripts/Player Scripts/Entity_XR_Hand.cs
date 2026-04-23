using UnityEngine;

public class Entity_XR_Hand : MonoBehaviour{
    public Entity_Player PlayerReference;

    public Entity_XR_Hand(){
    }

    public void OnTriggerEnter(Collider Other){
        Debug.Log(Other.gameObject.name);

        if (Other.gameObject.GetComponent<Item_Parent>()){
            Debug.Log("Is an item");

            Other.gameObject.GetComponent<Item_Parent>().AlternateInteract(PlayerReference);
        }
    }
}
