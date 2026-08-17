using System.Collections;
using UnityEngine;

// Module 1 opens and closes the guard doors through Item_Mill_Doors.AlternateInteract —
// Mill_Demo_Controller calls it around the milling demo and both Door_Click_Toggles call it
// on a marker click. On the PM8000 rig that slid two AxisMovement components; the Intellitek
// mill's doors are Rigidbodies on world-anchored ConfigurableJoints instead, so this subclass
// keeps the same call surface and slides the bodies along the joint axis.
//
// The doors stay physics objects: they are only kinematic while a scripted slide is running,
// so grabbing still works the rest of the time. Motion is along each door's local X (the
// joint's configured axis, world Z on this machine) and stays inside the joint's linear limit.
public class Mill_Doors_Physics : Item_Mill_Doors{
    [SerializeField] private Rigidbody LeftDoor;
    [SerializeField] private Rigidbody RightDoor;

    // The panels are 0.355 m wide, so 0.3 m each clears the opening without reaching the
    // joint limit.
    [SerializeField] private float SlideDistance = 0.3f;
    [SerializeField] private float SlideSeconds = 0.8f;

    private bool _isOpen;
    private Vector3 _leftClosed;
    private Vector3 _rightClosed;
    private Coroutine _slide;

    public bool IsOpen => _isOpen;

    private void Awake(){
        if (LeftDoor != null)
            _leftClosed = LeftDoor.transform.position;

        if (RightDoor != null)
            _rightClosed = RightDoor.transform.position;
    }

    public override void AlternateInteract(Entity_Player PlayerReference){
        if (LeftDoor == null || RightDoor == null){
            Debug.LogWarning("Mill_Doors_Physics: LeftDoor/RightDoor are not assigned.");
            return;
        }

        _isOpen = !_isOpen;

        if (_slide != null)
            StopCoroutine(_slide);

        _slide = StartCoroutine(Slide(_isOpen ? SlideDistance : 0f));
    }

    // The two doors part in opposite directions along the shared axis.
    private IEnumerator Slide(float offset){
        Vector3 leftTarget = _leftClosed + LeftDoor.transform.right * offset;
        Vector3 rightTarget = _rightClosed - RightDoor.transform.right * offset;
        Vector3 leftFrom = LeftDoor.transform.position;
        Vector3 rightFrom = RightDoor.transform.position;

        bool leftWasKinematic = LeftDoor.isKinematic;
        bool rightWasKinematic = RightDoor.isKinematic;
        LeftDoor.isKinematic = true;
        RightDoor.isKinematic = true;

        for (float elapsed = 0f; elapsed < SlideSeconds; elapsed += Time.deltaTime){
            float k = Mathf.SmoothStep(0f, 1f, elapsed / SlideSeconds);
            LeftDoor.transform.position = Vector3.Lerp(leftFrom, leftTarget, k);
            RightDoor.transform.position = Vector3.Lerp(rightFrom, rightTarget, k);
            yield return null;
        }

        LeftDoor.transform.position = leftTarget;
        RightDoor.transform.position = rightTarget;
        LeftDoor.isKinematic = leftWasKinematic;
        RightDoor.isKinematic = rightWasKinematic;
        _slide = null;
    }
}
