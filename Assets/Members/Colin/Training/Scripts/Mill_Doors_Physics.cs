using System.Collections;
using UnityEngine;

// Module 1 opens and closes the guard doors through this component — Mill_Demo_Controller sets
// a known state (Set_Open) around the milling demo and both Door_Click_Toggles toggle it
// (Item_Mill_Doors.AlternateInteract) on a marker click. On the PM8000 rig that slid two AxisMovement components; the Intellitek
// mill's doors are Rigidbodies on world-anchored ConfigurableJoints instead, so this subclass
// keeps the same call surface and slides each door along its panel's joint axis.
//
// A door is its whole assembly under ClassifyIntellitekMill/Enclosure/doors — the panel plus the
// hardware fixed to it — and the assembly carries the door's Rigidbody and joint (the classifier
// moves them up from the panel), so sliding or grabbing a door carries its hardware. The doors
// stay physics objects: they are only kinematic while a scripted slide is running, so grabbing
// still works the rest of the time. Motion is along each door's local X (the joint's configured
// axis, world Z on this machine) and stays inside the joint's linear limit.
public class Mill_Doors_Physics : Item_Mill_Doors{
    [SerializeField] private Transform LeftDoor;
    [SerializeField] private Transform RightDoor;

    // The joints' linear limit (0.2 m). Sliding further only lets the joint pull the door back
    // to it once the door is dynamic again.
    [SerializeField] private float SlideDistance = 0.2f;
    [SerializeField] private float SlideSeconds = 0.8f;

    private bool _isOpen;
    private Door_Slide _left;
    private Door_Slide _right;
    private Coroutine _slide;

    public bool IsOpen => _isOpen;
    public bool IsSliding => _slide != null;

    private void Awake(){
        if (LeftDoor != null)
            _left = new Door_Slide(LeftDoor);

        if (RightDoor != null)
            _right = new Door_Slide(RightDoor);
    }

    public override void AlternateInteract(Entity_Player PlayerReference){
        Set_Open(!_isOpen);
    }

    // Slides to a known state whatever the toggle history — a door dragged open by hand
    // never updates _isOpen, so the milling demo asks for "closed" rather than "toggle".
    public void Set_Open(bool open){
        if (_left == null || _right == null){
            Debug.LogWarning("Mill_Doors_Physics: LeftDoor/RightDoor are not assigned.");
            return;
        }

        _isOpen = open;

        if (_slide != null)
            StopCoroutine(_slide);

        _slide = StartCoroutine(Slide(_isOpen ? SlideDistance : 0f));
    }

    // The two doors part in opposite directions along the shared axis.
    private IEnumerator Slide(float offset){
        _left.Begin(offset);
        _right.Begin(-offset);

        for (float elapsed = 0f; elapsed < SlideSeconds; elapsed += Time.deltaTime){
            float k = Mathf.SmoothStep(0f, 1f, elapsed / SlideSeconds);
            _left.Step(k);
            _right.Step(k);
            yield return null;
        }

        _left.End();
        _right.End();
        _slide = null;
    }

    // One door body. Its dynamic/kinematic state is taken once at load, so a slide interrupted
    // by the next Set_Open can't leave the door stuck kinematic.
    private class Door_Slide{
        private readonly Rigidbody body;
        private readonly Vector3 closed;
        private readonly bool kinematic;
        private Vector3 from;
        private Vector3 target;

        public Door_Slide(Transform door){
            body = door.GetComponent<Rigidbody>();
            closed = door.position;
            kinematic = body.isKinematic;
        }

        public void Begin(float offset){
            from = body.transform.position;
            target = closed + body.transform.right * offset;
            body.isKinematic = true;
        }

        public void Step(float k){
            body.transform.position = Vector3.Lerp(from, target, k);
        }

        public void End(){
            Step(1f);
            body.isKinematic = kinematic;
        }
    }
}
