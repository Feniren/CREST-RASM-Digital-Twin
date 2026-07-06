using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI-button-driven controller for ArticulationBody joints (e.g. URDF imports).
/// Assign up to six joints in the inspector. Hook UI Buttons to the
/// parameterless Joint#Plus / Joint#Minus methods (use EventTrigger
/// PointerDown/PointerUp for hold-to-move, or plain Button.onClick for steps).
///
/// Recording:
///   Record()     -> returns robot to neutral pose, then records button input each frame.
///   StopRecord() -> stops and saves the recording (in memory).
///   Playback()   -> returns robot to neutral pose, then replays recorded input.
/// Works in Position and Velocity mode. Velocity-mode playback replays commanded
/// velocities, so small drift vs. original run is expected.
/// </summary>
public class ButtonURDFController : MonoBehaviour
{
    public enum DriveMode { Position, Velocity }

    private enum RecState { Idle, HomingToRecord, Recording, HomingToPlay, Playing }

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

    [Header("Step Mode")]
    [Tooltip("Degrees moved per single-step button click (Position mode only)")]
    public float stepDegrees = 5f;

    [Header("Recording")]
    [Tooltip("Max joint error (deg) to consider neutral pose reached")]
    public float homeToleranceDeg = 1f;
    [Tooltip("Safety timeout (s) for homing move")]
    public float homeTimeout = 15f;

    private ArticulationBody[] joints;
    private float[] targetPositions;
    // -1, 0, or +1 per joint; set by button press/release, consumed in Update
    private float[] inputs;

    // --- Recording state ---
    private struct Frame
    {
        public float[] signals; // per-joint commanded angular velocity (deg/s) = input * speed at record time
        public float dt;
    }

    private RecState recState = RecState.Idle;
    private readonly List<Frame> recording = new List<Frame>();
    private List<Frame> savedRecording;
    private float[] neutralPositions;   // deg, captured at Start
    private float homingTimer;
    private int playIndex;
    private float playClock;

    void Start()
    {
        joints = new[] { joint0, joint1, joint2, joint3, joint4, joint5 };
        targetPositions = new float[joints.Length];
        inputs = new float[joints.Length];
        neutralPositions = new float[joints.Length];

        for (int i = 0; i < joints.Length; i++)
        {
            if (joints[i] == null) continue;

            joints[i].jointFriction = 10f;
            joints[i].angularDamping = 10f;
            ApplyDriveSettings(joints[i]);

            if (joints[i].jointType != ArticulationJointType.FixedJoint)
            {
                targetPositions[i] = joints[i].jointPosition[0] * Mathf.Rad2Deg;
                neutralPositions[i] = targetPositions[i];
            }
        }
    }

    void Update()
    {
        switch (recState)
        {
            case RecState.Idle:
                DriveAllFromInputs(Time.deltaTime);
                break;

            case RecState.HomingToRecord:
            case RecState.HomingToPlay:
                UpdateHoming();
                break;

            case RecState.Recording:
                DriveAllFromInputs(Time.deltaTime);
                CaptureFrame(Time.deltaTime);
                break;

            case RecState.Playing:
                UpdatePlayback();
                break;
        }
    }

    private void DriveAllFromInputs(float dt)
    {
        for (int i = 0; i < joints.Length; i++)
            DriveJoint(i, inputs[i] * speed, dt);
    }

