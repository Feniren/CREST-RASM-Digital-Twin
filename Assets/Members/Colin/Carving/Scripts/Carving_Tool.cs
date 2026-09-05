using UnityEngine;

/// <summary>
/// Dummy tool in the sandbox. Tip = transform.position, axis = transform.up (tip toward shank).
/// Bound to exactly one stock; carving samples the tip pose every step so any motion source
/// (keyboard jog, Scene-view drag) carves.
/// </summary>
public class Carving_Tool : MonoBehaviour
{
    public Tool_Profile Profile = new Tool_Profile();
    public Mill_Stock Mill;
    public Lathe_Stock Lathe;

    public Vector3 Last_Tip { get; private set; }
    public bool Is_Lathe_Tool => Lathe != null;

    // Mill sweeps are tracked in stock-local space so a stock that moves under the tool (a
    // machine table) still carves the relative motion; a world-space sweep would only see the tool's own delta.
    Vector3 last_local;

    void OnEnable()
    {
        Seed();
    }

    /// <summary>Binds to a stock (or null) and re-seeds, so the teleport that brought the tool here is not carved.</summary>
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
        Vector3 tip = transform.position;
        if (Mill != null)
        {
            Vector3 local = Mill.transform.InverseTransformPoint(tip);
            Mill.Carve_Local(Profile, last_local, local, transform.up);
            last_local = local;
        }
        if (Lathe != null) Lathe.Carve(Profile, Last_Tip, tip);
        Last_Tip = tip;
    }

    /// <summary>Fits the child named "Visual" (a Unity cylinder primitive, 2 units tall, centred) to the profile.</summary>
    public void Sync_Visual()
    {
        var vis = transform.Find("Visual");
        if (vis == null) return;
        float half = Profile.Length * 0.5f;
        vis.localPosition = new Vector3(0f, half, 0f);
        vis.localRotation = Quaternion.identity;
        vis.localScale = new Vector3(Profile.Diameter, half, Profile.Diameter);
    }

    void OnValidate()
    {
        Sync_Visual();
    }
}
