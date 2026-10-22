using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR;

// Desktop fallback for the XRI lesson interactables: with no XR device, the
// screen-centre crosshair (Entity_Player locks the cursor) hovers the
// Component_Marker it points at, and a left click selects the Component_Marker /
// Action_Interactable it hits. UI clicks are handled by the
// InputSystemUIInputModule, so the crosshair is ignored over UI. Lives in
// Bootstrap; raycasts reach additively loaded modules.
public class Desktop_Click_Select : MonoBehaviour{
    const float MaxDistance = 30f;

    Component_Marker hovered;

    void Update(){
        if (XRSettings.isDeviceActive){
            Set_Hovered(null);
            return;
        }

        Camera cam = Camera.main;

        if (cam == null || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())){
            Set_Hovered(null);
            return;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // Markers use trigger colliders, so the query must include triggers.
        if (!Physics.Raycast(ray, out RaycastHit hit, MaxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide)){
            Set_Hovered(null);
            return;
        }

        Component_Marker marker = hit.collider.GetComponentInParent<Component_Marker>();
        Set_Hovered(marker);

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (marker != null){
            marker.Notify_Selected();
            return;
        }

        Action_Interactable action = hit.collider.GetComponentInParent<Action_Interactable>();
        if (action != null)
            action.Notify_Clicked();
    }

    void Set_Hovered(Component_Marker marker){
        if (marker == hovered)
            return;

        if (hovered != null)
            hovered.Notify_Hover(false);

        hovered = marker;

        if (hovered != null)
            hovered.Notify_Hover(true);
    }
}
