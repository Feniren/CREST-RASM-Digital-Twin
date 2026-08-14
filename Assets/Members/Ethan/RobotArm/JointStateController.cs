using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives ArticulationBody chains from an IJointStateProvider.
/// Matches joints by name; unmatched joints are ignored.
/// </summary>
public class JointStateController : MonoBehaviour
{
    [Header("Drive Parameters")]
    public float stiffness = 10000f;
    public float damping = 100f;
    public float forceLimit = 1000f;

    private IJointStateProvider provider;
    private Dictionary<string, ArticulationBody> jointMap;

    void Start()
    {
        provider = GetComponent<IJointStateProvider>();
        if (provider == null)
        {
            Debug.LogError($"[JointStateController] No IJointStateProvider on {name}");
            enabled = false;
            return;
        }

        BuildJointMap();
        provider.OnJointStateReceived += ApplyJointState;

        Debug.Log($"[JointStateController] Subscribed to {provider.GetType().Name}");
    }

    void OnDestroy()
    {
        if (provider != null) provider.OnJointStateReceived -= ApplyJointState;
    }

    private void BuildJointMap()
    {
        jointMap = new Dictionary<string, ArticulationBody>();

        foreach (var body in GetComponentsInChildren<ArticulationBody>())
        {
            if (body.jointType == ArticulationJointType.FixedJoint) continue;

            if (jointMap.ContainsKey(body.name))
            {
                Debug.LogWarning($"[JointStateController] Duplicate joint name: {body.name}");
                continue;
            }

            jointMap[body.name] = body;

            ApplyDriveSettings(body);

            Debug.Log($"[JointStateController] Registered joint '{body.name}'");
        }
    }

    private void ApplyJointState(JointStateMessage msg)
    {
        if (msg.name == null) return;

        int count = msg.name.Length;

        bool hasPosition = msg.position != null && msg.position.Length >= count;

        bool hasVelocity = msg.velocity != null && msg.velocity.Length >= count;

        for (int i = 0; i < count; i++)
        {
            string jointName = msg.name[i];

            if (!jointMap.TryGetValue(jointName, out var body))
            {
                Debug.LogWarning($"[JointStateController] ROS joint '{jointName}' not found in Unity.");
                continue;
            }

            var drive = body.xDrive;

            if (hasPosition)
            {
                float targetDegrees = msg.position[i] * Mathf.Rad2Deg;

                Debug.Log($"[JointStateController] {jointName}: ROS={msg.position[i]:F3} rad, target={targetDegrees:F1} deg");

                drive.target = targetDegrees;
            }

            if (hasVelocity)
                drive.targetVelocity = msg.velocity[i] * Mathf.Rad2Deg;

            body.xDrive = drive;
        }
    }

    private void ApplyDriveSettings(ArticulationBody body)
    {
        var drive = body.xDrive;
        drive.stiffness = stiffness;
        drive.damping = damping;
        drive.forceLimit = forceLimit;
        drive.driveType = ArticulationDriveType.Target;
        body.xDrive = drive;
    }
}
