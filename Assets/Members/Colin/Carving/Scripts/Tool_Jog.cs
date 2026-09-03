using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Keyboard jog for the sandbox (Input System package; legacy Input is disabled in this project).
/// WASD = world X/Z, E/Q = up/down (mill tools only), Tab = next tool, R = reset stocks,
/// [ ] = halve/double feed. Every listed tool carves every frame, so Scene-view dragging works too.
/// </summary>
public class Tool_Jog : MonoBehaviour
{
    public List<Carving_Tool> Tools = new List<Carving_Tool>();
    public Mill_Stock Mill;
    public Lathe_Stock Lathe;
    public float Feed_Mm_S = 20f;
    public int Active;

    readonly StringBuilder text = new StringBuilder();
    GUIStyle style;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && Tools.Count > 0)
        {
            Active = Mathf.Clamp(Active, 0, Tools.Count - 1);
            if (kb.tabKey.wasPressedThisFrame) Active = (Active + 1) % Tools.Count;
            if (kb.rKey.wasPressedThisFrame)
            {
                if (Mill != null) Mill.Reset_Stock();
                if (Lathe != null) Lathe.Reset_Stock();
            }
            if (kb.leftBracketKey.wasPressedThisFrame) Feed_Mm_S = Mathf.Max(1f, Feed_Mm_S * 0.5f);
            if (kb.rightBracketKey.wasPressedThisFrame) Feed_Mm_S = Mathf.Min(500f, Feed_Mm_S * 2f);

            var tool = Tools[Active];
            if (tool != null)
            {
                Vector3 d = Vector3.zero;
                if (kb.wKey.isPressed) d.z += 1f;
                if (kb.sKey.isPressed) d.z -= 1f;
                if (kb.dKey.isPressed) d.x += 1f;
                if (kb.aKey.isPressed) d.x -= 1f;
                if (!tool.Is_Lathe_Tool)
                {
                    if (kb.eKey.isPressed) d.y += 1f;
                    if (kb.qKey.isPressed) d.y -= 1f;
                }
                if (d != Vector3.zero) tool.transform.position += d.normalized * (Feed_Mm_S * 0.001f * Time.deltaTime);
            }
        }

        for (int i = 0; i < Tools.Count; i++)
            if (Tools[i] != null) Tools[i].Carve_Step();
    }

    void OnGUI()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 13, richText = false };
            style.normal.textColor = Color.white;
        }
        text.Clear();
        text.AppendLine("Carving Sandbox  (click the Game view so keys register)");
        var tool = Tools.Count > 0 && Active >= 0 && Active < Tools.Count ? Tools[Active] : null;
        text.AppendLine(tool != null ? "Active: " + tool.name + "  " + tool.Profile : "Active: none");
        text.AppendLine("Tab next tool   WASD move X/Z   E/Q up/down (mill)   [ ] feed " + Feed_Mm_S.ToString("0.#") + " mm/s   R reset");
        if (Mill != null && Mill.Data != null)
            text.AppendLine("Mill: " + Mill.Data.Nx + "x" + Mill.Data.Nz + " cells @" + (Mill.Cell * 1000f).ToString("0.##") + " mm, " + Mill.Data.Slab_Count + " slabs, "
                            + Mill.Vertex_Count + " verts, remesh " + Mill.Last_Remesh_Ms.ToString("0.0") + " ms");
        if (Lathe != null && Lathe.Data != null)
            text.AppendLine("Lathe: " + Lathe.Data.Nz + "x" + Lathe.Data.Nr + " cells @" + (Lathe.Cell * 1000f).ToString("0.##") + " mm, "
                            + Lathe.Vertex_Count + " verts, remesh " + Lathe.Last_Remesh_Ms.ToString("0.0") + " ms");
        GUI.Label(new Rect(10f, 10f, 640f, 110f), text.ToString(), style);
    }
}
