using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Standalone keyboard controller for ArticulationBody chains (e.g. URDF imports).
/// No dependency on Unity.Robotics.UrdfImporter.Control — drives articulations directly.
/// </summary>
public class StandaloneArticulationController : MonoBehaviour
{
	public enum DriveMode { Position, Velocity }

	[Header("Joint Selection")]
	[SerializeField] private Color highlightColor = new Color(1f, 0f, 0f, 1f);
	[SerializeField, Tooltip("Read-only in play mode")]
	private string selectedJointName;

	[Header("Drive Parameters")]
	public DriveMode driveMode = DriveMode.Position;
	public float stiffness = 10000f;
	public float damping = 100f;
	public float forceLimit = 1000f;
	public float speed = 5f;        // deg/s
	public float acceleration = 5f; // deg/s^2

	private ArticulationBody[] chain;
	private int selectedIndex;
	private int previousIndex;
	private Color[] storedColors;

	// Per-joint runtime state that replaces JointControl
	private float[] targetPositions;

	void Start()
	{
		chain = GetComponentsInChildren<ArticulationBody>();
		if (chain.Length == 0) return;

		targetPositions = new float[chain.Length];

		for (int i = 0; i < chain.Length; i++)
		{
			chain[i].jointFriction = 10f;
			chain[i].angularDamping = 10f;
			ApplyDriveSettings(chain[i]);

			// Initialize targets to current positions
			if (chain[i].jointType != ArticulationJointType.FixedJoint)
				targetPositions[i] = chain[i].jointPosition[0] * Mathf.Rad2Deg;
		}

		selectedIndex = 0;
		previousIndex = 0;
		StoreColors(selectedIndex);
		ApplyHighlight(selectedIndex);
		UpdateJointLabel();
	}

	void Update()
	{
		if (chain == null || chain.Length == 0) return;

		var kb = Keyboard.current;
		if (kb == null) return;

		// Joint selection
		if (kb.leftArrowKey.wasPressedThisFrame)
			SelectJoint(selectedIndex - 1);
		else if (kb.rightArrowKey.wasPressedThisFrame)
			SelectJoint(selectedIndex + 1);

		// Joint movement
		float input = 0f;
		if (kb.upArrowKey.isPressed) input += 1f;
		if (kb.downArrowKey.isPressed) input -= 1f;

		DriveJoint(selectedIndex, input);
	}

	private void SelectJoint(int newIndex)
	{
		newIndex = ((newIndex % chain.Length) + chain.Length) % chain.Length;
		if (newIndex == selectedIndex) return;

		RestoreColors(previousIndex);
		previousIndex = selectedIndex;
		RestoreColors(previousIndex);

		selectedIndex = newIndex;
		StoreColors(selectedIndex);
		ApplyHighlight(selectedIndex);
		UpdateJointLabel();
	}

	private void DriveJoint(int index, float direction)
	{
		if (index < 0 || index >= chain.Length) return;

		var body = chain[index];
		if (body.jointType == ArticulationJointType.FixedJoint) return;

		ArticulationDrive drive = body.xDrive;

		switch (driveMode)
		{
			case DriveMode.Position:
				targetPositions[index] += direction * speed * Time.deltaTime;
				drive.target = targetPositions[index];
				break;

			case DriveMode.Velocity:
				drive.targetVelocity = direction * speed;
				break;
		}

		body.xDrive = drive;
	}

	private void ApplyDriveSettings(ArticulationBody body)
	{
		ArticulationDrive drive = body.xDrive;
		drive.forceLimit = forceLimit;

		if (driveMode == DriveMode.Position)
		{
			drive.stiffness = stiffness;
			drive.damping = damping;
			drive.driveType = ArticulationDriveType.Target;
		}
		else
		{
			drive.stiffness = 0f;
			drive.damping = damping;
			drive.driveType = ArticulationDriveType.Velocity;
		}

		body.xDrive = drive;
	}

	#region Visual Highlight

	private void StoreColors(int index)
	{
		Renderer[] renderers = chain[index].GetComponentsInChildren<Renderer>();
		storedColors = new Color[renderers.Length];
		for (int i = 0; i < renderers.Length; i++)
			storedColors[i] = renderers[i].material.color;
	}

	private void RestoreColors(int index)
	{
		if (storedColors == null) return;
		Renderer[] renderers = chain[index].GetComponentsInChildren<Renderer>();
		for (int i = 0; i < renderers.Length && i < storedColors.Length; i++)
			renderers[i].material.color = storedColors[i];
	}

	private void ApplyHighlight(int index)
	{
		Renderer[] renderers = chain[index].GetComponentsInChildren<Renderer>();
		foreach (var r in renderers)
			r.material.color = highlightColor;
	}

	#endregion

	private void UpdateJointLabel()
	{
		selectedJointName = chain[selectedIndex].name + " (" + selectedIndex + ")";
	}

	void OnGUI()
	{
		GUIStyle style = GUI.skin.GetStyle("Label");
		style.alignment = TextAnchor.UpperCenter;
		float cx = Screen.width / 2f - 200f;
		GUI.Label(new Rect(cx, 10, 400, 20), "Left/Right: select joint | Up/Down: move joint", style);
		GUI.Label(new Rect(cx, 30, 400, 20), "Active: " + selectedJointName, style);
	}
}