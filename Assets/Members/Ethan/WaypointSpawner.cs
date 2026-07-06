using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Spawns a waypoint marker prefab at a random polar-coordinate offset
/// from this object, then watches for the target to arrive within range.
/// </summary>
public class WaypointSpawner : MonoBehaviour
{
    [Header("Spawning")]
    [Tooltip("Prefab spawned as the waypoint marker.")]
    [SerializeField] private GameObject waypointPrefab;

    [Tooltip("Min distance from this object the waypoint can spawn.")]
    [SerializeField] private float minRadius = 2f;

    [Tooltip("Max distance from this object the waypoint can spawn.")]
    [SerializeField] private float maxRadius = 10f;

    [Tooltip("Azimuth range (degrees) around this object's forward direction. 360 = full circle.")]
    [Range(0f, 360f)]
    [SerializeField] private float azimuthRange = 360f;

    [Tooltip("Min elevation angle (degrees). -90 = straight down.")]
    [Range(-90f, 90f)]
    [SerializeField] private float minElevation = 0f;

    [Tooltip("Max elevation angle (degrees). 90 = straight up.")]
    [Range(-90f, 90f)]
    [SerializeField] private float maxElevation = 45f;

    [Tooltip("Spawn waypoint automatically on Start.")]
    [SerializeField] private bool spawnOnStart = true;

    [Header("Detection")]
    [Tooltip("Object being tracked (player, NPC, etc).")]
    [SerializeField] private Transform target;

    [Tooltip("Target counts as 'arrived' within this distance of waypoint.")]
    [SerializeField] private float arrivalRadius = 1.5f;

    [Tooltip("Destroy waypoint marker when target arrives.")]
    [SerializeField] private bool destroyOnArrival = true;

    [Header("Events")]
    public UnityEvent onTargetArrived;

    public Transform CurrentWaypoint { get; private set; }
    public bool TargetHasArrived { get; private set; }

    private void Start()
    {
        if (spawnOnStart)
            SpawnWaypoint();
    }

    private void Update()
    {
        if (TargetHasArrived || CurrentWaypoint == null || target == null)
            return;

        if (GetDistanceToWaypoint() <= arrivalRadius)
            HandleArrival();
    }

    /// <summary>
    /// Spawns (or respawns) the waypoint at a random polar offset from this object.
    /// </summary>
    public void SpawnWaypoint()
    {
        if (waypointPrefab == null)
        {
            Debug.LogWarning($"{name}: No waypoint prefab assigned.", this);
            return;
        }

        ClearWaypoint();

        // Spherical coordinates: random azimuth, elevation, radius.
        float halfAzimuth = azimuthRange * 0.5f;
        float azimuth = 180f + Random.Range(-halfAzimuth, halfAzimuth);

        // Sample sin(elevation) uniformly for even distribution over sphere surface
        // (uniform elevation angle would cluster points at poles).
        float sinMin = Mathf.Sin(minElevation * Mathf.Deg2Rad);
        float sinMax = Mathf.Sin(maxElevation * Mathf.Deg2Rad);
        float elevation = Mathf.Asin(Random.Range(sinMin, sinMax)) * Mathf.Rad2Deg;

        float radius = Random.Range(minRadius, maxRadius);

        // Yaw around Y (azimuth), then pitch (elevation), applied to forward.
        Vector3 direction = Quaternion.Euler(-elevation, azimuth, 0f) * transform.forward;
        Vector3 spawnPosition = transform.position + direction * radius;

        CurrentWaypoint = Instantiate(waypointPrefab, spawnPosition, Quaternion.identity).transform;
        TargetHasArrived = false;
        Debug.Log("SpawnwaypEnd");
    }

    /// <summary>Removes current waypoint marker if one exists.</summary>
    public void ClearWaypoint()
    {
        if (CurrentWaypoint != null)
            Destroy(CurrentWaypoint.gameObject);

        CurrentWaypoint = null;
        TargetHasArrived = false;
    }

    private float GetDistanceToWaypoint()
    {
        Vector3 a = target.position;
        Vector3 b = CurrentWaypoint.position;

        return Vector3.Distance(a, b);
    }

    private void HandleArrival()
    {
        TargetHasArrived = true;

        if (destroyOnArrival)
            ClearWaypoint();

        onTargetArrived?.Invoke();
    }

    // Editor visualization of spawn bounds + arrival radius.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        DrawArc(minRadius);
        DrawArc(maxRadius);

        if (CurrentWaypoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(CurrentWaypoint.position, arrivalRadius);
        }
    }

    private void DrawArc(float radius)
    {
        const int segments = 48;
        float halfRange = azimuthRange * 0.5f;
        Vector3 prev = transform.position + Quaternion.Euler(0f, 180f - halfRange, 0f) * transform.forward * radius;

        for (int i = 1; i <= segments; i++)
        {
            float t = Mathf.Lerp(-halfRange, halfRange, i / (float)segments);
            Vector3 next = transform.position + Quaternion.Euler(0f, 180f + t, 0f) * transform.forward * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}