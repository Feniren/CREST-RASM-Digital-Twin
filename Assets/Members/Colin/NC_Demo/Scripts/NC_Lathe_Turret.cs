using UnityEngine;

/// <summary>
/// The lathe's tool turret: T1..Tn are Carving_Tools fixed on stations spaced evenly around
/// the turret's local Z axis (station k authored at (k-1)·360/n degrees). The ProTurn's 4-station
/// turret indexes about a vertical axis with the tools facing outward off-centre, so the Turret
/// transform sits at identity under a mount that holds the pivot (ring centre, local Z = down).
/// Indexing T turns the turret so that station's tool is presented at the cut. Indexing is
/// instant; the runner's tool-change pause covers it.
/// </summary>
public class NC_Lathe_Turret : MonoBehaviour, I_NC_Tool_Changer
{
    [Tooltip("The part that rotates; its local Z is the index axis; Index() owns its localRotation, so pose it with a parent")]
    [SerializeField] private Transform Turret;
    [Tooltip("Index = T - 1; each tool sits on its station under Turret")]
    [SerializeField] private Carving_Tool[] Tools;
    [Tooltip("Readout text per station, same order as Tools, e.g. \"R0.8 rough\"")]
    [SerializeField] private string[] Labels;
    [Tooltip("Turret local Z angle that presents T1; T k is presented at this minus (k-1)·360/n")]
    [SerializeField] private float T1_Angle;

    public int Active { get; private set; }
    public Carving_Tool Active_Tool => Has(Active) ? Tools[Active - 1] : null;
    public int Station_Count => Tools != null ? Tools.Length : 0;

    void Awake()
    {
        if (Turret == null || Tools == null || Tools.Length == 0)
        {
            Debug.LogWarning("NC_Lathe_Turret: Turret and Tools must be assigned.");
            enabled = false;
            return;
        }
        for (int i = 0; i < Tools.Length; i++)
        {
            if (Tools[i] != null) continue;
            Debug.LogWarning("NC_Lathe_Turret: Tools[" + i + "] is not assigned.");
            enabled = false;
            return;
        }
    }

    public bool Has(int t) => Tools != null && t > 0 && t <= Tools.Length && Tools[t - 1] != null;

    /// <summary>The turret angle that presents station t.</summary>
    public float Index_Angle(int t) => T1_Angle - (t - 1) * 360f / Tools.Length;

    /// <summary>Turns station t to the work (a teleport) and binds its tool; null if there is no such station.</summary>
    public Carving_Tool Mount(int t, Lathe_Stock stock)
    {
        if (!Has(t)) return null;
        if (Active != t)
        {
            var previous = Active_Tool;
            if (previous != null) previous.Bind((Lathe_Stock)null);
            Index(t);
            Active = t;
        }
        var tool = Tools[t - 1];
        tool.Bind(stock);       // after the index, so the swing is never carved
        return tool;
    }

    /// <summary>Editor/validator use too: rotates the turret to present station t.</summary>
    public void Index(int t)
    {
        Turret.localRotation = Quaternion.Euler(0f, 0f, Index_Angle(t));
    }

    public void Bind_Stock(Component stock)
    {
        var tool = Active_Tool;
        if (tool != null) tool.Bind(stock as Lathe_Stock);
    }

    public string Describe(int t)
    {
        if (t == 0) return "no tool";
        if (!Has(t)) return "T" + t + " (missing)";
        string label = Labels != null && t - 1 < Labels.Length && !string.IsNullOrEmpty(Labels[t - 1]) ? Labels[t - 1] : Tools[t - 1].Profile.ToString();
        return "T" + t + " " + label;
    }
}
