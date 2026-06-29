using System.Collections;
using UnityEngine;

public class Item_Robot_Arm : Item_Parent{
    Animator AnimatorReference;

    [Header("Gripper")]
    public Transform GripPoint;
    public GameObject HeldItem;

    private bool _isBusy;
    public bool IsBusy => _isBusy;

    private float _animationLength;

    public Item_Robot_Arm(){
        Name = "Robot Arm";
        Pickup = false;
        Quantity = 1;
    }

    public override void Start(){
        base.Start();

		float RandomOffset = Random.Range(0.0f, 1.0f);

        AnimatorReference = GetComponent<Animator>();

<<<<<<< HEAD
        var clips = AnimatorReference.runtimeAnimatorController.animationClips;
        _animationLength = clips.Length > 0 ? clips[0].length : 1f;

        AnimatorReference.Play("PickUpItem", 0, 0f);
        AnimatorReference.speed = 0;
    }

    public IEnumerator TransferForward(GameObject item, Transform destination){
        _isBusy = true;
        float halfDuration = _animationLength * 0.5f;

        item.transform.SetParent(GripPoint, true);
        item.transform.localPosition = Vector3.zero;
        HeldItem = item;

        AnimatorReference.speed = 1;
        AnimatorReference.Play("PickUpItem", 0, 0f);
        yield return new WaitForSeconds(halfDuration);
        AnimatorReference.speed = 0;

        HeldItem.transform.SetParent(destination, true);
        HeldItem.transform.localPosition = Vector3.zero;
        HeldItem = null;

        _isBusy = false;
    }

    public IEnumerator TransferReverse(GameObject item, Transform destination){
        _isBusy = true;
        float halfDuration = _animationLength * 0.5f;

        item.transform.SetParent(GripPoint, true);
        item.transform.localPosition = Vector3.zero;
        HeldItem = item;

        AnimatorReference.speed = 1;
        AnimatorReference.Play("PickUpItem", 0, 0.5f);
        yield return new WaitForSeconds(halfDuration);
        AnimatorReference.speed = 0;

        HeldItem.transform.SetParent(destination, true);
        HeldItem.transform.localPosition = Vector3.zero;
        HeldItem = null;

        _isBusy = false;
=======
		AnimatorReference.SetFloat("StartOffset", RandomOffset);

        //AnimatorReference.Play("PickUpItem");
>>>>>>> main
    }

    public override void AlternateInteract(Entity_Player PlayerReference){
    }
}
