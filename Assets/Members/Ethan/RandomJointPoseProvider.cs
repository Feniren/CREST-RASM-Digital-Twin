using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Emits random joint positions within each joint's limits when triggered.
/// Positions only; velocity/effort left zeroed (consumer teleports).
/// </summary>
public class RandomJointPoseProvider : MonoBehaviour, IJointStateProvider
{
    [Tooltip("Fallback bounds (radians) for joints without explicit limits")]
    public float fallbackMin = -3.14159f;
    public float fallbackMax = 3.14159f;

    public event Action<JointStateMessage> OnJointStateReceived;

    private string[] jointNames;
    private float[] lowerLimits;
    private float[] upperLimits;
    private JointStateMessage latestMessage;

    void Start()
    {
        var bodies = GetComponentsInChildren<ArticulationBody>();
        var names = new List<string>();
        var los = new List<float>();
        var his = new List<float>();

        foreach (var body in bodies)
        {
            if (body.jointType == ArticulationJointType.FixedJoint)
                continue;

            names.Add(body.name);

            var drive = body.xDrive;
            bool isAngular = body.jointType == ArticulationJointType.RevoluteJoint
                || body.jointType == ArticulationJointType.SphericalJoint;
            float lo = drive.lowerLimit;
            float hi = drive.upperLimit;

            if (isAngular) { lo *= Mathf.Deg2Rad; hi *= Mathf.Deg2Rad; }
            bool limited = body.twistLock == ArticulationDofLock.LimitedMotion
                || body.jointType == ArticulationJointType.PrismaticJoint
                || lo != 0f || lo != 0f;

            if (limited && drive.upperLimit > drive.lowerLimit)
            {
                los.Add(lo);
                his.Add(hi);
            }
            else
            {
                los.Add(fallbackMin);
                his.Add(fallbackMax);
            }
        }

        jointNames = names.ToArray();
        lowerLimits = los.ToArray();
        upperLimits = his.ToArray();
    }

    /// <summary>Generate + emit a new random pose.</summary>
    [ContextMenu("Trigger Random Pose")]
    public void Trigger()
    {
        int count = jointNames.Length;
        var msg = new JointStateMessage
        {
            name = jointNames,
            position = new float[count],
            velocity = new float[count],
            effort = new float[count]
        };

        for (int i = 0; i < count; i++)
            msg.position[i] = UnityEngine.Random.Range(lowerLimits[i], upperLimits[i]);

        latestMessage = msg;
        OnJointStateReceived?.Invoke(msg);
    }

    public JointStateMessage GetLatestJointState() => latestMessage;
}