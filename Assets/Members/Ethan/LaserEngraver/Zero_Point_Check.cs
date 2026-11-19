using UnityEngine;
using UnityEngine.Events;

// True while the laser head tip is touching the top of the focus gauge,
// measured vertically in Frame's local space.
public class Zero_Point_Check : MonoBehaviour
{
    [SerializeField] private Transform Frame;

    [SerializeField] private Transform Head_Tip;

    [Tooltip("Optional. Falls back to the top of Gauge_Collider's bounds.")]
    [SerializeField] private Transform Gauge_Top;

    [SerializeField] private Collider Gauge_Collider;

    [Tooltip("Head tip above gauge top, in Frame units.")]
    [SerializeField] private float Max_Gap = 0.003f;

    [Tooltip("Gauge pushed past the head tip (tilting), in Frame units.")]
    [SerializeField] private float Max_Overlap = 0.01f;

    [SerializeField] private GameObject Visual;

    public UnityEvent Completed;
    public UnityEvent Lost;

    public bool Satisfied { get; private set; }

    public float Current_Gap { get; private set; } = float.NaN;

    private void Awake()
    {
        Show_Visual(false);
    }

    private void Update()
    {
        Current_Gap = Measure_Gap();

        bool ok = !float.IsNaN(Current_Gap) && Current_Gap <= Max_Gap && Current_Gap >= -Max_Overlap;

        if (ok == Satisfied)
            return;

        Satisfied = ok;

        if (ok)
            Completed?.Invoke();
        else
            Lost?.Invoke();
    }

    public void Show_Visual(bool on)
    {
        if (Visual != null)
            Visual.SetActive(on);
    }

    private float Measure_Gap()
    {
        if (Head_Tip == null)
            return float.NaN;

        Vector3 gaugeTop;

        if (Gauge_Top != null)
            gaugeTop = Gauge_Top.position;
        else if (Gauge_Collider != null && Gauge_Collider.enabled && Gauge_Collider.gameObject.activeInHierarchy)
        {
            Bounds b = Gauge_Collider.bounds;
            gaugeTop = new Vector3(b.center.x, b.max.y, b.center.z);
        }
        else
            return float.NaN;

        Transform frame = Frame != null ? Frame : transform;
        return frame.InverseTransformPoint(Head_Tip.position).y - frame.InverseTransformPoint(gaugeTop).y;
    }
}
