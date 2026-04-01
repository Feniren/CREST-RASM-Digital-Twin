using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class URDFController : MonoBehaviour
{
    public GameObject provider_source;
    private Dictionary<string, ArticulationBody> _jointMap;
    private IJointStateProvider _provider;

    private void Awake()
    {
        _jointMap = GetComponentsInChildren<ArticulationBody>()
            .Where(ab => ab.jointType != ArticulationJointType.FixedJoint)
            .ToDictionary(ab => ab.name);

        Debug.Log($"[URDFController] Found {_jointMap.Count} joints:");
        foreach (var kvp in _jointMap)
            Debug.Log($"  \"{kvp.Key}\" stiffness={kvp.Value.xDrive.stiffness} damping={kvp.Value.xDrive.damping}");
    }

    public void Start()
    {
        if (provider_source == null)
        {
            Debug.LogError("[URDFController] provider_source is null");
            return;
        }

        var p = provider_source.GetComponent<IJointStateProvider>();
        if (p == null)
        {
            Debug.LogError("[URDFController] No IJointStateProvider on provider_source");
            return;
        }

        SetProvider(p);
        Debug.Log("[URDFController] Provider connected");
    }

    public void SetProvider(IJointStateProvider provider)
    {
        if (_provider != null)
            _provider.OnJointStateReceived -= ApplyJointState;

        _provider = provider;
        _provider.OnJointStateReceived += ApplyJointState;
    }

    private void ApplyJointState(JointStateMessage msg)
    {
        Debug.Log($"[URDFController] Received msg with {msg.name.Length} joints");

        for (int i = 0; i < msg.name.Length; i++)
        {
            if (_jointMap.TryGetValue(msg.name[i], out var joint))
            {
                var drive = joint.xDrive;
                drive.target = msg.position[i] * Mathf.Rad2Deg;
                joint.xDrive = drive;
                Debug.Log($"  Set \"{msg.name[i]}\" target={drive.target:F1}°");
            }
            else
            {
                Debug.LogWarning($"  No match for \"{msg.name[i]}\"");
            }
        }
    }
}