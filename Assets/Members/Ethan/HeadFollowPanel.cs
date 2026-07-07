using UnityEngine;

public class HeadFollowPanel : MonoBehaviour
{
    [SerializeField] Transform head;        // XR Origin > Camera
    [SerializeField] float distance = 1.5f;
    [SerializeField] float followSpeed = 3f;
    [SerializeField] float deadzoneAngle = 15f; // won't move until gaze drifts past this

    Vector3 targetPos;
    Quaternion targetRot;

    void LateUpdate()
    {
        Vector3 desired = head.position + head.forward * distance;

        float angle = Vector3.Angle(head.forward, transform.position - head.position);
        if (angle > deadzoneAngle)
        {
            targetPos = desired;
            targetRot = Quaternion.LookRotation(transform.position - head.position);
        }

        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(transform.position - head.position), followSpeed * Time.deltaTime);
    }
}