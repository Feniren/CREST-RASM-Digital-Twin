using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Keyboard jog for the sandbox (bindings live in Carving_Sandbox.inputactions): Move = X/Z,
/// Vertical = up/down (mill tools only), Next_Tool, Reset both stocks, Feed_Down/Feed_Up halve
/// and double the feed. Every listed tool carves every frame, so Scene-view dragging works too.
/// Stats go to the optional Readout text.
/// </summary>
public class Tool_Jog : MonoBehaviour
{
    [Header("Sandbox")]
    [SerializeField] private List<Carving_Tool> Tools = new List<Carving_Tool>();
    [SerializeField] private Mill_Stock Mill;
    [SerializeField] private Lathe_Stock Lathe;
    [SerializeField] private float Feed_Mm_S = 20f;

    [Header("Readout (optional)")]
    [SerializeField] private TMP_Text Readout;
    [SerializeField] private float Readout_Seconds = 0.1f;

    const float Min_Feed_Mm_S = 1f;
    const float Max_Feed_Mm_S = 500f;
    const float Mm_To_M = 0.001f;

    Carving_Sandbox_Input input;
    int active;
    float next_readout;
    readonly StringBuilder text = new StringBuilder();

    // Created here rather than in Awake: a script reload during Play empties non-serialized
    // fields and re-runs OnEnable, not Awake.
    void OnEnable()
    {
        if (input == null) input = new Carving_Sandbox_Input();
        input.Sandbox.Enable();
    }

    void OnDisable()
    {
        input?.Sandbox.Disable();
    }

    void OnDestroy()
    {
        input?.Dispose();
    }

    void Update()
    {
        if (Tools.Count > 0)
        {
            Read_Input();
            var tool = Tools[active];
            if (tool != null) Jog(tool, Time.deltaTime);
        }
        Carve_All();
        Refresh_Readout();
    }

    void Read_Input()
    {
        var a = input.Sandbox;
        active = Mathf.Clamp(active, 0, Tools.Count - 1);
        if (a.Next_Tool.WasPressedThisFrame()) active = (active + 1) % Tools.Count;
        if (a.Reset.WasPressedThisFrame())
        {
            if (Mill != null) Mill.Reset_Stock();
            if (Lathe != null) Lathe.Reset_Stock();
        }
        if (a.Feed_Down.WasPressedThisFrame()) Feed_Mm_S = Mathf.Max(Min_Feed_Mm_S, Feed_Mm_S * 0.5f);
        if (a.Feed_Up.WasPressedThisFrame()) Feed_Mm_S = Mathf.Min(Max_Feed_Mm_S, Feed_Mm_S * 2f);
    }

    void Jog(Carving_Tool tool, float dt)
    {
        Vector2 move = input.Sandbox.Move.ReadValue<Vector2>();
        float vertical = tool.Is_Lathe_Tool ? 0f : input.Sandbox.Vertical.ReadValue<float>();
        var d = new Vector3(move.x, vertical, move.y);
        if (d != Vector3.zero) tool.transform.position += d.normalized * (Feed_Mm_S * Mm_To_M * dt);
    }

    void Carve_All()
    {
        for (int i = 0; i < Tools.Count; i++)
            if (Tools[i] != null) Tools[i].Carve_Step();
    }

    void Refresh_Readout()
    {
        if (Readout == null || Time.unscaledTime < next_readout) return;
        next_readout = Time.unscaledTime + Readout_Seconds;

        text.Clear();
        var tool = Tools.Count > 0 ? Tools[active] : null;
        text.Append("Active: ").Append(tool != null ? tool.name : "none");
        if (tool != null && tool.Profile != null) text.Append("  ").Append(tool.Profile);
        text.Append("   feed ").Append(Feed_Mm_S.ToString("0.#")).AppendLine(" mm/s");
        if (Mill != null && Mill.Data != null)
        {
            text.Append("Mill: ").Append(Mill.Data.Nx).Append('x').Append(Mill.Data.Nz).Append(" cells @").Append((Mill.Data.Cell * 1000f).ToString("0.##")).Append(" mm, ")
                .Append(Mill.Data.Slab_Count).Append(" slabs, ").Append(Mill.Vertex_Count).Append(" verts, remesh ").Append(Mill.Last_Remesh_Ms.ToString("0.0")).AppendLine(" ms");
        }
        if (Lathe != null && Lathe.Data != null)
        {
            text.Append("Lathe: ").Append(Lathe.Data.Nz).Append('x').Append(Lathe.Data.Nr).Append(" cells @").Append((Lathe.Data.Cell * 1000f).ToString("0.##")).Append(" mm, ")
                .Append(Lathe.Vertex_Count).Append(" verts, remesh ").Append(Lathe.Last_Remesh_Ms.ToString("0.0")).AppendLine(" ms");
        }
        Readout.SetText(text);
    }
}
