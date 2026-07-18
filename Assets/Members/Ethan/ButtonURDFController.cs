using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI-button-driven controller for ArticulationBody joints (e.g. URDF imports).
/// Assign up to six joints in the inspector. Hook UI Buttons to the
/// parameterless Joint#Plus / Joint#Minus methods (use EventTrigger
/// PointerDown/PointerUp for hold-to-move).
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

    [Header("Recording")]
    [Tooltip("Max joint error (deg) to consider neutral pose reached")]
    public float homeToleranceDeg = 1f;
    [Tooltip("Safety timeout (s) for homing move")]
    public float homeTimeout = 15f;

    [Header("Grab")]
    [Tooltip("GameObject that performs the grab (the gripper)")]
    [SerializeField] private Transform grabber;
    [Tooltip("Max distance (m) from grabber to a rigidbody's position for grab to attach")]
    public float grabDistance = 0.1f;
    [Tooltip("Optional: only grab bodies on these layers (0 = all)")]
    public LayerMask grabMask = ~0;

    [Header("Other")]
    public AudioSource servoSound;

    private ArticulationBody[] joints;
    private float[] targetPositions;
    // -1, 0, or +1 per joint; set by button press/release, consumed in Update
    private float[] inputs;

    // --- Recording state ---
    private struct Frame
    {
        public float[] signals;
        public float dt;
        public bool grab; // recorded grab state this frame
    }

    private RecState recState = RecState.Idle;
    private readonly List<Frame> recording = new List<Frame>();
    private List<Frame> savedRecording;
    private float[] neutralPositions;   // deg, captured at Start
    private float homingTimer;
    private int playIndex;
    private float playClock;

    // --- Grab state ---
    private bool grabActive;
    private Rigidbody grabbedBody;
    private ArticulationBody grabbedArtBody;
    private Transform grabbedTf;
    private Vector3 grabLocalPos;
    private Quaternion grabLocalRot;
    private bool grabbedWasKinematic;
    // recorded per-frame grab command: true = want-grabbed
    private bool grabInput;

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
                UpdateGrabFollow();
                DriveAllFromInputs(Time.deltaTime);
                break;

            case RecState.HomingToRecord:
            case RecState.HomingToPlay:
                UpdateGrabFollow();
                UpdateHoming();
                break;

            case RecState.Recording:
                UpdateGrabFollow();
                DriveAllFromInputs(Time.deltaTime);
                CaptureFrame(Time.deltaTime);
                break;

            case RecState.Playing:
                UpdateGrabFollow();
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
                // Clamp to joint limits so the target can't wind up past the
                // physical range (otherwise reversing direction stalls until
                // the accumulated overshoot is consumed).
                targetPositions[index] = ClampToJointLimits(body, drive, targetPositions[index]);
                drive.target = targetPositions[index];
                break;

            case DriveMode.Velocity:
                drive.targetVelocity = signal;
                break;
        }

        body.xDrive = drive;
    }

    // Clamps a target angle (deg) to the joint's allowed range, if limited.
    private static float ClampToJointLimits(ArticulationBody body, in ArticulationDrive drive, float targetDeg)
    {
        bool limited =
            (body.jointType == ArticulationJointType.RevoluteJoint && body.twistLock == ArticulationDofLock.LimitedMotion) ||
            (body.jointType == ArticulationJointType.PrismaticJoint &&
                (body.linearLockX == ArticulationDofLock.LimitedMotion ||
                 body.linearLockY == ArticulationDofLock.LimitedMotion ||
                 body.linearLockZ == ArticulationDofLock.LimitedMotion));

        if (!limited) return targetDeg;
        return Mathf.Clamp(targetDeg, drive.lowerLimit, drive.upperLimit);
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
        grabInput = false;
        if (grabActive) ReleaseGrab();
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
        grabInput = false;
        if (grabActive) ReleaseGrab();
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
        if (servoSound != null && !servoSound.isPlaying) servoSound.Play();
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

        if (servoSound != null) servoSound.Stop();

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
        var f = new Frame { signals = new float[joints.Length], dt = dt, grab = grabInput };
        for (int i = 0; i < joints.Length; i++)
            f.signals[i] = inputs[i] * speed;
        recording.Add(f);
    }

    private void UpdatePlayback()
    {
        playClock += Time.deltaTime;

        bool anySignal = false;

        while (playIndex < savedRecording.Count && playClock >= savedRecording[playIndex].dt)
        {
            var f = savedRecording[playIndex];
            for (int i = 0; i < joints.Length; i++)
            {
                DriveJoint(i, f.signals[i], f.dt);
                if (f.signals[i] != 0f) anySignal = true;
            }
            ApplyGrabState(f.grab);
            playClock -= f.dt;
            playIndex++;
        }

        if (playIndex < savedRecording.Count)
        {
            var cur = savedRecording[playIndex];
            for (int i = 0; i < cur.signals.Length && !anySignal; i++)
                if (cur.signals[i] != 0f) anySignal = true;
        }

        if (servoSound != null)
        {
            if (anySignal && !servoSound.isPlaying) servoSound.Play();
            else if (!anySignal && servoSound.isPlaying) servoSound.Stop();
        }

        if (playIndex >= savedRecording.Count)
        {
            if (servoSound != null) servoSound.Stop();
            recState = RecState.Idle;
            ClearInputs();
            if (driveMode == DriveMode.Velocity) ZeroVelocities();
        }
    }

    /// <summary>Toggle grab on/off. Wire to a "Grab" button (Button.onClick).</summary>
    public void ToggleGrab()
    {
        grabInput = !grabInput;
        ApplyGrabState(grabInput);
    }

    // Applies the desired grab state: attach nearest eligible body, or release.
    private void ApplyGrabState(bool want)
    {
        if (want && !grabActive) TryGrab();
        else if (!want && grabActive) ReleaseGrab();
    }

    private void TryGrab()
    {
        if (grabber == null) return;

        Rigidbody best = null;
        float bestSqr = grabDistance * grabDistance;
        Vector3 gp = grabber.position;

        Collider[] hits = Physics.OverlapSphere(gp, grabDistance, grabMask, QueryTriggerInteraction.Ignore);
        foreach (var c in hits)
        {
            var rb = c.attachedRigidbody;
            if (rb == null) continue;
            float d = (rb.worldCenterOfMass - gp).sqrMagnitude;
            if (d <= bestSqr) { bestSqr = d; best = rb; }
        }

        if (best == null) return;

        grabbedBody = best;
        grabbedTf = best.transform;
        grabbedWasKinematic = best.isKinematic;
        best.isKinematic = true;

        grabLocalPos = grabber.InverseTransformPoint(grabbedTf.position);
        grabLocalRot = Quaternion.Inverse(grabber.rotation) * grabbedTf.rotation;

        grabActive = true;
    }

    private void ReleaseGrab()
    {
        if (grabbedBody != null)
        {
            grabbedBody.isKinematic = grabbedWasKinematic;
            grabbedBody.linearVelocity = Vector3.zero;
            grabbedBody.angularVelocity = Vector3.zero;
        }
        grabbedBody = null;
        grabbedTf = null;
        grabActive = false;
    }

    private void UpdateGrabFollow()
    {
        if (!grabActive || grabber == null || grabbedTf == null) return;
        grabbedTf.position = grabber.TransformPoint(grabLocalPos);
        grabbedTf.rotation = grabber.rotation * grabLocalRot;
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
    public void Joint0Plus() { inputs[0] = 1f; servoSound.Play(); }
    public void Joint0Minus() { inputs[0] = -1f; servoSound.Play(); }
    public void Joint1Plus() { inputs[1] = 1f; servoSound.Play(); }
    public void Joint1Minus() { inputs[1] = -1f; servoSound.Play(); }
    public void Joint2Plus() { inputs[2] = 1f; servoSound.Play(); }
    public void Joint2Minus() { inputs[2] = -1f; servoSound.Play(); }
    public void Joint3Plus() { inputs[3] = 1f; servoSound.Play(); }
    public void Joint3Minus() { inputs[3] = -1f; servoSound.Play(); }
    public void Joint4Plus() { inputs[4] = 1f; servoSound.Play(); }
    public void Joint4Minus() { inputs[4] = -1f; servoSound.Play(); }
    public void Joint5Plus() { inputs[5] = 1f; servoSound.Play(); }
    public void Joint5Minus() { inputs[5] = -1f; servoSound.Play(); }

    // Release handlers — stop moving. Wire to EventTrigger PointerUp (and PointerExit for safety).
    public void Joint0Stop() { inputs[0] = 0f; servoSound.Stop(); }
    public void Joint1Stop() { inputs[1] = 0f; servoSound.Stop(); }
    public void Joint2Stop() { inputs[2] = 0f; servoSound.Stop(); }
    public void Joint3Stop() { inputs[3] = 0f; servoSound.Stop(); }
    public void Joint4Stop() { inputs[4] = 0f; servoSound.Stop(); }
    public void Joint5Stop() { inputs[5] = 0f; servoSound.Stop(); }

    // Speed Changers (they change da speed)
    public void SetSpeed1() { speed = 10f; servoSound.volume = .2f; }
    public void SetSpeed2() { speed = 20f; servoSound.volume = .4f; }
    public void SetSpeed3() { speed = 40f; servoSound.volume = .6f; }
    public void SetSpeed4() { speed = 60f; servoSound.volume = .8f; }

    #endregion
}