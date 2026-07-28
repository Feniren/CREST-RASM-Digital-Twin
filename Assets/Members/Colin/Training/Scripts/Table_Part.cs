using UnityEngine;

// A duplicated component sitting on the Module 1 parts table. Table_Part_Display
// toggles its glow shell to mark it as the part the trainee is currently
// identifying. Mirrors the glow handling in Component_Marker.
public class Table_Part : MonoBehaviour{
    public string Part_Id;

    [SerializeField] private Renderer Glow;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock block;

    public void Set_Glow(bool on, Color color){
        if (Glow == null)
            return;

        Glow.enabled = on;

        if (!on)
            return;

        block ??= new MaterialPropertyBlock();
        Glow.GetPropertyBlock(block);
        block.SetColor(BaseColorId, color);
        Glow.SetPropertyBlock(block);
    }
}
