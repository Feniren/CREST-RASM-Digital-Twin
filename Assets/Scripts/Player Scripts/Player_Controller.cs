using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Controller : Controller{
    public InputAction InputSystem;
    public Entity_Player PlayerReference;
    public Rigidbody RigidBodyReference;

    public GameObject PhysicalProjectilePrefab;
    public GameObject SpellProjectilePrefab;

    Player_Input PlayerInput;

    Vector3 MovementVelocity;
    Vector2 ControlRotation;
    Vector3 FirstPersonCameraLocation;
    bool IsFirstPerson;
    Vector3 ThirdPersonCameraLocation;

    int JumpCount;
    bool Throw = false;

    const string MouseX = "Mouse X";
    const string MouseY = "Mouse Y";

    private void Awake(){
        RigidBodyReference = GetComponent<Rigidbody>();
        PlayerReference = GetComponent<Entity_Player>();

        PlayerInput = new Player_Input();

        PlayerInput.Player.Enable();
        PlayerInput.Player.DropItem.performed += DropItem;
        PlayerInput.Player.Interact.performed += Interact;
        PlayerInput.Player.AlternateInteract.performed += AlternateInteract;
        PlayerInput.Player.Jump.performed += Jump;
        PlayerInput.Player.Look.performed += Look;
        PlayerInput.Player.Move.performed += Move;
        PlayerInput.Player.Move.canceled += StopMoving;
        PlayerInput.Player.ShootPhysical.performed += ShootPhysical;
        PlayerInput.Player.ShootSpell.performed += ShootSpell;
        PlayerInput.Player.SwitchCameraPerspective.performed += SwitchCameraPerspective;

        FirstPersonCameraLocation = new Vector3(0.0f, 0.433f, 0.328f);
        IsFirstPerson = true;
        ThirdPersonCameraLocation = new Vector3(0.0f, 1.14f, -2.161f);
    }

    void Start(){
        RigidBodyReference = GetComponent<Rigidbody>();
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
        ControlRotation.x = (Input.GetAxis(MouseX) * PlayerReference.PlayerSettings.LookSpeedX);
        ControlRotation.y -= (Input.GetAxis(MouseY) * PlayerReference.PlayerSettings.LookSpeedY);
        ControlRotation.y = Mathf.Clamp(ControlRotation.y, -90.0f, 90.0f);

        //Quaternion XQuaternion = Quaternion.AngleAxis(ControlRotation.x, Vector3.up);
        Quaternion XQuaternion = Quaternion.Euler(ControlRotation.y, 0.0f, 0.0f);

        gameObject.transform.Rotate(new Vector3(0.0f, ControlRotation.x, 0.0f));
        PlayerReference.CameraReference.transform.localRotation = XQuaternion;

        if (Input.GetKeyDown("l")){
            PlayerReference.LevelUp();
        }

        if (Input.GetKeyDown("r"))
        {
            gameObject.transform.position = new Vector3(0.0f, 5.0f, 0.0f);
        }

        if (Input.GetKeyDown("m"))
        {
            Throw = !Throw;
        }
    }

    private void FixedUpdate(){
        //PlayerInput.Player.Look.ReadValue<Vector2>();

        Vector3 Movement = ((PlayerReference.CameraReference.transform.right * MovementVelocity.x) + (PlayerReference.CameraReference.transform.forward * MovementVelocity.y));

        Movement.y = RigidBodyReference.linearVelocity.y;

        Movement.x *= PlayerReference.EntityStatistics.MovementSpeed;
        Movement.z *= PlayerReference.EntityStatistics.MovementSpeed;

        RigidBodyReference.linearVelocity = Movement;
    }

    public void DropItem(InputAction.CallbackContext Context){
        GameObject DroppedItem;

        if (PlayerReference.InventoryReference.StaticInventory.Count > 0){
            DroppedItem = Instantiate(PlayerReference.ItemLibraryReference.Find(PlayerReference.InventoryReference.StaticInventory[0].Key), gameObject.transform.position + (PlayerReference.CameraReference.transform.forward * 2.0f), Quaternion.identity);

            PlayerReference.InventoryReference.RemoveFromInventory(PlayerReference.InventoryReference.StaticInventory[0].Key, 1);

            if (Throw){
                DroppedItem.GetComponent<Rigidbody>().AddForce(PlayerReference.CameraReference.transform.forward * 30.0f, ForceMode.Impulse);
            }
        }
    }

    public void GrabEnd(InputAction.CallbackContext Context){
        if (Context.canceled){
            //Debug.Log("Grab End");
        }
    }

    public void Look(InputAction.CallbackContext Context){
        //PlayerReference.CameraReference.transform.localEulerAngles += 
        //PlayerReference.CameraReference.transform.localEulerAngles += Vector3.right;
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
                RigidBodyReference.AddForce(new Vector3(0.0f, 80.0f, 0.0f), ForceMode.Impulse);

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

    public void EquipItem(InputAction.CallbackContext Context){
        if (Context.performed){
            PlayerReference.ToggleEquippedItem();
        }
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
