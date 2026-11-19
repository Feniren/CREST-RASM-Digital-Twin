using System.Collections;
using UnityEngine;

// Slides the lathe's guard doors (Lathe/LatheOutterShell/LeftLatheDoor + RightLatheDoor) shut for
// Lathe_Demo_Controller's turning demo and back open afterwards. The doors rest open in the
// prefab and are dynamic Rigidbodies on world-anchored ConfigurableJoints (local Y free, ±0.1 m),
// so the trainee can still drag them. The closed pose is outside that limit, so a door stays
// kinematic from the moment it starts closing until it is back at its open rest pose; only
// then is its original physics state restored.
public class Lathe_Doors : MonoBehaviour{
    [SerializeField] private Transform LeftDoor;
    [SerializeField] private Transform RightDoor;

    // Signed travel from the open rest pose to closed, along each door's local Y (world X here).
    [SerializeField] private float LeftCloseDistance = -0.322f;
    [SerializeField] private float RightCloseDistance = 0.329f;
    [SerializeField] private float SlideSeconds = 1.2f;

    private Door_Slide _left;
    private Door_Slide _right;
    private Coroutine _slide;
    private bool _isOpen = true;

    public bool IsOpen => _isOpen;
    public bool IsSliding => _slide != null;

    private void Awake(){
        if (LeftDoor == null || RightDoor == null){
            Debug.LogWarning("Lathe_Doors: LeftDoor and RightDoor must be assigned.");
            enabled = false;
            return;
        }

        _left = new Door_Slide(LeftDoor, LeftCloseDistance);
        _right = new Door_Slide(RightDoor, RightCloseDistance);
    }

    public void Set_Open(bool open){
        if (!enabled)
            return;

        _isOpen = open;

        if (_slide != null)
            StopCoroutine(_slide);

        _slide = StartCoroutine(Slide(open));
    }

    private IEnumerator Slide(bool open){
        _left.Begin(open);
        _right.Begin(open);

        for (float elapsed = 0f; elapsed < SlideSeconds; elapsed += Time.deltaTime){
            float k = Mathf.SmoothStep(0f, 1f, elapsed / SlideSeconds);
            _left.Step(k);
            _right.Step(k);
            yield return null;
        }

        _left.End(open);
        _right.End(open);
        _slide = null;
    }

    // One door. Its open pose and physics state are taken once at load, so an interrupted
    // slide can't leave it stuck kinematic or drifting from its rest pose.
    private class Door_Slide{
        private readonly Transform door;
        private readonly Rigidbody body;
        private readonly Vector3 open;
        private readonly Vector3 closed;
        private readonly bool kinematic;
        private Vector3 from;
        private Vector3 target;

        public Door_Slide(Transform door, float closeDistance){
            this.door = door;
            body = door.GetComponent<Rigidbody>();
            open = door.position;
            closed = open + door.up * closeDistance;
            kinematic = body != null && body.isKinematic;
        }

        public void Begin(bool toOpen){
            from = door.position;
            target = toOpen ? open : closed;
            if (body != null)
                body.isKinematic = true;
        }

        public void Step(float k){
            door.position = Vector3.Lerp(from, target, k);
        }

        public void End(bool toOpen){
            Step(1f);
            if (body != null && toOpen)
                body.isKinematic = kinematic;
        }
    }
}
