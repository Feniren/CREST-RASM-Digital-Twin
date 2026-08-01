using UnityEngine;

/// <summary>
/// Owns the visual engraving state for an engravable object.
///
/// The object's physical colliders should be primitive or convex.
/// Exact raycast queries are handled by a separate EngraveDetector.
/// </summary>
[DisallowMultipleComponent]
public sealed class EngravableSurface : MonoBehaviour
{
    [Header("Surface")]
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;

    [Tooltip("Separate object containing the non-convex engraving MeshCollider.")]
    [SerializeField] private EngraveDetector engraveDetector;

    [Header("Texture Settings")]
    [SerializeField, Min(1)] private int atlasWidth = 512;
    [SerializeField, Min(1)] private int atlasHeight = 512;

    [Header("Shaders")]
    [SerializeField] private Shader laserRevealShader;
    [SerializeField] private Shader revealBrushShader;

    public Texture2D Atlas { get; private set; }
    public RenderTexture RevealMask { get; private set; }

    public Mesh SurfaceMesh => _runtimeMesh;
    public MeshRenderer SurfaceRenderer => meshRenderer;
    public EngraveDetector Detector => engraveDetector;

    private Mesh _runtimeMesh;
    private Material _overlayMaterial;
    private Material _revealBrushMaterial;
    private Material[] _originalMaterials;

    private void Reset()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();

        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        if (meshFilter == null || meshRenderer == null)
        {
            Debug.LogError(
                $"{nameof(EngravableSurface)} on {name} requires a " +
                $"{nameof(MeshFilter)} and {nameof(MeshRenderer)}.", this);

            enabled = false;
            return;
        }

        if (laserRevealShader == null)
            laserRevealShader = Shader.Find("Custom/LaserReveal");

        if (revealBrushShader == null)
            revealBrushShader = Shader.Find("Hidden/RevealBrush");

        if (laserRevealShader == null || revealBrushShader == null)
        {
            Debug.LogError(
                $"Could not find the required laser engraving shaders on {name}.",
                this);

            enabled = false;
            return;
        }

        CreateRuntimeMesh();
        CreateEngravingTextures();
        CreateMaterials();
        ConfigureRenderer();
        ConfigureProxy();

