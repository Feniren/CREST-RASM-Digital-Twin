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

            // Prismatic uses xDrive too; revolute/spherical map similarly for single-DOF URDF joints.
            var drive = body.xDrive;
            bool limited = body.twistLock == ArticulationDofLock.LimitedMotion
                || body.jointType == ArticulationJointType.PrismaticJoint
                || drive.lowerLimit != 0f || drive.upperLimit != 0f;

            if (limited && drive.upperLimit > drive.lowerLimit)
            {
                los.Add(drive.lowerLimit);
                his.Add(drive.upperLimit);
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