    // signal = commanded angular velocity in deg/s
    private void DriveJoint(int index, float signal, float dt)
    {
        var body = joints[index];
        if (body == null || body.jointType == ArticulationJointType.FixedJoint) return;

        ArticulationDrive drive = body.xDrive;

        switch (driveMode)
        {
            case DriveMode.Position:
                targetPositions[index] += signal * dt;
                drive.target = targetPositions[index];
                break;

            case DriveMode.Velocity:
                drive.targetVelocity = signal;
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

    #region Recording / Playback

    /// <summary>Move to neutral pose, then start recording. Wire to a "Record" button.</summary>
    public void Record()
    {
        if (recState != RecState.Idle) return;
        recording.Clear();
        BeginHoming(RecState.HomingToRecord);
    }

    /// <summary>Stop recording and save it. Wire to a "Stop" button.</summary>
    public void StopRecord()
    {
        if (recState != RecState.Recording) return;
        savedRecording = new List<Frame>(recording);
        recState = RecState.Idle;
        ClearInputs();
        if (driveMode == DriveMode.Velocity) ZeroVelocities();
    }

    /// <summary>Move to neutral pose, then replay saved recording. Wire to a "Playback" button.</summary>
    public void Playback()
    {
        if (recState != RecState.Idle) return;
        if (savedRecording == null || savedRecording.Count == 0)
        {
            Debug.LogWarning("No recording saved.");
            return;
        }
        BeginHoming(RecState.HomingToPlay);
    }

    private void BeginHoming(RecState pending)
    {
        recState = pending;
        homingTimer = 0f;
        ClearInputs();

        // Force position drives for the homing move (even in Velocity mode).
        for (int i = 0; i < joints.Length; i++)
        {
            var body = joints[i];
            if (body == null || body.jointType == ArticulationJointType.FixedJoint) continue;

            ArticulationDrive drive = body.xDrive;
            drive.driveType = ArticulationDriveType.Target;
            drive.stiffness = stiffness;
            drive.damping = damping;
            drive.forceLimit = forceLimit;
            drive.targetVelocity = 0f;
            drive.target = neutralPositions[i];
            body.xDrive = drive;
        }
    }

    private void UpdateHoming()
    {
        homingTimer += Time.deltaTime;
        bool arrived = true;

        for (int i = 0; i < joints.Length; i++)
        {
            var body = joints[i];
            if (body == null || body.jointType == ArticulationJointType.FixedJoint) continue;

            float posDeg = body.jointPosition[0] * Mathf.Rad2Deg;
            if (Mathf.Abs(posDeg - neutralPositions[i]) > homeToleranceDeg)
            {
                arrived = false;
                break;
            }
        }

        if (!arrived && homingTimer < homeTimeout) return;

        // Snap logical targets to neutral so Position mode resumes cleanly.
        for (int i = 0; i < joints.Length; i++)
            targetPositions[i] = neutralPositions[i];

        // Restore configured drive mode.
        for (int i = 0; i < joints.Length; i++)
            if (joints[i] != null) ApplyDriveSettings(joints[i]);

        if (recState == RecState.HomingToRecord)
        {
            ClearInputs(); // ignore anything pressed during homing
            recState = RecState.Recording;
        }
        else
        {
            playIndex = 0;
            playClock = 0f;
            recState = RecState.Playing;
        }
    }

    private void CaptureFrame(float dt)
    {
        var f = new Frame { signals = new float[joints.Length], dt = dt };
        for (int i = 0; i < joints.Length; i++)
            f.signals[i] = inputs[i] * speed; // bakes in mid-recording speed changes
        recording.Add(f);
    }

    private void UpdatePlayback()
    {
        // Consume recorded frames against real elapsed time so playback duration matches recording.
        playClock += Time.deltaTime;

        while (playIndex < savedRecording.Count && playClock >= savedRecording[playIndex].dt)
        {
            var f = savedRecording[playIndex];
            for (int i = 0; i < joints.Length; i++)
                DriveJoint(i, f.signals[i], f.dt);
            playClock -= f.dt;
            playIndex++;
        }

        if (playIndex >= savedRecording.Count)
        {
            recState = RecState.Idle;
            ClearInputs();
            if (driveMode == DriveMode.Velocity) ZeroVelocities();
        }
    }

    private void ClearInputs()
    {
        for (int i = 0; i < inputs.Length; i++) inputs[i] = 0f;
    }

    private void ZeroVelocities()
    {
        for (int i = 0; i < joints.Length; i++)
        {
            var body = joints[i];
            if (body == null || body.jointType == ArticulationJointType.FixedJoint) continue;
            ArticulationDrive drive = body.xDrive;
            drive.targetVelocity = 0f;
            body.xDrive = drive;
        }
    }

    #endregion

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