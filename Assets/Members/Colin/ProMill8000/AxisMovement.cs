using UnityEngine;

namespace ProMill8000
{
    public enum MovementAxis { X, Y, Z }

    /// <summary>
    /// Slides this transform, plus any dependents, along one of its OWN local axes. The axis is
    /// declared in the drive's frame, so a mill instance can sit at any rotation in a scene and
    /// the declaration stays right; anything that needs the direction in world space reads
    /// WorldDirection. Positions along the axis are scalar projections onto that direction.
    /// </summary>
    public class AxisMovement : MonoBehaviour
    {
        [Header("Axis Configuration")]
        [Tooltip("Local axis of this transform to slide along")]
        [SerializeField] private MovementAxis axis = MovementAxis.Y;
        [SerializeField] private float speed = 1f;

        [Header("Coupled Movement")]
        [Tooltip("Transforms that ride on this axis (moved along with this object)")]
        [SerializeField] private Transform[] dependents;

        [Header("Limits (optional)")]
        [SerializeField] private bool enableLimits;
        [SerializeField] private float minOffset = -1f;
        [SerializeField] private float maxOffset = 1f;

        private float _originPosition;
        private float? _targetPosition;

        public float CurrentPosition => Vector3.Dot(transform.position, WorldDirection);

        public bool IsMoving => _targetPosition.HasValue;

        public float OffsetFromOrigin => CurrentPosition - _originPosition;

        public MovementAxis Axis => axis;

        /// <summary>Unit vector of the drive axis in world space.</summary>
        public Vector3 WorldDirection => transform.TransformDirection(LocalDirection);

        private Vector3 LocalDirection
        {
            get
            {
                if (axis == MovementAxis.X) return Vector3.right;
                if (axis == MovementAxis.Z) return Vector3.forward;
                return Vector3.up;
            }
        }

        private void Awake()
        {
            _originPosition = CurrentPosition;
        }

        private void Update()
        {
            if (!_targetPosition.HasValue)
                return;

            float current = CurrentPosition;
            float target = _targetPosition.Value;
            float next = Mathf.MoveTowards(current, target, speed * Time.deltaTime);
            ApplyPosition(next);

            if (Mathf.Approximately(next, target))
                _targetPosition = null;
        }

        public void MoveBy(float delta)
        {
            float newPos = CurrentPosition + delta;
            ApplyPosition(Clamp(newPos));
        }

        public void MoveToTarget(float axisPosition)
        {
            _targetPosition = Clamp(axisPosition);
        }

        public void MoveToOffset(float offset)
        {
            MoveToTarget(_originPosition + offset);
        }

        public void Stop()
        {
            _targetPosition = null;
        }

        public void ResetToOrigin()
        {
            Stop();
            ApplyPosition(_originPosition);
        }

        private void ApplyPosition(float value)
        {
            Vector3 direction = WorldDirection;
            float delta = value - Vector3.Dot(transform.position, direction);
            transform.position += direction * delta;

            if (dependents != null)
            {
                foreach (Transform dep in dependents)
                {
                    if (dep != null)
                        dep.position += direction * delta;
                }
            }
        }

        private float Clamp(float value)
        {
            if (!enableLimits)
                return value;
            return Mathf.Clamp(value, _originPosition + minOffset, _originPosition + maxOffset);
        }
    }
}
