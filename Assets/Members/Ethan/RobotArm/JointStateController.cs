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
    }

    void OnDestroy()
    {
        if (provider != null)
            provider.OnJointStateReceived -= ApplyJointState;
    }

    private void BuildJointMap()
    {
        jointMap = new Dictionary<string, ArticulationBody>();
        foreach (var body in GetComponentsInChildren<ArticulationBody>())
        {
            if (body.jointType == ArticulationJointType.FixedJoint)
                continue;

            if (jointMap.ContainsKey(body.name))
            {
                Debug.LogWarning($"[JointStateController] Duplicate joint name: {body.name}");
                continue;
            }

            jointMap[body.name] = body;
            ApplyDriveSettings(body);
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
            if (!jointMap.TryGetValue(msg.name[i], out var body))
                continue;

            var drive = body.xDrive;

            if (hasPosition)
                drive.target = msg.position[i] * Mathf.Rad2Deg;

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