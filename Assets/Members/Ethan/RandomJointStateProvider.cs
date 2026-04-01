using System;
using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// Drives random joint targets into a URDF robot's ArticulationBody chain.
/// Attach to the same GameObject that has the URDF Controller component.
/// </summary>
public class RandomJointStateProvider : MonoBehaviour, IJointStateProvider
{
    public GameObject machine;

    [SerializeField] private float publishRateHz = 10f;
    [SerializeField] private float minPosition = -Mathf.PI;
    [SerializeField] private float maxPosition = Mathf.PI;

    public event Action<JointStateMessage> OnJointStateReceived;

    private ArticulationBody[] _joints;
    private JointStateMessage _latest;

    public JointStateMessage GetLatestJointState() => _latest;

    private void Start()
    {
        Debug.Log("starts");
        // Filter to only movable joints (skip the fixed root body)
        _joints = machine.GetComponentsInChildren<ArticulationBody>()
            .Where(ab => ab.jointType != ArticulationJointType.FixedJoint)
            .ToArray();
        Debug.Log(_joints);
        StartCoroutine(PublishLoop());
    }

    private IEnumerator PublishLoop()
    {
        var interval = new WaitForSeconds(1f / publishRateHz);

        while (true)
        {
            _latest = BuildMessage();
            OnJointStateReceived?.Invoke(_latest);
            yield return interval;
        }
    }

    private JointStateMessage BuildMessage()
    {
        int count = _joints.Length;
        var msg = new JointStateMessage
        {
            name = new string[count],
            position = new float[count],
            velocity = new float[count],
            effort = new float[count]
        };

        for (int i = 0; i < count; i++)
        {
            msg.name[i] = _joints[i].name;
            msg.position[i] = UnityEngine.Random.Range(minPosition, maxPosition);
            msg.velocity[i] = UnityEngine.Random.Range(-1f, 1f);
            msg.effort[i] = 0f;
        }

        return msg;
    }
}