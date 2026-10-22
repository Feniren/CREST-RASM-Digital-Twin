using UnityEngine;

public class Face_Camera : MonoBehaviour{
    [SerializeField] private bool flip180 = true;

    private Camera cam;

    private void LateUpdate(){
        if (cam == null){
            cam = Camera.main;

            if (cam == null)
                return;
        }

        Vector3 direction = transform.position - cam.transform.position;

        if (!flip180)
            direction = -direction;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }
}
