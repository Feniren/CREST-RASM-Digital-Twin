using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Teleports ArticulationBody joints to target positions and holds them frozen.
/// Matches joints by name; unmatched joints are ignored.
/// </summary>
public class JointStateMsgTeleporter : MonoBehaviour
{
    private IJointStateProvider provider;
    private Dictionary<string, ArticulationBody> jointMap;
    private Dictionary<ArticulationBody, float> heldTargets;

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
        heldTargets = new Dictionary<ArticulationBody, float>();
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
        }
    }

    private void ApplyJointState(JointStateMessage msg)
    {
        if (msg.name == null) return;

        int count = msg.name.Length;
        bool hasPosition = msg.position != null && msg.position.Length >= count;
        if (!hasPosition) return;

        for (int i = 0; i < count; i++)
        {
            if (!jointMap.TryGetValue(msg.name[i], out var body))
                continue;

            heldTargets[body] = msg.position[i];
        }
    }

    // Runs every physics step. Re-pins position AND velocity so solver can't drift.
    void FixedUpdate()
    {
        if (heldTargets == null) return;

        foreach (var kv in heldTargets)
        {
            var body = kv.Key;
            float targetRad = kv.Value;

            var jp = body.jointPosition;
            jp[0] = targetRad;
            body.jointPosition = jp;

            var jv = body.jointVelocity;
            jv[0] = 0f;
            body.jointVelocity = jv;
        }
    }
}