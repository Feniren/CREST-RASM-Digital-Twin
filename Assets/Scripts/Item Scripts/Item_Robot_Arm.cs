using System.Collections;
using UnityEngine;

public class Item_Robot_Arm : Item_Parent{
    Animator AnimatorReference;

    [Header("Gripper")]
    public Transform GripPoint;
    public GameObject HeldItem;

    private bool _isBusy;
    public bool IsBusy => _isBusy;

    public Item_Robot_Arm(){
        Name = "Robot Arm";
        Pickup = false;
        Quantity = 1;
    }

    public override void Start(){
        base.Start();

        AnimatorReference = GetComponent<Animator>();

        AnimatorReference.Play("PickUpItem");
    }

    public IEnumerator GrabItem(GameObject item){
        _isBusy = true;
        float halfDuration = GetAnimationLength() * 0.5f;

        AnimatorReference.Play("PickUpItem", 0, 0f);
        yield return new WaitForSeconds(halfDuration);

        item.transform.SetParent(GripPoint, false);
        item.transform.localPosition = Vector3.zero;
        HeldItem = item;

        yield return new WaitForSeconds(halfDuration);
        _isBusy = false;
    }

    public IEnumerator ReleaseItem(Transform destination){
        _isBusy = true;
        float halfDuration = GetAnimationLength() * 0.5f;

        AnimatorReference.Play("PickUpItem", 0, 0f);
        yield return new WaitForSeconds(halfDuration);

        HeldItem.transform.SetParent(destination, false);
        HeldItem.transform.localPosition = Vector3.zero;
        HeldItem = null;

        yield return new WaitForSeconds(halfDuration);
        _isBusy = false;
    }

    private float GetAnimationLength(){
        var clips = AnimatorReference.GetCurrentAnimatorClipInfo(0);
        return clips.Length > 0 ? clips[0].clip.length : 1f;
    }

    public override void AlternateInteract(Entity_Player PlayerReference){
    }
}
