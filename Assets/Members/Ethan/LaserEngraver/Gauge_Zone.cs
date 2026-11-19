using UnityEngine;
using UnityEngine.Events;

// True while a LaserFocusGauge collider is inside this object's BoxCollider.
[RequireComponent(typeof(BoxCollider))]
public class Gauge_Zone : MonoBehaviour
{
    [SerializeField] private GameObject Visual;

    [SerializeField] private LayerMask Gauge_Layers = ~0;

    public UnityEvent Completed;
    public UnityEvent Lost;

    public bool Satisfied { get; private set; }

    private BoxCollider zone;
    private readonly Collider[] hits = new Collider[16];

    private void Awake()
    {
        zone = GetComponent<BoxCollider>();
        zone.isTrigger = true;
        Show_Visual(false);
    }

    private void FixedUpdate()
    {
        bool inside = Gauge_Inside();

        if (inside == Satisfied)
            return;

        Satisfied = inside;

        if (inside)
            Completed?.Invoke();
        else
            Lost?.Invoke();
    }

    public void Show_Visual(bool on)
    {
        if (Visual != null)
            Visual.SetActive(on);
    }

    private bool Gauge_Inside()
    {
        Transform t = zone.transform;
        Vector3 center = t.TransformPoint(zone.center);
        Vector3 halfExtents = Vector3.Scale(zone.size * 0.5f, Abs(t.lossyScale));

        int count = Physics.OverlapBoxNonAlloc(center, halfExtents, hits, t.rotation, Gauge_Layers, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            Collider c = hits[i];

            if (c == zone || !c.enabled)
                continue;

            if (c.GetComponentInParent<LaserFocusGauge>() != null)
                return true;
        }

        return false;
    }

    private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    private void OnDrawGizmos()
    {
        BoxCollider box = zone != null ? zone : GetComponent<BoxCollider>();

        if (box == null)
            return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Satisfied ? new Color(0f, 1f, 0.4f, 0.5f) : new Color(1f, 0.8f, 0f, 0.5f);
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
