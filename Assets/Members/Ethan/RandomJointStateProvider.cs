using System;
using UnityEngine;

/// <summary>
/// Mock provider emitting smooth, physically consistent joint states.
/// Each joint follows a sinusoidal trajectory with randomized parameters,
/// producing continuous position/velocity pairs that mimic real servo motion.
/// </summary>
public class RandomJointStateProvider : MonoBehaviour, IJointStateProvider
{
	[Tooltip("Emit interval in seconds")]
	public float publishRate = 0.1f;

	[Tooltip("Position amplitude bounds in radians")]
	public float minAmplitude = 0.3f;
	public float maxAmplitude = 1.57f;

	[Tooltip("Oscillation frequency bounds in Hz")]
	public float minFrequency = 0.1f;
	public float maxFrequency = 0.5f;

	public event Action<JointStateMessage> OnJointStateReceived;

	private string[] jointNames;
	private float[] amplitudes;
	private float[] frequencies;
	private float[] phases;
	private float timer;
	private JointStateMessage latestMessage;

	void Start()
	{
		var bodies = GetComponentsInChildren<ArticulationBody>();
		var names = new System.Collections.Generic.List<string>();

		foreach (var body in bodies)
		{
			if (body.jointType != ArticulationJointType.FixedJoint)
				names.Add(body.name);
		}

		jointNames = names.ToArray();
		int count = jointNames.Length;

		amplitudes = new float[count];
		frequencies = new float[count];
		phases = new float[count];

		for (int i = 0; i < count; i++)
		{
			amplitudes[i] = UnityEngine.Random.Range(minAmplitude, maxAmplitude);
			frequencies[i] = UnityEngine.Random.Range(minFrequency, maxFrequency);
			phases[i] = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
		}
	}

	void Update()
	{
		timer += Time.deltaTime;
		if (timer < publishRate) return;
		timer -= publishRate;

		float t = Time.time;
		int count = jointNames.Length;

		var msg = new JointStateMessage
		{
			name = jointNames,
			position = new float[count],
			velocity = new float[count],
			effort = new float[count]
		};

		for (int i = 0; i < count; i++)
		{
			float w = 2f * Mathf.PI * frequencies[i];
			float theta = w * t + phases[i];

			// position = A * sin(wt + phi)
			msg.position[i] = amplitudes[i] * Mathf.Sin(theta);

			// velocity = d/dt position = A * w * cos(wt + phi)
			msg.velocity[i] = amplitudes[i] * w * Mathf.Cos(theta);
		}

		latestMessage = msg;
		OnJointStateReceived?.Invoke(msg);
	}

	public JointStateMessage GetLatestJointState() => latestMessage;
}