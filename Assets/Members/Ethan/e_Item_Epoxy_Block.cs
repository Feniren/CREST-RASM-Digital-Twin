using UnityEngine;
using UnityEngine.Rendering;

public class e_Item_Epoxy_Block : Item_Parent
{
	public Texture2D Atlas { get; private set; }
	public RenderTexture RevealMask { get; private set; }

	// Kept for legacy PaintAt support
	public int brushRadius = 5;

	private Material _overlayMat;
	private Material _revealBrushMat;

	public e_Item_Epoxy_Block()
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
	/// Stamps a shape texture onto the atlas at the given top-left pixel position.
	/// </summary>
	public void PaintShape(Texture2D shape, int originX, int originY, float intensity)
	{
		int shapeW = shape.width;
		int shapeH = shape.height;

		int startX = Mathf.Max(0, -originX);
		int startY = Mathf.Max(0, -originY);
		int endX = Mathf.Min(shapeW, Atlas.width - originX);
		int endY = Mathf.Min(shapeH, Atlas.height - originY);

		for (int sx = startX; sx < endX; sx++)
		{
			for (int sy = startY; sy < endY; sy++)
			{
				float mask = shape.GetPixel(sx, sy).a;
				if (mask <= 0f) continue;

				int px = originX + sx;
				int py = originY + sy;

				Color c = Atlas.GetPixel(px, py);
				c.a = Mathf.Clamp01(c.a + mask * intensity);
				Atlas.SetPixel(px, py, c);
			}
		}

		Atlas.Apply();
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