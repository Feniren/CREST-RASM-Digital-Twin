using UnityEngine;

/// <summary>
/// Provides a precise, non-convex collider for engraving raycasts.
/// Needed because non-convex collider required for UV spot detection,
/// and because non-convex colliders cannot be on non-kinematic rigidbodies.
///
/// This object must remain separate from the owner's dynamic Rigidbody.
/// Its layer should be excluded from ordinary physical collisions.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshCollider))]
public sealed class EngraveDetector : MonoBehaviour
{
    [Header("Proxying")]
    [SerializeField] private EngravableSurface owner;
    [SerializeField] private Transform followedTransform;
    [SerializeField] private Rigidbody proxyBody;
    [SerializeField] private MeshCollider proxyCollider;

    [Header("Synchronization")]
    [Tooltip("Enable to synchronize again in LateUpdate for visual and raycast accuracy.")]
    [SerializeField] private bool synchronizeInLateUpdate = true;

    // debug visuals
    private bool showWireframe = true;
    private bool showOnlyWhenSelected;
    private Color wireframeColor = Color.cyan;

    public EngravableSurface Owner => owner;
    public MeshCollider QueryCollider => proxyCollider;

    private void Reset()
    {
        proxyBody = GetComponent<Rigidbody>();
        proxyCollider = GetComponent<MeshCollider>();

        ConfigurePhysicsComponents();
    }

    private void Awake()
    {
        if (proxyBody == null)
            proxyBody = GetComponent<Rigidbody>();

        if (proxyCollider == null)
            proxyCollider = GetComponent<MeshCollider>();

        ConfigurePhysicsComponents();

        Debug.Log($"[EngraveDetector] Awake on '{name}'. owner={(owner != null ? owner.name : "null")}, " +
            $"followedTransform={(followedTransform != null ? followedTransform.name : "null")}, " +
            $"sharedMesh={(proxyCollider != null && proxyCollider.sharedMesh != null ? proxyCollider.sharedMesh.name : "null")}.", this);
    }

    public void Configure(
        EngravableSurface owner,
        Transform followedTransform,
        Mesh queryMesh)
    {
        this.owner = owner;
        this.followedTransform = followedTransform;

        if (proxyBody == null)
            proxyBody = GetComponent<Rigidbody>();

        if (proxyCollider == null)
            proxyCollider = GetComponent<MeshCollider>();

        ConfigurePhysicsComponents();

        proxyCollider.sharedMesh = null;
        proxyCollider.sharedMesh = queryMesh;

        SyncImmediately();

        Debug.Log($"[EngraveDetector] Configured on '{name}'. owner='{(owner != null ? owner.name : "null")}', " +
            $"followedTransform='{(followedTransform != null ? followedTransform.name : "null")}', " +
            $"queryMesh='{(queryMesh != null ? queryMesh.name : "null")}' (vertexCount={(queryMesh != null ? queryMesh.vertexCount : 0)}), " +
            $"proxyCollider.convex={(proxyCollider != null ? proxyCollider.convex.ToString() : "n/a")}, " +
            $"layer={LayerMask.LayerToName(gameObject.layer)}.", this);
    }

    private void ConfigurePhysicsComponents()
    {
        if (proxyBody != null)
        {
            proxyBody.isKinematic = true;
            proxyBody.useGravity = false;
            proxyBody.detectCollisions = true;
        }

        if (proxyCollider != null)
        {
            proxyCollider.convex = false;
        }
    }

    private void FixedUpdate()
    {
        if (followedTransform == null || proxyBody == null)
        {
            Debug.LogWarning($"[EngraveDetector] '{name}' FixedUpdate skipped sync: " +
                $"followedTransform={(followedTransform != null ? "set" : "null")}, " +
                $"proxyBody={(proxyBody != null ? "set" : "null")}.", this);
            return;
        }

        proxyBody.MovePosition(followedTransform.position);
        proxyBody.MoveRotation(followedTransform.rotation);
        transform.localScale = followedTransform.lossyScale;
    }

    private void LateUpdate()
    {
        if (!synchronizeInLateUpdate)
            return;

        SyncImmediately();
    }

    /// <summary>
    /// Call immediately before an engraving raycast when exact same-frame
    /// alignment is important.
    /// </summary>
    public void SyncImmediately()
    {
        if (followedTransform == null)
            return;
        transform.SetPositionAndRotation(followedTransform.position, followedTransform.rotation);
        transform.localScale = followedTransform.lossyScale;
    }

    public bool TryGetOwner(out EngravableSurface engravable)
    {
        engravable = owner;
        return engravable != null;
    }

    private void OnDrawGizmos()
    {
        if (!showOnlyWhenSelected) DrawProxyWireframe();
    }

    private void OnDrawGizmosSelected()
    {
        if (showOnlyWhenSelected) DrawProxyWireframe();
    }

    private void DrawProxyWireframe()
    {
        if (!showWireframe)
            return;

        if (proxyCollider == null)
            proxyCollider = GetComponent<MeshCollider>();

        Mesh mesh = null;
        if (proxyCollider != null) mesh = proxyCollider.sharedMesh;

        if (mesh == null) return;

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color previousColor = Gizmos.color;

        Gizmos.color = wireframeColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireMesh(mesh);

        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }
}
