using UnityEngine;

/// <summary>
/// A cutting tool: tip = transform.position, axis = transform.up (tip toward shank). Bound to
/// exactly one stock; Carve_Step samples the tip pose, so any motion source (keyboard jog,
/// Scene-view drag, machine axes) carves. Mill sweeps are tracked in stock-local space so a
/// stock that moves under the tool (a machine table) still carves the relative motion.
/// </summary>
public class Carving_Tool : MonoBehaviour
{
    [SerializeField] private Tool_Definition Definition;
    [Tooltip("Unity cylinder primitive (2 units tall, centred), fitted to the profile by Sync Visual.")]
    [SerializeField] private Transform Visual;
    [Header("Bound stock (assign exactly one)")]
    [SerializeField] private Mill_Stock Mill;
    [SerializeField] private Lathe_Stock Lathe;

    public Tool_Profile Profile => Definition != null ? Definition.Profile : null;
    public Vector3 Last_Tip { get; private set; }
    public bool Is_Lathe_Tool => Lathe != null;

    const float Still_Sqr = 1e-14f;   // a tip that moved less than 0.1 µm has not moved; nothing is stamped

    Vector3 last_local;

    void Awake()
    {
        if (Definition == null)
        {
            Debug.LogWarning("Carving_Tool: Definition is not assigned on " + name + ".");
            enabled = false;
            return;
        }
        if (Mill != null && Lathe != null)
        {
            Debug.LogWarning("Carving_Tool: " + name + " has both Mill and Lathe assigned; using Mill.");
            Lathe = null;
        }
        Sync_Visual();
    }

    void OnEnable()
    {
        Seed();
    }

    /// <summary>Binds to a mill stock (or null) and re-seeds, so the teleport that brought the tool here is not carved.</summary>
    public void Bind(Mill_Stock mill)
    {
        Mill = mill;
        Seed();
    }

    void Seed()
    {
        Last_Tip = transform.position;
        if (Mill != null) last_local = Mill.transform.InverseTransformPoint(Last_Tip);
    }

    public void Carve_Step()
    {
        if (Definition == null) return;
        Vector3 tip = transform.position;
        if (Mill != null)
        {
            Vector3 local = Mill.transform.InverseTransformPoint(tip);
            if ((local - last_local).sqrMagnitude > Still_Sqr)
            {
                Mill.Carve_Local(Profile, last_local, local, transform.up);
                last_local = local;
            }
        }
        if (Lathe != null && (tip - Last_Tip).sqrMagnitude > Still_Sqr) Lathe.Carve(Profile, Last_Tip, tip);
        Last_Tip = tip;
    }

    /// <summary>Fits the Visual cylinder to the profile: tip at the origin, shank along +Y.</summary>
    [ContextMenu("Sync Visual")]
    public void Sync_Visual()
    {
        if (Visual == null || Profile == null) return;
        float half = Profile.Length * 0.5f;
        Visual.localPosition = new Vector3(0f, half, 0f);
        Visual.localRotation = Quaternion.identity;
        Visual.localScale = new Vector3(Profile.Diameter, half, Profile.Diameter);
    }
}