        Debug.Log($"[EngravableSurface] '{name}' initialized. Atlas={atlasWidth}x{atlasHeight}, " +
            $"runtimeMesh subMeshCount={_runtimeMesh.subMeshCount}, detector={(engraveDetector != null ? engraveDetector.name : "none")}.", this);
    }

    /// <summary>
    /// Creates a private mesh instance and adds an overlay submesh containing
    /// the same triangles as submesh zero.
    /// </summary>
    private void CreateRuntimeMesh()
    {
        Mesh sourceMesh = meshFilter.sharedMesh;

        if (sourceMesh == null)
        {
            Debug.LogError($"{name} has no mesh assigned.", this);
            enabled = false;
            return;
        }

        _runtimeMesh = Instantiate(sourceMesh);
        _runtimeMesh.name = $"{sourceMesh.name} (Engravable Instance)";

        int[] surfaceTriangles = _runtimeMesh.GetTriangles(0);

        // Preserve existing submeshes and append the engraving overlay.
        int overlaySubmeshIndex = _runtimeMesh.subMeshCount;
        _runtimeMesh.subMeshCount = overlaySubmeshIndex + 1;
        _runtimeMesh.SetTriangles(surfaceTriangles, overlaySubmeshIndex);

        meshFilter.sharedMesh = _runtimeMesh;
    }

    private void CreateEngravingTextures()
    {
        Atlas = new Texture2D(atlasWidth, atlasHeight, TextureFormat.RGBA32, false)
        {
            name = $"{name} Engraving Atlas",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] clearPixels = new Color32[atlasWidth * atlasHeight];

        // White RGB with zero alpha.
        Color32 transparentWhite = new Color32(255, 255, 255, 0);

        for (int i = 0; i < clearPixels.Length; i++)
            clearPixels[i] = transparentWhite;

        Atlas.SetPixels32(clearPixels);
        Atlas.Apply(false, false);

        RevealMask = new RenderTexture(atlasWidth, atlasHeight, 0, RenderTextureFormat.R8)
        {
            name = $"{name} Reveal Mask",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false
        };

        RevealMask.Create();
        ClearRevealMask();
    }

    private void CreateMaterials()
    {
        _overlayMaterial = new Material(laserRevealShader)
        {
            name = $"{name} Laser Reveal Material"
        };

        _overlayMaterial.SetTexture("_EngraveTex", Atlas);
        _overlayMaterial.SetTexture("_RevealMask", RevealMask);
        _overlayMaterial.SetColor("_Color", Color.white);

        _revealBrushMaterial = new Material(revealBrushShader)
        {
            name = $"{name} Reveal Brush Material"
        };
    }

    private void ConfigureRenderer()
    {
        _originalMaterials = meshRenderer.sharedMaterials;

        Material baseMaterial = _originalMaterials.Length > 0
        ? _originalMaterials[0]
        : null;

        int materialCount = _runtimeMesh.subMeshCount;
        Material[] materials = new Material[materialCount];

        // Preserve existing materials where possible.
        for (int i = 0; i < materialCount - 1; i++)
        {
            materials[i] = i < _originalMaterials.Length
            ? _originalMaterials[i]
            : baseMaterial;
        }

        // The final submesh is the engraving overlay.
        materials[materialCount - 1] = _overlayMaterial;

        meshRenderer.sharedMaterials = materials;
    }

    private void ConfigureProxy()
    {
        if (engraveDetector == null)
        {
            Debug.LogWarning($"{name} has no engraving proxy assigned. " +
                "Visual engraving will work, but precise engraving raycasts will not.", this);

            return;
        }

        engraveDetector.Configure(owner: this, followedTransform: meshFilter.transform,
                                  queryMesh: _runtimeMesh);
    }

    /// <summary>
    /// Paints a reveal circle at a UV coordinate.
    /// Radius is measured in normalized 0-1 UV space.
    /// </summary>
    public void PaintReveal(Vector2 uv, float radiusUV, float hardness = 0.8f)
    {
        if (_revealBrushMaterial == null || RevealMask == null)
        {
            Debug.LogWarning($"[EngravableSurface] '{name}' PaintReveal skipped: " +
                $"revealBrushMaterial={(_revealBrushMaterial != null ? "set" : "null")}, " +
                $"RevealMask={(RevealMask != null ? "set" : "null")}.", this);
            return;
        }

        _revealBrushMaterial.SetVector("_BrushPos", new Vector4(uv.x, uv.y, 0f, 0f));

        _revealBrushMaterial.SetFloat("_BrushRadius", Mathf.Max(0f, radiusUV));

        _revealBrushMaterial.SetFloat(
            "_Hardness",
            Mathf.Clamp01(hardness));

        /*
         * Blitting from a RenderTexture back into itself can have undefined
         * results. Use a temporary texture as the source.
         */
        RenderTexture temporary = RenderTexture.GetTemporary(
            RevealMask.descriptor);

        Graphics.Blit(RevealMask, temporary);
        Graphics.Blit(temporary, RevealMask, _revealBrushMaterial);

        RenderTexture.ReleaseTemporary(temporary);
    }

    /// <summary>
    /// Resets the reveal mask to fully hidden.
    /// </summary>
    public void ClearRevealMask()
    {
        if (RevealMask == null)
            return;

        RenderTexture previous = RenderTexture.active;

        RenderTexture.active = RevealMask;
        GL.Clear(false, true, Color.black);
        RenderTexture.active = previous;
    }

    /// <summary>
    /// Stamps a texture into the engraving atlas.
    ///
    /// originX and originY are atlas pixel coordinates.
    /// The stamp is counter-rotated so it remains aligned with world space.
    /// </summary>
    public void PaintShape(
        Texture2D shape,
        int originX,
        int originY,
        float intensity)
    {
        if (shape == null || Atlas == null)
        {
            Debug.LogWarning($"[EngravableSurface] '{name}' PaintShape skipped: " +
                $"shape={(shape != null ? "set" : "null")}, Atlas={(Atlas != null ? "set" : "null")}.", this);
            return;
        }

        int shapeWidth = shape.width;
        int shapeHeight = shape.height;

        float angleRadians =
        -meshFilter.transform.eulerAngles.y * Mathf.Deg2Rad;

        float cos = Mathf.Cos(angleRadians);
        float sin = Mathf.Sin(angleRadians);

        float halfDiagonal = 0.5f * Mathf.Sqrt(shapeWidth * shapeWidth + shapeHeight * shapeHeight);

        float halfWidth = shapeWidth * 0.5f;
        float halfHeight = shapeHeight * 0.5f;

        int startX = Mathf.Max(0, Mathf.FloorToInt(originX - halfDiagonal));

        int startY = Mathf.Max(0, Mathf.FloorToInt(originY - halfDiagonal));

        int endX = Mathf.Min(Atlas.width, Mathf.CeilToInt(originX + halfDiagonal));

        int endY = Mathf.Min(Atlas.height, Mathf.CeilToInt(originY + halfDiagonal));

        int regionWidth = endX - startX;
        int regionHeight = endY - startY;

        if (regionWidth <= 0 || regionHeight <= 0)
        {
            Debug.LogWarning($"[EngravableSurface] '{name}' PaintShape skipped: stamp region out of atlas bounds " +
                $"(origin=({originX},{originY}), shape={shapeWidth}x{shapeHeight}, atlas={Atlas.width}x{Atlas.height}).", this);
            return;
        }

        Color[] atlasPixels = Atlas.GetPixels(startX, startY, regionWidth, regionHeight);

        Color[] shapePixels = shape.GetPixels();

        for (int pixelY = startY; pixelY < endY; pixelY++)
        {
            for (int pixelX = startX; pixelX < endX; pixelX++)
            {
                float offsetX = pixelX - originX;
                float offsetY = pixelY - originY;

                float shapeX =
                offsetX * cos -
                offsetY * sin +
                halfWidth;

                float shapeY =
                offsetX * sin +
                offsetY * cos +
                halfHeight;

                int sampleX = Mathf.RoundToInt(shapeX);
                int sampleY = Mathf.RoundToInt(shapeY);

                if ((uint)sampleX >= (uint)shapeWidth || (uint)sampleY >= (uint)shapeHeight)
                    continue;


                float mask = shapePixels[sampleY * shapeWidth + sampleX].a;

                if (mask <= 0f) continue;

                int atlasIndex = (pixelY - startY) * regionWidth + (pixelX - startX);

                Color atlasPixel = atlasPixels[atlasIndex];

                atlasPixel.a = Mathf.Clamp01(atlasPixel.a + mask * intensity);

                atlasPixels[atlasIndex] = atlasPixel;
            }
        }

        Atlas.SetPixels(startX, startY, regionWidth, regionHeight, atlasPixels);

        Atlas.Apply(false, false);

        Debug.Log($"[EngravableSurface] '{name}' PaintShape stamped region ({startX},{startY}) to ({endX},{endY}), intensity={intensity}.", this);
    }

    /// <summary>
    /// Converts normalized UV coordinates into atlas pixel coordinates.
    /// </summary>
    public Vector2Int UVToAtlasPixel(Vector2 uv)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt(uv.x * Atlas.width), 0, Atlas.width - 1);

        int y = Mathf.Clamp(Mathf.FloorToInt(uv.y * Atlas.height), 0, Atlas.height - 1);

        return new Vector2Int(x, y);
    }

    private void OnDestroy()
    {
        if (meshRenderer != null && _originalMaterials != null)
            meshRenderer.sharedMaterials = _originalMaterials;

        if (RevealMask != null)
        {
            RevealMask.Release();
            Destroy(RevealMask);
        }

        if (Atlas != null) Destroy(Atlas);

        if (_overlayMaterial != null) Destroy(_overlayMaterial);

        if (_revealBrushMaterial != null) Destroy(_revealBrushMaterial);

        if (_runtimeMesh != null) Destroy(_runtimeMesh);
    }
}
