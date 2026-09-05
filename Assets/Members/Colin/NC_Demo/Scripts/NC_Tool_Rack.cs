using UnityEngine;

/// <summary>
/// The tool table: T1..Tn are Carving_Tool cylinders parked in rack slots. A tool change is a
/// teleport — the previous tool goes back to its slot, the next hangs from the holder with its
/// shank at the spindle nose. Binding happens after the move so the jump is never carved.
/// </summary>
public class NC_Tool_Rack : MonoBehaviour
{
    public Carving_Tool[] Tools;      // index = T - 1
    public Transform[] Slots;

    public int Active { get; private set; }   // T number in the spindle, 0 = none
    public Carving_Tool Active_Tool => Active > 0 && Active <= Tools.Length ? Tools[Active - 1] : null;

    public bool Has(int t) => t > 0 && t <= Tools.Length && Tools[t - 1] != null;

    public Carving_Tool Mount(int t, Transform holder, Mill_Stock stock)
    {
        if (!Has(t)) return null;
        var tool = Tools[t - 1];
        if (Active != t)
        {
            Park_Active();
            tool.transform.SetParent(holder, false);
            tool.transform.localPosition = new Vector3(0f, -tool.Profile.Length, 0f);
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

    /// <summary>"T1 Ø10 Flat" for labels.</summary>
    public string Describe(int t)
    {
        if (!Has(t)) return "T" + t + " (missing)";
        var p = Tools[t - 1].Profile;
        return "T" + t + " Ø" + (p.Diameter * 1000f).ToString("0.#") + " " + p.Tip_Shape;
    }
}
