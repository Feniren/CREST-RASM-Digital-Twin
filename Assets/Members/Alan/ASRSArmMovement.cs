using UnityEngine;

public class ASRSArmMovement : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    public enum MovementAxis { X, Y, Z }

        [Header("Axis Configuration")]
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

        public float CurrentPosition => GetAxisValue(transform.position);

        public bool IsMoving => _targetPosition.HasValue;

        public float OffsetFromOrigin => CurrentPosition - _originPosition;

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

        public void MoveToTarget(float worldPosition)
        {
            _targetPosition = Clamp(worldPosition);
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
            float delta = value - GetAxisValue(transform.position);
            transform.position = SetAxisValue(transform.position, value);

            if (dependents != null)
            {
                foreach (Transform dep in dependents)
    {
                    if (dep != null)
                        dep.position = SetAxisValue(dep.position, GetAxisValue(dep.position) + delta);
                }
            }
        }
        
        private float GetAxisValue(Vector3 pos)
        {
            if (axis == MovementAxis.X) return pos.x;
            if (axis == MovementAxis.Z) return pos.z;
            return pos.y;
    }

        private Vector3 SetAxisValue(Vector3 pos, float value)
    {
            if (axis == MovementAxis.X) pos.x = value;
            else if (axis == MovementAxis.Z) pos.z = value;
            else pos.y = value;
            return pos;
        }
        
        private float Clamp(float value)
        {
            if (!enableLimits)
                return value;
            return Mathf.Clamp(value, _originPosition + minOffset, _originPosition + maxOffset);
    }
}
