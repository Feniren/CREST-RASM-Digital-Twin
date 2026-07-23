using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR;

// Desktop fallback for the XRI lesson interactables: with no XR device, a left
// click raycasts from the screen-centre crosshair (Entity_Player locks the
// cursor) and selects the Component_Marker / Action_Interactable it hits.
// UI clicks are handled by the InputSystemUIInputModule, so clicks over UI are
// skipped here. Lives in Bootstrap; raycasts reach additively loaded modules.
public class Desktop_Click_Select : MonoBehaviour{
    const float MaxDistance = 30f;

    void Update(){
        if (XRSettings.isDeviceActive)
            return;

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // Markers use trigger colliders, so the query must include triggers.
        if (!Physics.Raycast(ray, out RaycastHit hit, MaxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            return;

        Component_Marker marker = hit.collider.GetComponentInParent<Component_Marker>();
        if (marker != null){
            marker.Notify_Selected();
            return;
        }

        Action_Interactable action = hit.collider.GetComponentInParent<Action_Interactable>();
        if (action != null)
            action.Notify_Clicked();
    }
}
