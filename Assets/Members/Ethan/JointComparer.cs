using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class JointComparer : MonoBehaviour
{
    [System.Serializable]
    public class JointPair
    {
        public string label;
        public Transform jointA;
        public Transform jointB;

        [Header("Runtime (read-only)")]
        public float positionDelta;
        public float angleDelta;
        public Vector3 positionOffset;
        public Vector3 eulerOffset;
    }

    [Tooltip("Pairs of joints to compare each frame.")]
    public List<JointPair> pairs = new List<JointPair>();

    [Header("Total Distance Trigger")]
    [Tooltip("Fires when this threshold >= sum of all pair position deltas.")]
    public float totalDistanceThreshold = 0.1f;

    [Tooltip("Assignable reaction, fires once until reset.")]
    public UnityEvent onTotalDistanceTrigger;

    [Tooltip("Runtime: sum of all pair position deltas.")]
    public float totalDistance;

    [Tooltip("True after firing; blocks re-trigger until ResetTrigger().")]
    public bool triggerDisabled = false;

    [Header("Options")]
    [Tooltip("Compare in world space; otherwise local space.")]
    public bool useWorldSpace = true;

    [Tooltip("Log a warning when a threshold is exceeded.")]
    public bool warnOnThreshold = false;
    public float positionThreshold = 0.05f;
    public float angleThreshold = 5f;

    [Header("Gizmos")]
    public bool drawGizmos = true;
    public Color gizmoColor = Color.yellow;

    void Update()
    {
        totalDistance = 0f;
        for (int i = 0; i < pairs.Count; i++)
        {
            Evaluate(pairs[i]);
            totalDistance += pairs[i].positionDelta;
        }

        if (!triggerDisabled && totalDistanceThreshold >= totalDistance)
        {
            triggerDisabled = true;
            onTotalDistanceTrigger?.Invoke();
            Invoke(nameof(ResetTrigger), 2f);
        }
    }

    void Evaluate(JointPair p)
    {
        if (p.jointA == null || p.jointB == null) return;

        Vector3 posA, posB;
        Quaternion rotA, rotB;

        if (useWorldSpace)
        {
            posA = p.jointA.position;
            posB = p.jointB.position;
            rotA = p.jointA.rotation;
            rotB = p.jointB.rotation;
        }
        else
        {
            posA = p.jointA.localPosition;
            posB = p.jointB.localPosition;
            rotA = p.jointA.localRotation;
            rotB = p.jointB.localRotation;
        }

        p.positionOffset = posB - posA;
        p.positionDelta = p.positionOffset.magnitude;

        Quaternion diff = Quaternion.Inverse(rotA) * rotB;
        p.angleDelta = Quaternion.Angle(rotA, rotB);
        p.eulerOffset = NormalizeEuler(diff.eulerAngles);

        if (warnOnThreshold)
        {
            if (p.positionDelta > positionThreshold)
                Debug.LogWarning($"[JointComparer] '{p.label}' pos delta {p.positionDelta:F3} > {positionThreshold}", this);
            if (p.angleDelta > angleThreshold)
                Debug.LogWarning($"[JointComparer] '{p.label}' angle delta {p.angleDelta:F2} > {angleThreshold}", this);
        }
    }

    static Vector3 NormalizeEuler(Vector3 e)
    {
        e.x = Mathf.DeltaAngle(0f, e.x);
        e.y = Mathf.DeltaAngle(0f, e.y);
        e.z = Mathf.DeltaAngle(0f, e.z);
        return e;
    }

    /// <summary>Re-arms trigger. Call externally.</summary>
    public void ResetTrigger()
    {
        triggerDisabled = false;
    }

    // Query helpers
    public float GetPositionDelta(int index) =>
        (index >= 0 && index < pairs.Count) ? pairs[index].positionDelta : -1f;

    public float GetAngleDelta(int index) =>
        (index >= 0 && index < pairs.Count) ? pairs[index].angleDelta : -1f;

    public JointPair GetPair(string label) =>
        pairs.Find(p => p.label == label);

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        Gizmos.color = gizmoColor;
        foreach (var p in pairs)
        {
            if (p.jointA == null || p.jointB == null) continue;
            Gizmos.DrawLine(p.jointA.position, p.jointB.position);
            Gizmos.DrawWireSphere(p.jointA.position, 0.01f);
            Gizmos.DrawWireSphere(p.jointB.position, 0.01f);
        }
    }
}