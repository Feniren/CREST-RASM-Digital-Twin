using UnityEngine;
using UnityEngine.InputSystem;

// Player_Controller hooks the RIGHT grip's release to ThrowItem (the desktop
// equipped-item throw) instead of GrabEnd, so anything picked up with the
// right controller's grip is never let go. This listens to the same right
// grip button itself and lets go through the hand's own
// Entity_XR_Hand.GrabEnd — exactly what the left grip already does — without
// touching the shared Player_Controller.
//
// Safe if Player_Controller is fixed later: GrabEnd does nothing once the
// hand is already free.
[DisallowMultipleComponent]
public class XR_Right_Grip_Release : MonoBehaviour
{
    private InputAction rightGrip;
    private Entity_XR_Hand hand;

    // Adds one to 'host' unless the scene already has one.
    public static void Ensure(Component host)
    {
        if (host != null && FindFirstObjectByType<XR_Right_Grip_Release>(FindObjectsInactive.Include) == null)
            host.gameObject.AddComponent<XR_Right_Grip_Release>();
    }

    private void Awake()
    {
        // Same binding as the project's XRRightGrab action.
        rightGrip = new InputAction("Right Grip Release Fix", InputActionType.Button, "<XRController>{RightHand}/{GripButton}");
        rightGrip.canceled += _ => Release();
    }

    private void OnEnable() => rightGrip.Enable();
    private void OnDisable() => rightGrip.Disable();
    private void OnDestroy() => rightGrip.Dispose();

    private void Release()
    {
        if (hand == null)
        {
            Player_Controller player = FindFirstObjectByType<Player_Controller>();
            if (player != null && player.RightHand != null)
                hand = player.RightHand.GetComponent<Entity_XR_Hand>();
        }

        if (hand != null)
            hand.GrabEnd();
    }
}
