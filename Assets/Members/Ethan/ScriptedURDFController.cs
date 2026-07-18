using UnityEngine;

/// <summary>
/// Scripted sequence controller for ArticulationBody chains (e.g. URDF imports).
/// Each instruction moves one joint: negative for `duration`, positive for 2x `duration`,
/// then negative for `duration` again to return to rest. Then advances to next joint.
/// </summary>
public class ScriptedURDFController : MonoBehaviour
{
    [Header("Sequence")]
    [SerializeField, Tooltip("Seconds per instruction phase. Index i drives the i-th movable joint.")]
    private float[] instructions;

    [Header("Highlight")]
    [SerializeField] private Color highlightColor = new Color(1f, 0f, 0f, 1f);
    [SerializeField, Tooltip("Read-only in play mode")]
    private string activeJointName;

    [Header("Drive Parameters")]
    public float stiffness = 10000f;
    public float damping = 100f;
    public float forceLimit = 1000f;
    public float speed = 5f; // deg/s

    private enum Phase { Negative, Positive, Return, Done }

    private ArticulationBody[] joints; // movable joints only
    private float[] targetPositions;

    private int instructionIndex;
    private Phase phase;
    private float phaseTimer;

    private Color[] storedColors;
    private int highlightedJoint = -1;

    void Start()
    {
        // Collect movable (non-fixed) joints in chain order
        var all = GetComponentsInChildren<ArticulationBody>();
        var list = new System.Collections.Generic.List<ArticulationBody>();
        foreach (var b in all)
            if (b.jointType != ArticulationJointType.FixedJoint)
                list.Add(b);
        joints = list.ToArray();

        if (joints.Length == 0 || instructions == null || instructions.Length == 0)
        {
            phase = Phase.Done;
            return;
        }

        targetPositions = new float[joints.Length];
        for (int i = 0; i < joints.Length; i++)
        {
            joints[i].jointFriction = 10f;
            joints[i].angularDamping = 10f;
            ApplyDriveSettings(joints[i]);
            targetPositions[i] = joints[i].jointPosition[0] * Mathf.Rad2Deg;
        }

        BeginInstruction(0);
    }

    void Update()
    {
        if (phase == Phase.Done) return;

        int jointIndex = instructionIndex % joints.Length;
        float duration = Mathf.Max(0f, instructions[instructionIndex]);

        float direction = phase == Phase.Positive ? 1f : -1f;
        float phaseLength = phase == Phase.Positive ? duration * 2f : duration;

        DriveJoint(jointIndex, direction);

        phaseTimer += Time.deltaTime;
        if (phaseTimer < phaseLength) return;

        phaseTimer = 0f;
        switch (phase)
        {
            case Phase.Negative:
                phase = Phase.Positive;
                break;
            case Phase.Positive:
                phase = Phase.Return;
                break;
            case Phase.Return:
                if (instructionIndex + 1 < instructions.Length)
                    BeginInstruction(instructionIndex + 1);
                else
                    Finish();
                break;
        }
    }

    private void BeginInstruction(int index)
    {
        instructionIndex = index;
        phase = Phase.Negative;
        phaseTimer = 0f;

        int jointIndex = instructionIndex % joints.Length;
        SetHighlight(jointIndex);
        activeJointName = joints[jointIndex].name + " (" + jointIndex + ")";
    }

    private void Finish()
    {
        ClearHighlight();
        phase = Phase.Done;
        activeJointName = "(done)";
    }

    private void DriveJoint(int index, float direction)
    {
        var body = joints[index];
        targetPositions[index] += direction * speed * Time.deltaTime;

        ArticulationDrive drive = body.xDrive;
        drive.target = targetPositions[index];
        body.xDrive = drive;
    }

    private void ApplyDriveSettings(ArticulationBody body)
    {
        ArticulationDrive drive = body.xDrive;
        drive.forceLimit = forceLimit;
        drive.stiffness = stiffness;
        drive.damping = damping;
        drive.driveType = ArticulationDriveType.Target;
        body.xDrive = drive;
    }

    #region Visual Highlight

    private Renderer[] GetLinkRenderers(int jointIndex)
    {
        var result = new System.Collections.Generic.List<Renderer>();
        CollectRenderers(joints[jointIndex].transform, result, isRoot: true);
        return result.ToArray();
    }

    private void CollectRenderers(Transform t, System.Collections.Generic.List<Renderer> result, bool isRoot)
    {
        if (!isRoot && t.GetComponent<ArticulationBody>() != null) return; // next link — stop

        result.AddRange(t.GetComponents<Renderer>());
        foreach (Transform child in t)
            CollectRenderers(child, result, isRoot: false);
    }

    // Only this joint's own renderers — GetComponents, not GetComponentsInChildren.
    private void SetHighlight(int jointIndex)
    {
        ClearHighlight();

        Renderer[] renderers = GetLinkRenderers(jointIndex);
        storedColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            storedColors[i] = renderers[i].material.color;
            renderers[i].material.color = highlightColor;
        }
        highlightedJoint = jointIndex;
    }

    private void ClearHighlight()
    {
        if (highlightedJoint < 0 || storedColors == null) return;

        Renderer[] renderers = GetLinkRenderers(highlightedJoint);
        for (int i = 0; i < renderers.Length && i < storedColors.Length; i++)
            renderers[i].material.color = storedColors[i];

        highlightedJoint = -1;
        storedColors = null;
    }

    #endregion

    void OnGUI()
    {
        GUIStyle style = GUI.skin.GetStyle("Label");
        style.alignment = TextAnchor.UpperCenter;
        float cx = Screen.width / 2f - 200f;
        GUI.Label(new Rect(cx, 10, 400, 20), "Instruction " + (instructionIndex + 1) + "/" + (instructions != null ? instructions.Length : 0) + " | Phase: " + phase, style);
        GUI.Label(new Rect(cx, 30, 400, 20), "Active: " + activeJointName, style);
    }
}