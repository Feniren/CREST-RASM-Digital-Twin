using UnityEngine;
using UnityEngine.Rendering;

public class e_Item_Epoxy_Block : Item_Parent
{
	public Texture2D Atlas { get; private set; }
	public int brushRadius = 5;

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

		// Duplicate the triangle list as a second submesh
		Mesh mesh = Instantiate(filter.mesh);
		int[] tris = mesh.GetTriangles(0);
		mesh.subMeshCount = 2;
		mesh.SetTriangles(tris, 0); // submesh 0: base material
		mesh.SetTriangles(tris, 1); // submesh 1: engraving overlay
		filter.mesh = mesh;

		// MeshCollider needs the updated mesh
		MeshCollider collider = GetComponent<MeshCollider>();
		if (collider == null) collider = gameObject.AddComponent<MeshCollider>();
		collider.sharedMesh = mesh;

		// Build transparent overlay texture
		Atlas = new Texture2D(512, 512, TextureFormat.RGBA32, false);
		Color[] clear = new Color[512 * 512];
		for (int i = 0; i < clear.Length; i++)
			clear[i] = new Color(1f, 1f, 1f, 0.0f);
		Atlas.SetPixels(clear);
		Atlas.Apply();

		Material overlayMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
		overlayMat.SetFloat("_Surface", 1);
		overlayMat.mainTexture = Atlas;

		CoreUtils.SetKeyword(overlayMat, "_SURFACE_TYPE_TRANSPARENT", true);
		overlayMat.SetFloat("_Surface", 1);
		overlayMat.SetFloat("_Blend", 0); // 0 = Alpha
		overlayMat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
		overlayMat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
		overlayMat.SetFloat("_ZWrite", 0);
		overlayMat.renderQueue = (int)RenderQueue.Transparent;

		renderer.materials = new Material[] { baseMat, overlayMat };
	}

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
}

