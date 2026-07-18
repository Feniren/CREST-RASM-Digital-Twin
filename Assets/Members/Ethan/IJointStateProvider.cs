using UnityEngine;
using System;

public interface IJointStateProvider
{
    JointStateMessage GetLatestJointState();
    event Action<JointStateMessage> OnJointStateReceived;
}