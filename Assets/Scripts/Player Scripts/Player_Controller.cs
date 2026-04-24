using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Controller : Controller{
    public InputAction InputSystem;
    public Entity_Player PlayerReference;
    public Rigidbody RigidBodyReference;

    public GameObject PhysicalProjectilePrefab;
    public GameObject SpellProjectilePrefab;
	public GameObject ItemInstance = null;
    public GameObject LeftHand;
    public GameObject RightHand;

    Player_Input PlayerInput;

    Vector3 MovementVelocity;
    Vector2 ControlRotation;
    Vector3 FirstPersonCameraLocation;
    bool IsFirstPerson;
    Vector3 ThirdPersonCameraLocation;
    float ThrowStartTime;
    float ThrowEndTime;

    private void Awake(){
        PlayerInput = new Player_Input();

        PlayerInput.Player.Enable();
        PlayerInput.Player.EquipItem.performed += EquipItem;
        PlayerInput.Player.Interact.performed += Interact;
        PlayerInput.Player.AlternateInteract.performed += AlternateInteract;
        PlayerInput.Player.Jump.performed += Jump;
        PlayerInput.Player.Look.performed += Look;
		PlayerInput.Player.Look.canceled += Look;
		PlayerInput.Player.Move.performed += Move;
        PlayerInput.Player.Move.canceled += StopMoving;
        PlayerInput.Player.ResetPosition.performed += ResetPosition;
        //PlayerInput.Player.ShootPhysical.performed += ShootPhysical;
        //PlayerInput.Player.ShootSpell.performed += ShootSpell;
        PlayerInput.Player.SwitchCameraPerspective.performed += SwitchCameraPerspective;
        PlayerInput.ItemEquipped.ThrowItem.started += StartThrow;
		PlayerInput.ItemEquipped.ThrowItem.canceled += ThrowItem;
        PlayerInput.Player.XRRightGrab.started += GrabStart;
        PlayerInput.Player.XRLeftGrab.started += GrabStart;
        PlayerInput.Player.XRRightGrab.canceled += GrabEnd;
        PlayerInput.Player.XRLeftGrab.canceled += GrabEnd;

        FirstPersonCameraLocation = new Vector3(0.0f, 0.433f, 0.328f);
        IsFirstPerson = true;
        ThirdPersonCameraLocation = new Vector3(0.0f, 1.14f, -2.161f);
    }

    void Start(){
        RigidBodyReference = GetComponent<Rigidbody>();
        PlayerReference = GetComponent<Entity_Player>();

        MovementVelocity = Vector2.zero;
        ControlRotation = Vector2.zero;
    }

    private void OnEnable(){
        //InputSystem.Enable();
    }

    private void OnDisable(){
        //InputSystem.Disable();
    }

    private void FixedUpdate(){
        Vector3 Movement = ((PlayerReference.CameraReference.transform.right * MovementVelocity.x) + (PlayerReference.CameraReference.transform.forward * MovementVelocity.y));

        Movement.y = RigidBodyReference.linearVelocity.y;

        Movement.x *= PlayerReference.EntityStatistics.MovementSpeed;
        Movement.z *= PlayerReference.EntityStatistics.MovementSpeed;

        RigidBodyReference.linearVelocity = Movement;
	}

	private void LateUpdate(){
		if (PlayerReference.PlayerSettings.XREnabled){
			gameObject.transform.Rotate(new Vector3(0.0f, (ControlRotation.x * 2.0f), 0.0f));
		}
	}

    public void Interact(InputAction.CallbackContext Context){
        RaycastHit Hit;

        if (Physics.Raycast(PlayerReference.CameraReference.transform.position, PlayerReference.CameraReference.transform.TransformDirection(Vector3.forward), out Hit, 100.0f, 1)){
            if (Hit.collider.gameObject.GetComponent<Item_Parent>()){
                Hit.collider.gameObject.GetComponent<Item_Parent>().Interact(PlayerReference);
            }
        }
    }   

    public void AlternateInteract(InputAction.CallbackContext Context){
        RaycastHit Hit;

        if (Physics.Raycast(PlayerReference.CameraReference.transform.position, PlayerReference.CameraReference.transform.TransformDirection(Vector3.forward), out Hit, 100.0f, 1)){
			if (Hit.collider.gameObject.GetComponent<Item_Parent>()){
                Hit.collider.gameObject.GetComponent<Item_Parent>().AlternateInteract(PlayerReference);
			}
        }
    }

	public void EquipItem(InputAction.CallbackContext Context){
		if (Context.performed){
			if (ItemInstance){
				Item_Parent Item = ItemInstance.GetComponent<Item_Parent>();

				Debug.Log("Item exists. Adding to inventory");

				PlayerReference.InventoryReference.AddToInventory(Item.Name, 1);

				Destroy(ItemInstance);

				PlayerInput.ItemEquipped.Disable();
				PlayerInput.Player.ShootPhysical.Enable();
			}
			else{
				if (PlayerReference.InventoryReference.StaticInventory.Count > 0){
					ItemInstance = Instantiate(PlayerReference.ItemLibraryReference.Find(PlayerReference.InventoryReference.StaticInventory[^1].Key), gameObject.transform.position + (PlayerReference.CameraReference.transform.forward * 2.0f), Quaternion.identity);

					PlayerReference.InventoryReference.RemoveFromInventory(PlayerReference.InventoryReference.StaticInventory[^1].Key, 1);

					ItemInstance.GetComponent<Rigidbody>().isKinematic = true;

					ItemInstance.transform.SetParent(PlayerReference.ItemAnchor.transform, true);

					ItemInstance.transform.position = PlayerReference.ItemAnchor.transform.position;

					Debug.Log("Item created at " + ItemInstance.transform.position);

					PlayerInput.ItemEquipped.Enable();
					PlayerInput.Player.ShootPhysical.Disable();
				}
			}
		}
	}

	public void GrabEnd(InputAction.CallbackContext Context){
		if (Context.canceled){
			if (Context.action.name == "XRLeftGrab"){
				LeftHand.GetComponent<Entity_XR_Hand>().GrabEnd();
			}
			else if (Context.action.name == "XRRightGrab"){
				RightHand.GetComponent<Entity_XR_Hand>().GrabEnd();
			}
		}
	}

	public void GrabStart(InputAction.CallbackContext Context){
		if (Context.started){
			GameObject ActiveHand;
			List<GameObject> ItemList = new List<GameObject>();
			float Distance;
			float MinimumDistance;
			int MinimumDistanceIndex;
			Collider[] OverlappedObjects;

			if (Context.action.name == "XRLeftGrab"){
				ActiveHand = LeftHand;
			}
			else if (Context.action.name == "XRRightGrab"){
				ActiveHand = RightHand;
			}
			else{
				return;
			}

			OverlappedObjects = Physics.OverlapSphere(ActiveHand.transform.position, (ActiveHand.GetComponentInChildren<SphereCollider>().radius * 0.1f));

			for (int Index = 0; Index < OverlappedObjects.Length; Index++){
				if (OverlappedObjects[Index].gameObject.GetComponent<Interact_Interface>() != null){
					ItemList.Add(OverlappedObjects[Index].gameObject);
				}
			}

			MinimumDistance = float.MaxValue;
			MinimumDistanceIndex = 0;

			for (int Index = 0; Index < ItemList.Count; Index++){
				Distance = Vector3.Distance(ActiveHand.transform.position, ItemList[Index].transform.position);

				if (Distance < MinimumDistance){
					MinimumDistance = Distance;
					MinimumDistanceIndex = Index;
				}
			}

			if (ItemList.Count > 0){
				ActiveHand.GetComponent<Entity_XR_Hand>().GrabStart(PlayerReference, ItemList[MinimumDistanceIndex]);
			}
		}
	}

	public void Jump(InputAction.CallbackContext Context){
        if (Context.performed){
            if (PlayerReference.EntityStatistics.JumpCurrent < PlayerReference.EntityStatistics.JumpMax){
                RigidBodyReference.AddForce(new Vector3(0.0f, PlayerReference.EntityStatistics.JumpForce, 0.0f), ForceMode.Impulse);

                PlayerReference.EntityStatistics.JumpCurrent++;
            }
        }
    }

	public void Look(InputAction.CallbackContext Context){
		if (Context.performed){
			Vector2 Look = Context.ReadValue<Vector2>();

			ControlRotation.x = (Look.x * PlayerReference.PlayerSettings.LookSpeedX);
			ControlRotation.y -= (Look.y * PlayerReference.PlayerSettings.LookSpeedY);
			ControlRotation.y = Mathf.Clamp(ControlRotation.y, -90.0f, 90.0f);

			Quaternion XQuaternion = Quaternion.Euler(ControlRotation.y, 0.0f, 0.0f);
			
			gameObject.transform.Rotate(new Vector3(0.0f, ControlRotation.x, 0.0f));

			if (!PlayerReference.PlayerSettings.XREnabled){
				PlayerReference.CameraReference.transform.localRotation = XQuaternion;
			}
		}
		else if (Context.canceled){
			if (PlayerReference.PlayerSettings.XREnabled){
				ControlRotation = Vector2.zero;
			}
		}
	}

	public void Move(InputAction.CallbackContext Context){
        MovementVelocity = Context.ReadValue<Vector2>();
    }

    public void ResetPosition(InputAction.CallbackContext Context){
        PlayerReference.gameObject.transform.position = new Vector3(0.0f, 10.0f, 10.0f);
    }

    public void ShootPhysical(InputAction.CallbackContext Context){
        GameObject ProjectileReference;

        ProjectileReference = Instantiate(PhysicalProjectilePrefab, (PlayerReference.CameraReference.transform.position + (PlayerReference.CameraReference.transform.forward * 1.5f)), Quaternion.identity);

        ProjectileReference.GetComponent<Item_Projectile>().Owner = gameObject;

        ProjectileReference.GetComponent<Item_Projectile>().RigidBodyReference.AddForce(PlayerReference.CameraReference.transform.forward * 30.0f, ForceMode.Impulse);
    }

    public void ShootSpell(InputAction.CallbackContext Context){
        GameObject ProjectileReference;

        ProjectileReference = Instantiate(SpellProjectilePrefab, (PlayerReference.CameraReference.transform.position + (PlayerReference.CameraReference.transform.forward * 1.5f)), Quaternion.identity);

        ProjectileReference.GetComponent<Item_Projectile>().Owner = gameObject;

        ProjectileReference.GetComponent<Item_Projectile>().RigidBodyReference.AddForce(PlayerReference.CameraReference.transform.forward * 30.0f, ForceMode.Impulse);
    }

	public void StartThrow(InputAction.CallbackContext Context){
		if (Context.started){
			if (ItemInstance){
				ThrowStartTime = Time.time;
			}
		}
	}

	public void StopMoving(InputAction.CallbackContext Context){
		MovementVelocity = new Vector2(0.0f, 0.0f);
	}

	public void SwitchCameraPerspective(InputAction.CallbackContext Context){
        IsFirstPerson = !IsFirstPerson;

        if (IsFirstPerson){
            PlayerReference.CameraReference.transform.localPosition = FirstPersonCameraLocation;
        }
        else{
            PlayerReference.CameraReference.transform.localPosition = ThirdPersonCameraLocation;
        }

        Debug.Log(PlayerReference.CameraReference.transform.localPosition);
    }

	public void ThrowItem(InputAction.CallbackContext Context){
		float ElapsedThrowTime;
		float ThrowForce = 0.0f;

		if (Context.canceled){
			ThrowEndTime = Time.time;

			ElapsedThrowTime = (ThrowEndTime - ThrowStartTime);

			if (ElapsedThrowTime > 3.0f){
				ElapsedThrowTime = 3.0f;
			}

			if (ElapsedThrowTime > 0.2f){
				ThrowForce = (ElapsedThrowTime * 10.0f);
			}

			if (ItemInstance){
				ItemInstance.GetComponent<Rigidbody>().isKinematic = false;

				ItemInstance.transform.SetParent(null, true);

				ItemInstance.GetComponent<Rigidbody>().AddForce((PlayerReference.CameraReference.transform.forward * ThrowForce), ForceMode.Impulse);

				ItemInstance = null;
			}
		}
	}
}
