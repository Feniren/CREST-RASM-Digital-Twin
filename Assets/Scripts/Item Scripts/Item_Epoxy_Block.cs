using UnityEngine;
using UnityEngine.Rendering;

public class Item_Epoxy_Block : Item_Parent
{
    public Texture2D Atlas { get; private set; }
    public RenderTexture RevealMask { get; private set; }

    // Kept for legacy PaintAt support
    public int brushRadius = 5;

    private Material _overlayMat;
    private Material _revealBrushMat;

    public Item_Epoxy_Block()
    {
        Name = "Epoxy Block";
        Pickup = true;
        Quantity = 1;
    }

    public override void Start()
    {
        base.Start();

        MeshFilter filter = GetComponent<MeshFilter>();
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        Material baseMat = renderer.material;

        // Second submesh shares the same triangles as submesh 0
        Mesh mesh = Instantiate(filter.mesh);
        int[] tris = mesh.GetTriangles(0);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(tris, 0);
        mesh.SetTriangles(tris, 1);
        filter.mesh = mesh;

        MeshCollider col = GetComponent<MeshCollider>();
        if (col == null) col = gameObject.AddComponent<MeshCollider>();
        col.sharedMesh = mesh;

        // Atlas: persistent engraving result, starts fully transparent
        Atlas = new Texture2D(512, 512, TextureFormat.RGBA32, false);
        Color[] clear = new Color[512 * 512];
        for (int i = 0; i < clear.Length; i++)
            clear[i] = new Color(1f, 1f, 1f, 0f);
        Atlas.SetPixels(clear);
        Atlas.Apply();

        // RevealMask: GPU-side, starts black (nothing revealed)
        RevealMask = new RenderTexture(512, 512, 0, RenderTextureFormat.R8);
        RevealMask.filterMode = FilterMode.Bilinear;
        RevealMask.Create();
        ClearRevealMask();

        // Overlay material using the custom reveal shader
        _overlayMat = new Material(Shader.Find("Custom/LaserReveal"));
        _overlayMat.SetTexture("_EngraveTex", Atlas);
        _overlayMat.SetTexture("_RevealMask", RevealMask);
        _overlayMat.SetColor("_Color", Color.white);

        // Brush material used by PaintReveal
        _revealBrushMat = new Material(Shader.Find("Hidden/RevealBrush"));

        renderer.materials = new Material[] { baseMat, _overlayMat };
    }

    /// <summary>
    /// Paints a reveal circle at the given UV coordinate.
    /// radiusUV is in 0-1 UV space.
    /// </summary>
    public void PaintReveal(Vector2 uv, float radiusUV, float hardness = 0.8f)
    {
        _revealBrushMat.SetVector("_BrushPos", new Vector4(uv.x, uv.y, 0, 0));
        _revealBrushMat.SetFloat("_BrushRadius", radiusUV);
        _revealBrushMat.SetFloat("_Hardness", hardness);

        RenderTexture prev = RenderTexture.active;
        Graphics.Blit(RevealMask, RevealMask, _revealBrushMat);
        RenderTexture.active = prev;
    }

    /// <summary>
    /// Resets the reveal mask to fully hidden.
    /// </summary>
    public void ClearRevealMask()
    {
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = RevealMask;
        GL.Clear(false, true, Color.black);
        RenderTexture.active = prev;
    }

    /// <summary>
    /// Stamps a shape texture onto the atlas at the given center pixel position.
    /// Counter-rotates against the object's Y rotation so the stamp appears
    /// axis-aligned in world space regardless of object orientation.
    /// </summary>
    public void PaintShape(Texture2D shape, int originX, int originY, float intensity)
    {
        if (shape == null) return;

        int shapeW = shape.width;
        int shapeH = shape.height;

        // Negate to counter-rotate: world-aligned stamp cancels object rotation
        float angleRad = -transform.localEulerAngles.y * Mathf.Deg2Rad;
        float cos = Mathf.Cos(angleRad);
        float sin = Mathf.Sin(angleRad);

        float halfDiag = 0.5f * Mathf.Sqrt(shapeW * shapeW + shapeH * shapeH);
        float halfW = shapeW * 0.5f;
        float halfH = shapeH * 0.5f;

        int startX = Mathf.Max(0, Mathf.FloorToInt(originX - halfDiag));
        int startY = Mathf.Max(0, Mathf.FloorToInt(originY - halfDiag));
        int endX = Mathf.Min(Atlas.width, Mathf.CeilToInt(originX + halfDiag));
        int endY = Mathf.Min(Atlas.height, Mathf.CeilToInt(originY + halfDiag));

        int regionW = endX - startX;
        int regionH = endY - startY;
        if (regionW <= 0 || regionH <= 0) return;

        Color[] atlasPixels = Atlas.GetPixels(startX, startY, regionW, regionH);
        Color[] shapePixels = shape.GetPixels();

        for (int py = startY; py < endY; py++)
        {
            for (int px = startX; px < endX; px++)
            {
                float dx = px - originX;
                float dy = py - originY;

                // Rotate atlas-space offset into shape-space, centered on shape
                float sx = dx * cos - dy * sin + halfW;
                float sy = dx * sin + dy * cos + halfH;

                int ix = Mathf.RoundToInt(sx);
                int iy = Mathf.RoundToInt(sy);

                if ((uint)ix >= (uint)shapeW || (uint)iy >= (uint)shapeH) continue;

                float mask = shapePixels[iy * shapeW + ix].a;
                if (mask <= 0f) continue;

                int ai = (py - startY) * regionW + (px - startX);
                atlasPixels[ai].a = Mathf.Clamp01(atlasPixels[ai].a + mask * intensity);
            }
        }

        Atlas.SetPixels(startX, startY, regionW, regionH, atlasPixels);
        Atlas.Apply(); // note: if paint_shape ends up being called repeatedly,
                       // move Apply() responsibility to caller for better performance.
    }
    /// <summary>
    /// Paints opacity directly at a pixel position. Kept for legacy use.
    /// </summary>
    public void PaintAt(int centerX, int centerY, float increase)
    {
        for (int dx = -brushRadius; dx <= brushRadius; dx++)
        {
            for (int dy = -brushRadius; dy <= brushRadius; dy++)
            {
                int px = centerX + dx;
                int py = centerY + dy;

                if (px < 0 || px >= Atlas.width || py < 0 || py >= Atlas.height)
                    continue;
                if (dx * dx + dy * dy > brushRadius * brushRadius)
                    continue;

                Color c = Atlas.GetPixel(px, py);
                c.a = Mathf.Clamp01(c.a + increase);
                Atlas.SetPixel(px, py, c);
            }
        }

        Atlas.Apply();
    }

    void OnDestroy()
    {
        if (RevealMask != null) RevealMask.Release();
    }
}