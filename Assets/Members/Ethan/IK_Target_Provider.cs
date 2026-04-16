using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IK-based IJointStateProvider. Solves Jacobian transpose IK toward a target
/// position (base-frame), interpolates linearly in Cartesian space, and
/// publishes joint angles as JointStateMessage.
///
/// Uses a shadow copy of joint angles for FK rather than writing to
/// ArticulationBody.jointPosition, avoiding conflicts with the drive system.
/// </summary>
public class IKTargetProvider : MonoBehaviour, IJointStateProvider
{
	[Header("IK Settings")]
	[Tooltip("Child transform treated as end-effector (auto-detects last link if null)")]
	public Transform endEffector;

	[Tooltip("Position convergence threshold in meters")]
	public float positionTolerance = 0.001f;

	[Tooltip("Max IK iterations per solve step")]
	public int maxIterations = 20;

	[Tooltip("Jacobian transpose step size")]
	public float stepSize = 0.5f;

	[Tooltip("Clamp per-iteration joint delta (radians)")]
	public float maxJointDelta = 0.1f;

	[Header("Motion")]
	[Tooltip("Cartesian linear speed in m/s")]
	public float linearSpeed = 0.2f;

	[Tooltip("Publish rate in Hz")]
	public float publishRate = 50f;

	[Header("Target")]
	[Tooltip("Set in inspector to test — base-frame coordinates (meters)")]
	public Vector3 targetPosition;
	[Tooltip("Enable to start solving toward targetPosition")]
	public bool targetActive; // inspector-only trigger

	public event Action<JointStateMessage> OnJointStateReceived;

	// Chain data
	private ArticulationBody[] allBodies;
	private int[] movableIndices;
	private string[] jointNames;

	// Shadow FK state — never written back to ArticulationBody
	private float[] currentAngles;       // radians
	private Vector3[] jointAxesLocal;     // local rotation axis per movable joint

	private Transform baseTransform;
	private float publishTimer;
	private Vector3 interpolatedTarget;
	private bool interpolatedInitialized;

	void Start()
	{
		allBodies = GetComponentsInChildren<ArticulationBody>();
		if (allBodies.Length == 0) return;

		baseTransform = allBodies[0].transform;

		var indices = new List<int>();
		var names = new List<string>();

		for (int i = 0; i < allBodies.Length; i++)
		{
			if (allBodies[i].jointType != ArticulationJointType.FixedJoint)
			{
				indices.Add(i);
				names.Add(allBodies[i].name);
			}
		}

		movableIndices = indices.ToArray();
		jointNames = names.ToArray();
		currentAngles = new float[movableIndices.Length];

		// Cache local joint axes
		jointAxesLocal = new Vector3[movableIndices.Length];
		for (int i = 0; i < movableIndices.Length; i++)
		{
			var body = allBodies[movableIndices[i]];
			jointAxesLocal[i] = body.anchorRotation * Vector3.right;
			currentAngles[i] = body.jointPosition[0];
		}

		if (endEffector == null)
			endEffector = allBodies[allBodies.Length - 1].transform;
	}

	void Update()
	{
		if (movableIndices == null || movableIndices.Length == 0) return;

		// Inspector-driven target
		if (targetActive)
		{
			SetTarget(targetPosition);
			targetActive = false;
		}

		publishTimer += Time.deltaTime;
		float interval = 1f / publishRate;
		if (publishTimer < interval) return;
		publishTimer -= interval;

		if (!interpolatedInitialized) return;

		// Cartesian linear interpolation
		Vector3 toTarget = targetPosition - interpolatedTarget;
		float maxStep = linearSpeed * interval;

		if (toTarget.magnitude > maxStep)
			interpolatedTarget += toTarget.normalized * maxStep;
		else
			interpolatedTarget = targetPosition;

		SolveIK(interpolatedTarget);
		Publish();
	}

	public void SetTarget(Vector3 baseFramePosition)
	{
		targetPosition = baseFramePosition;

		if (!interpolatedInitialized)
		{
			interpolatedTarget = ComputeFK();
			interpolatedInitialized = true;
		}
	}

	public void SetTargetWorld(Vector3 worldPosition)
	{
		SetTarget(baseTransform.InverseTransformPoint(worldPosition));
	}

	public JointStateMessage GetLatestJointState()
	{
		if (currentAngles == null) return null;
		return BuildMessage();
	}

	#region IK Solver

