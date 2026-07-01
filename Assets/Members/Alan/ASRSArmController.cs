using UnityEngine;

public class ASRSArmController : MonoBehaviour
{
    [Header("Axis Transforms")]
    [SerializeField] private Transform armZ;
    [SerializeField] private Transform armY;
    [SerializeField] private Transform armX;

    [Header("Speeds")]
    [SerializeField] private float moveSpeed = 0.5f;
    [SerializeField] private float rotateSpeed = 60f;

    private float? targetZ;
    private float? targetY;
    private float? targetX;
    private float? targetYaw;

    public Transform ArmZ => armZ;
    public Transform ArmY => armY;
    public Transform ArmX => armX;

    public bool IsMoving =>
        targetZ.HasValue || targetY.HasValue || targetX.HasValue || targetYaw.HasValue;

    private void Update()
    {
        MoveAxis(armZ, ref targetZ, Axis.Z);
        MoveAxis(armY, ref targetY, Axis.Y);
        MoveAxis(armX, ref targetX, Axis.X);
        RotateYaw();
    }

    public void MoveZ(float localZ)
    {
        targetZ = localZ;
    }

    public void MoveY(float localY)
    {
        targetY = localY;
    }

    public void MoveX(float localX)
    {
        targetX = localX;
    }

    public void RotateY(float angle = 180f)
    {
        targetYaw = angle;
    }

    public void StopAll()
    {
        targetZ = null;
        targetY = null;
        targetX = null;
        targetYaw = null;
    }

    private enum Axis
    {
        X,
        Y,
        Z
    }

    private void MoveAxis(Transform target, ref float? targetValue, Axis axis)
    {
        if (target == null || !targetValue.HasValue)
            return;

        Vector3 pos = target.localPosition;

        float current = GetAxis(pos, axis);
        float next = Mathf.MoveTowards(current, targetValue.Value, moveSpeed * Time.deltaTime);

        SetAxis(ref pos, axis, next);
        target.localPosition = pos;

        if (Mathf.Approximately(next, targetValue.Value))
            targetValue = null;
    }

    private void RotateYaw()
    {
        if (armY == null || !targetYaw.HasValue)
            return;

        Vector3 rot = armY.localEulerAngles;

        float next = Mathf.MoveTowardsAngle(rot.y, targetYaw.Value, rotateSpeed * Time.deltaTime);
        rot.y = next;
        armY.localEulerAngles = rot;

        if (Mathf.Abs(Mathf.DeltaAngle(next, targetYaw.Value)) < 0.1f)
            targetYaw = null;
    }

    private float GetAxis(Vector3 value, Axis axis)
    {
        switch (axis)
        {
            case Axis.X: return value.x;
            case Axis.Y: return value.y;
            case Axis.Z: return value.z;
            default: return 0f;
        }
    }

    private void SetAxis(ref Vector3 value, Axis axis, float axisValue)
    {
        switch (axis)
        {
            case Axis.X:
                value.x = axisValue;
                break;

            case Axis.Y:
                value.y = axisValue;
                break;

            case Axis.Z:
                value.z = axisValue;
                break;
        }
    }
}