using UnityEngine;

/// <summary>
/// The tool table: T1..Tn are Carving_Tools parked in rack slots (index = T - 1). A tool change
/// is a teleport — the previous tool goes back to its slot, the next hangs from the holder with
/// its shank at the spindle nose. Binding happens after the move so the jump is never carved.
/// </summary>
public class NC_Tool_Rack : MonoBehaviour
{
    [Tooltip("Index = T - 1")]
    [SerializeField] private Carving_Tool[] Tools;
    [Tooltip("Park position per tool, same order as Tools")]
    [SerializeField] private Transform[] Slots;

    public int Active { get; private set; }   // T number in the spindle, 0 = none
    public Carving_Tool Active_Tool => Has(Active) ? Tools[Active - 1] : null;

    void Awake()
    {
        if (Tools == null || Slots == null || Tools.Length != Slots.Length)
        {
            Debug.LogWarning("NC_Tool_Rack: Tools and Slots must be assigned with the same length.");
            enabled = false;
            return;
        }
        for (int i = 0; i < Tools.Length; i++)
        {
            if (Tools[i] != null && Slots[i] != null) continue;
            Debug.LogWarning("NC_Tool_Rack: Tools[" + i + "] / Slots[" + i + "] is not assigned.");
            enabled = false;
            return;
        }
    }

    public bool Has(int t) => Tools != null && t > 0 && t <= Tools.Length && Tools[t - 1] != null;

    public Carving_Tool Mount(int t, Transform holder, Mill_Stock stock)
    {
        if (!Has(t)) return null;
        var tool = Tools[t - 1];
        if (Active != t)
        {
            Park_Active();
            tool.transform.SetParent(holder, false);
            tool.transform.localPosition = new Vector3(0f, -tool.Profile.Length, 0f);   // shank at the nose, tip below it
            tool.transform.localRotation = Quaternion.identity;
            Active = t;
        }
        tool.Bind(stock);
        return tool;
    }

    public void Park_Active()
    {
        var tool = Active_Tool;
        if (tool != null)
        {
            tool.Bind(null);
            tool.transform.SetParent(Slots[Active - 1], false);
            tool.transform.localPosition = Vector3.zero;
            tool.transform.localRotation = Quaternion.identity;
        }
        Active = 0;
    }

    public void Bind_Stock(Mill_Stock stock)
    {
        var tool = Active_Tool;
        if (tool != null) tool.Bind(stock);
    }

    /// <summary>"T1 Ø10 Flat" for labels; "no tool" when nothing is mounted.</summary>
    public string Describe(int t)
    {
        if (t == 0) return "no tool";
        if (!Has(t)) return "T" + t + " (missing)";
        var p = Tools[t - 1].Profile;
        return "T" + t + " Ø" + (p.Diameter * NC_Kinematics.M_To_Mm).ToString("0.#") + " " + p.Tip_Shape;
    }
}