	private void SolveIK(Vector3 targetLocal)
	{
		for (int iter = 0; iter < maxIterations; iter++)
		{
			Vector3 eeLocal = ComputeFK();
			Vector3 error = targetLocal - eeLocal;

			if (error.sqrMagnitude < positionTolerance * positionTolerance)
				break;

			for (int i = 0; i < movableIndices.Length; i++)
			{
				// Recompute FK up to this joint to get world-axis and position
				ComputeJointTransform(i, out Vector3 jointPosLocal, out Vector3 axisLocal);

				// J column: axis × (ee - jointPos)
				Vector3 jCol = Vector3.Cross(axisLocal, eeLocal - jointPosLocal);

				float dTheta = stepSize * Vector3.Dot(jCol, error);
				dTheta = Mathf.Clamp(dTheta, -maxJointDelta, maxJointDelta);

				currentAngles[i] += dTheta;
				ClampAngle(i);

				// Recompute ee after this joint changed (sequential update)
				eeLocal = ComputeFK();
				error = targetLocal - eeLocal;
			}
		}
	}

	#endregion

	#region Shadow FK

	/// <summary>
	/// Forward kinematics computed from shadow angles — no ArticulationBody mutation.
	/// Returns end-effector position in base-local coordinates.
	/// </summary>
	private Vector3 ComputeFK()
	{
		ComputeChainTransform(movableIndices.Length, out Vector3 pos, out _);
		return pos;
	}

	/// <summary>
	/// Compute position and rotation axis of movable joint at given solver index.
	/// </summary>
	private void ComputeJointTransform(int solverIndex, out Vector3 posLocal, out Vector3 axisLocal)
	{
		ComputeChainTransform(solverIndex, out posLocal, out Quaternion rot);
		axisLocal = rot * Vector3.right;
	}


	/// <summary>
	/// Shadow FK using ArticulationBody anchor transforms.
	/// Each joint step:
	///   1. parentAnchorPosition/Rotation — move to joint attachment point in parent frame
	///   2. R(θ) around Vector3.right — apply revolute motion in anchor frame
	///   3. Inverse(anchorRotation) — convert from anchor frame into child body frame
	///
	/// Full per-joint: T_child = T_parent * parentAnchorRotation * R(θ) * Inverse(anchorRotation)
	///
	/// </summary>
	private void ComputeChainTransform(int upToSolverIndex, out Vector3 pos, out Quaternion rot)
	{
		pos = Vector3.zero;
		rot = Quaternion.identity;

		int solverIdx = 0;
		bool chainStarted = false; // don't acumulate until first revolute

		for (int i = 1; i < allBodies.Length; i++)
		{
			var body = allBodies[i];

			if (!chainStarted)
			{
				if (body.jointType == ArticulationJointType.FixedJoint)
					continue; // assumes everything before first revolute already in baseTransform
				chainStarted = true;
			}

			pos += rot * body.parentAnchorPosition;
			rot *= body.parentAnchorRotation;

			if (body.jointType != ArticulationJointType.FixedJoint)
			{
				if (solverIdx >= upToSolverIndex)
					break;

				rot *= Quaternion.AngleAxis(currentAngles[solverIdx] * Mathf.Rad2Deg, Vector3.right);
				solverIdx++;
			}

			rot *= Quaternion.Inverse(body.anchorRotation);
		}
	}
	#endregion

	private void ClampAngle(int solverIndex)
	{
		var body = allBodies[movableIndices[solverIndex]];
		var drive = body.xDrive;

		float lower = drive.lowerLimit * Mathf.Deg2Rad;
		float upper = drive.upperLimit * Mathf.Deg2Rad;

		// Skip clamping if limits aren't set
		if (Mathf.Approximately(lower, upper)) return;

		currentAngles[solverIndex] = Mathf.Clamp(currentAngles[solverIndex], lower, upper);
	}

	private void Publish()
	{
		OnJointStateReceived?.Invoke(BuildMessage());
	}

	private JointStateMessage BuildMessage()
	{
		return new JointStateMessage
		{
			name = jointNames,
			position = (float[])currentAngles.Clone(),
			velocity = new float[currentAngles.Length],
			effort = new float[currentAngles.Length]
		};
	}

	void OnDrawGizmosSelected()
	{
		if (baseTransform == null) return;

		// Draw target sphere
		Gizmos.color = Color.green;
		Vector3 worldTarget = baseTransform.TransformPoint(targetPosition);
		Gizmos.DrawWireSphere(worldTarget, 0.02f);

		// Draw interpolated target
		if (interpolatedInitialized)
		{
			Gizmos.color = Color.yellow;
			Vector3 worldInterp = baseTransform.TransformPoint(interpolatedTarget);
			Gizmos.DrawWireSphere(worldInterp, 0.015f);
		}
	}
}