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

	Player_Input PlayerInput;

    Vector3 MovementVelocity;
    Vector2 ControlRotation;
    Vector3 FirstPersonCameraLocation;
    bool IsFirstPerson;
    Vector3 ThirdPersonCameraLocation;

    bool Throw = false;

    private void Awake(){
        PlayerInput = new Player_Input();

        PlayerInput.Player.Enable();
        PlayerInput.Player.EquipItem.performed += EquipItem;
        PlayerInput.Player.Interact.performed += Interact;
        PlayerInput.Player.AlternateInteract.performed += AlternateInteract;
        PlayerInput.Player.Jump.performed += Jump;
        PlayerInput.Player.Look.performed += Look;
        PlayerInput.Player.Move.performed += Move;
        PlayerInput.Player.Move.canceled += StopMoving;
        PlayerInput.Player.ShootPhysical.performed += ShootPhysical;
        PlayerInput.Player.ShootSpell.performed += ShootSpell;
        PlayerInput.Player.SwitchCameraPerspective.performed += SwitchCameraPerspective;
		PlayerInput.ItemEquipped.ThrowItem.performed += ThrowItem;

        FirstPersonCameraLocation = new Vector3(0.0f, 0.433f, 0.328f);
        IsFirstPerson = true;
        ThirdPersonCameraLocation = new Vector3(0.0f, 1.14f, -2.161f);
    }

    void Start(){
        RigidBodyReference = GetComponent<Rigidbody>();
        PlayerReference = GetComponent<Entity_Player>();

        MovementVelocity = new Vector2(0.0f, 0.0f);
        ControlRotation = new Vector2(0.0f, 0.0f);
    }

    private void OnEnable(){
        //InputSystem.Enable();
    }

    private void OnDisable(){
        //InputSystem.Disable();
    }

    void Update(){
        /*if (Input.GetKeyDown("l")){
            PlayerReference.LevelUp();
        }

        if (Input.GetKeyDown("r"))
        {
            gameObject.transform.position = new Vector3(0.0f, 5.0f, 0.0f);
        }

        if (Input.GetKeyDown("m"))
        {
            Throw = !Throw;
        }*/
    }

    private void FixedUpdate(){
        Vector3 Movement = ((PlayerReference.CameraReference.transform.right * MovementVelocity.x) + (PlayerReference.CameraReference.transform.forward * MovementVelocity.y));

        Movement.y = RigidBodyReference.linearVelocity.y;

        Movement.x *= PlayerReference.EntityStatistics.MovementSpeed;
        Movement.z *= PlayerReference.EntityStatistics.MovementSpeed;

        RigidBodyReference.linearVelocity = Movement;
    }

    public void EquipItem(InputAction.CallbackContext Context){
		if (Context.performed){
			//PlayerReference.ToggleEquippedItem();

			if (ItemInstance){
				Item_Parent Item = ItemInstance.GetComponent<Item_Parent>();

				Debug.Log("Item exists. Adding to inventory");

				PlayerReference.InventoryReference.AddToInventory(Item.Name, 1);

				Destroy(ItemInstance);

				PlayerInput.ItemEquipped.Disable();
			}
			else{
				if (PlayerReference.InventoryReference.StaticInventory.Count > 0){
					ItemInstance = Instantiate(PlayerReference.ItemLibraryReference.Find(PlayerReference.InventoryReference.StaticInventory[^1].Key), gameObject.transform.position + (PlayerReference.CameraReference.transform.forward * 2.0f), Quaternion.identity);

					PlayerReference.InventoryReference.RemoveFromInventory(PlayerReference.InventoryReference.StaticInventory[^1].Key, 1);

					ItemInstance.GetComponent<Rigidbody>().isKinematic = true;

					ItemInstance.transform.SetParent(PlayerReference.ItemAnchor.transform, true);

                    ItemInstance.transform.position = PlayerReference.ItemAnchor.transform.position;

                    if (Throw){
						ItemInstance.GetComponent<Rigidbody>().AddForce(PlayerReference.CameraReference.transform.forward * 30.0f, ForceMode.Impulse);
					}

					Debug.Log("Item created at " + ItemInstance.transform.position);

					PlayerInput.ItemEquipped.Enable();
				}
			}
		}
    }

	public void ThrowItem(InputAction.CallbackContext Context){
		if (Context.performed){
			ItemInstance.GetComponent<Rigidbody>().isKinematic = false;

			ItemInstance.transform.SetParent(null, true);

			ItemInstance = null;
		}
	}

	public void GrabEnd(InputAction.CallbackContext Context){
        if (Context.canceled){
            //Debug.Log("Grab End");
        }
    }

    public void Look(InputAction.CallbackContext Context){
        Vector2 MouseLook = PlayerInput.Player.Look.ReadValue<Vector2>();

        ControlRotation.x = (MouseLook.x * PlayerReference.PlayerSettings.LookSpeedX);
        ControlRotation.y -= (MouseLook.y * PlayerReference.PlayerSettings.LookSpeedY);
        ControlRotation.y = Mathf.Clamp(ControlRotation.y, -90.0f, 90.0f);

        Quaternion XQuaternion = Quaternion.Euler(ControlRotation.y, 0.0f, 0.0f);

        gameObject.transform.Rotate(new Vector3(0.0f, ControlRotation.x, 0.0f));
        PlayerReference.CameraReference.transform.localRotation = XQuaternion;
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

    public void Jump(InputAction.CallbackContext Context){
        if (Context.performed){
            if (PlayerReference.EntityStatistics.JumpCurrent < PlayerReference.EntityStatistics.JumpMax){
                RigidBodyReference.AddForce(new Vector3(0.0f, PlayerReference.EntityStatistics.JumpForce, 0.0f), ForceMode.Impulse);

                PlayerReference.EntityStatistics.JumpCurrent++;
            }
        }
    }

    public void GrabStart(InputAction.CallbackContext Context){
        if (Context.performed){
            //Debug.Log("Grab");

            PlayerReference.SetItemEquipped(true);
        }
    }

    public void Move(InputAction.CallbackContext Context){
        MovementVelocity = Context.ReadValue<Vector2>();
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

    public void StopMoving(InputAction.CallbackContext Context){
        MovementVelocity = new Vector2(0.0f, 0.0f);
    }
}
