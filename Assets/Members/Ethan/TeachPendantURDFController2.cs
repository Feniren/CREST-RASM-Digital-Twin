using UnityEngine;

/// <summary>
/// UI-button-driven controller for ArticulationBody joints (e.g. URDF imports).
/// Assign up to six joints in the inspector. Hook UI Buttons to the
/// parameterless Joint#Plus / Joint#Minus methods (use EventTrigger
/// PointerDown/PointerUp for hold-to-move, or plain Button.onClick for steps).
/// </summary>
public class ButtonURDFController : MonoBehaviour
{
    public enum DriveMode { Position, Velocity }

    [Header("Joints (assign in inspector)")]
    [SerializeField] private ArticulationBody joint0;
    [SerializeField] private ArticulationBody joint1;
    [SerializeField] private ArticulationBody joint2;
    [SerializeField] private ArticulationBody joint3;
    [SerializeField] private ArticulationBody joint4;
    [SerializeField] private ArticulationBody joint5;

    [Header("Drive Parameters")]
    public DriveMode driveMode = DriveMode.Position;
    public float stiffness = 10000f;
    public float damping = 100f;
    public float forceLimit = 1000f;
    public float speed = 5f;        // deg/s
    public float acceleration = 5f; // deg/s^2

    private ArticulationBody[] joints;
    private float[] targetPositions;
    // -1, 0, or +1 per joint; set by button press/release, consumed in Update
    private float[] inputs;

    void Start()
    {
        joints = new[] { joint0, joint1, joint2, joint3, joint4, joint5 };
        targetPositions = new float[joints.Length];
        inputs = new float[joints.Length];

        for (int i = 0; i < joints.Length; i++)
        {
            if (joints[i] == null) continue;

            joints[i].jointFriction = 10f;
            joints[i].angularDamping = 10f;
            ApplyDriveSettings(joints[i]);

            if (joints[i].jointType != ArticulationJointType.FixedJoint)
                targetPositions[i] = joints[i].jointPosition[0] * Mathf.Rad2Deg;
        }
    }

    void Update()
    {
        for (int i = 0; i < joints.Length; i++)
            DriveJoint(i, inputs[i]);
    }

    private void DriveJoint(int index, float direction)
    {
        var body = joints[index];
        if (body == null || body.jointType == ArticulationJointType.FixedJoint) return;

        ArticulationDrive drive = body.xDrive;

        switch (driveMode)
        {
            case DriveMode.Position:
                targetPositions[index] += direction * speed * Time.deltaTime;
                drive.target = targetPositions[index];
                break;

            case DriveMode.Velocity:
                drive.targetVelocity = direction * speed;
                break;
        }

        body.xDrive = drive;
    }

    private void ApplyDriveSettings(ArticulationBody body)
    {
        ArticulationDrive drive = body.xDrive;
        drive.forceLimit = forceLimit;

        if (driveMode == DriveMode.Position)
        {
            drive.stiffness = stiffness;
            drive.damping = damping;
            drive.driveType = ArticulationDriveType.Target;
        }
        else
        {
            drive.stiffness = 0f;
            drive.damping = damping;
            drive.driveType = ArticulationDriveType.Velocity;
        }

        body.xDrive = drive;
    }

    #region Button Hooks (parameterless)

    // Press handlers — start moving. Wire to EventTrigger PointerDown.
    public void Joint0Plus() { inputs[0] = 1f; }
    public void Joint0Minus() { inputs[0] = -1f; }
    public void Joint1Plus() { inputs[1] = 1f; }
    public void Joint1Minus() { inputs[1] = -1f; }
    public void Joint2Plus() { inputs[2] = 1f; }
    public void Joint2Minus() { inputs[2] = -1f; }
    public void Joint3Plus() { inputs[3] = 1f; }
    public void Joint3Minus() { inputs[3] = -1f; }
    public void Joint4Plus() { inputs[4] = 1f; }
    public void Joint4Minus() { inputs[4] = -1f; }
    public void Joint5Plus() { inputs[5] = 1f; }
    public void Joint5Minus() { inputs[5] = -1f; }

    // Release handlers — stop moving. Wire to EventTrigger PointerUp (and PointerExit for safety).
    public void Joint0Stop() { inputs[0] = 0f; }
    public void Joint1Stop() { inputs[1] = 0f; }
    public void Joint2Stop() { inputs[2] = 0f; }
    public void Joint3Stop() { inputs[3] = 0f; }
    public void Joint4Stop() { inputs[4] = 0f; }
    public void Joint5Stop() { inputs[5] = 0f; }

    // Speed Changers (they change da speed)
    public void SetSpeed1() { speed = 5f; }
    public void SetSpeed2() { speed = 10f; }
    public void SetSpeed3() { speed = 20f; }
    public void SetSpeed4() { speed = 40f; }

    // Single-step alternatives — one discrete nudge per click. Wire to Button.onClick.
    public void Joint0StepPlus() { Step(0, 1f); }
    public void Joint0StepMinus() { Step(0, -1f); }
    public void Joint1StepPlus() { Step(1, 1f); }
    public void Joint1StepMinus() { Step(1, -1f); }
    public void Joint2StepPlus() { Step(2, 1f); }
    public void Joint2StepMinus() { Step(2, -1f); }
    public void Joint3StepPlus() { Step(3, 1f); }
    public void Joint3StepMinus() { Step(3, -1f); }
    public void Joint4StepPlus() { Step(4, 1f); }
    public void Joint4StepMinus() { Step(4, -1f); }
    public void Joint5StepPlus() { Step(5, 1f); }
    public void Joint5StepMinus() { Step(5, -1f); }

    [Header("Step Mode")]
    [Tooltip("Degrees moved per single-step button click (Position mode only)")]
    public float stepDegrees = 5f;

    private void Step(int index, float direction)
    {
        var body = joints[index];
        if (body == null || body.jointType == ArticulationJointType.FixedJoint) return;
        if (driveMode != DriveMode.Position) return;

        targetPositions[index] += direction * stepDegrees;
        ArticulationDrive drive = body.xDrive;
        drive.target = targetPositions[index];
        body.xDrive = drive;
    }

    #endregion
}