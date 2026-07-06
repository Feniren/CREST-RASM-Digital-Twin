using UnityEngine;
using UnityEngine.Events;

public class PickAndPlaceHandler : MonoBehaviour
{
    [Header("Spawn")]
    public GameObject blockPrefab;
    public Transform spawnPoint; // or use spawnPosition below
    public Vector3 spawnPosition;
    public bool useSpawnPoint = true;

    [Header("Reset")]
    public float resetHeight = -10f;

    [Header("Objective")]
    public Vector3 objectiveCenter;
    public Vector3 objectiveSize = new Vector3(2, 2, 2);
    public UnityEvent onObjectiveReached;
    public bool triggerOnce = true;

    GameObject block;
    bool triggered;

    void Start()
    {
        SpawnBlock();
    }

    void SpawnBlock()
    {
        Vector3 pos = useSpawnPoint && spawnPoint ? spawnPoint.position : spawnPosition;
        if (block == null)
            block = Instantiate(blockPrefab, pos, Quaternion.identity);
        else
        {
            // reset instead of re-instantiate
            block.transform.SetPositionAndRotation(pos, Quaternion.identity);
            if (block.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = Vector3.zero; // .velocity on Unity < 6
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    void Update()
    {
        if (block == null) return;

        // reset check
        if (block.transform.position.y < resetHeight)
            SpawnBlock();

        // objective check (AABB, no trigger colliders needed)
        if (!triggered || !triggerOnce)
        {
            Bounds b = new Bounds(objectiveCenter, objectiveSize);
            if (b.Contains(block.transform.position))
            {
                triggered = true;
                onObjectiveReached.Invoke();
            }
        }
    }

    void OnDrawGizmos()
    {
        // objective zone (green)
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(objectiveCenter, objectiveSize);
        // reset plane (red)
        Gizmos.color = Color.red;
        Vector3 c = useSpawnPoint && spawnPoint ? spawnPoint.position : spawnPosition;
        Gizmos.DrawWireCube(new Vector3(c.x, resetHeight, c.z), new Vector3(20, 0.01f, 20));
        // spawn point (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(c, 0.5f);
    }
}