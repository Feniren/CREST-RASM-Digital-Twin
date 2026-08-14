using System;
using RosMessageTypes.Sensor;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

public class ROSJointStateProvider : MonoBehaviour, IJointStateProvider
{
    public string topicName = "/joint_states";

    public event Action<JointStateMessage> OnJointStateReceived;

    private ROSConnection ros;
    private JointStateMessage latestMessage;

    void Start()
    {
        Debug.Log($"[ROSJointStateProvider] Starting on {name}");

        ros = ROSConnection.GetOrCreateInstance();

        Debug.Log($"[ROSJointStateProvider] Subscribing to '{topicName}'");

        ros.Subscribe<JointStateMsg>(topicName, OnRosJointStateReceived);
    }

    private void OnRosJointStateReceived(JointStateMsg rosMsg)
    {
        Debug.Log($"[ROSJointStateProvider] RECEIVED ROS MESSAGE: {rosMsg.name?.Length ?? 0} joints"
        );

        if (rosMsg.name != null)
        {
            for (int i = 0; i < rosMsg.name.Length; i++)
            {
                double pos = rosMsg.position != null && i < rosMsg.position.Length ? rosMsg.position[i] : 0.0;

                Debug.Log($"[ROSJointStateProvider] {rosMsg.name[i]} = {pos:F3}");
            }
        }

        var msg = new JointStateMessage
        {
            name = rosMsg.name ?? Array.Empty<string>(),
            position = ConvertToFloatArray(rosMsg.position),
            velocity = ConvertToFloatArray(rosMsg.velocity),
            effort = ConvertToFloatArray(rosMsg.effort)
        };

        latestMessage = msg;

        Debug.Log("[ROSJointStateProvider] Invoking OnJointStateReceived");

        OnJointStateReceived?.Invoke(msg);
    }

    public JointStateMessage GetLatestJointState()
    {
        return latestMessage;
    }

    private static float[] ConvertToFloatArray(double[] values)
    {
        if (values == null) return Array.Empty<float>();

        var result = new float[values.Length];

        for (int i = 0; i < values.Length; i++)
            result[i] = (float)values[i];

        return result;
    }

    void OnDestroy()
    {
        if (ros != null) ros.Unsubscribe(topicName);
    }
}
