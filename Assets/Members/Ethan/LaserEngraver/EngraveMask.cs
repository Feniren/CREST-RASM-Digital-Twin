using UnityEngine;

/// <summary>
/// Represents a resolved laser engraving raster mask
/// ready to be stamped onto a target atlas.
/// </summary>
public class EngraveMask
{
	public Texture2D RasterMask { get; private set; }

	// Machine-space dimensions in inches
	public float WidthInches { get; private set; }
	public float HeightInches { get; private set; }
	public int DPI { get; private set; }

	public EngraveMask(Texture2D mask, float widthInches, float heightInches, int dpi)
	{
		RasterMask = mask;
		WidthInches = widthInches;
		HeightInches = heightInches;
		DPI = dpi;
	}

    /// <summary>
    /// Loads a raster image (PNG/JPG) and treats it as a grayscale burn mask.
    /// White = full burn, black = no burn (easy to invert).
    /// </summary>
    public static EngraveMask FromImage(byte[] fileData, float widthInches, int dpi)
    {
        Texture2D source = new Texture2D(2, 2);
        source.LoadImage(fileData);
        return FromImage(source, widthInches, dpi);
    }

    public static EngraveMask FromImage(Texture2D source, float widthInches, int dpi)
    {
        source.filterMode = FilterMode.Point;

        float heightInches = widthInches * ((float)source.height / source.width);

        // Convert to single-channel alpha mask
        Texture2D mask = new Texture2D(source.width, source.height, TextureFormat.Alpha8, false);
        Color[] pixels = source.GetPixels();

        for (int i = 0; i < pixels.Length; i++)
        {
            float lum = pixels[i].grayscale; // BLACK = BURN: 1 - pixels instead
            pixels[i] = new Color(0, 0, 0, lum);
        }

        mask.SetPixels(pixels);
        mask.Apply();

        return new EngraveMask(mask, widthInches, heightInches, dpi);
    }

    /// <summary>
    /// Returns the mask resampled to match a target atlas resolution.
    /// targetPixelsPerInch = atlas pixels per inch of machine space.
    /// </summary>
    public Texture2D GetResampledMask(float targetPixelsPerInch)
	{
		int targetW = Mathf.RoundToInt(WidthInches * targetPixelsPerInch);
		int targetH = Mathf.RoundToInt(HeightInches * targetPixelsPerInch);

		RenderTexture rt = RenderTexture.GetTemporary(targetW, targetH);
		Graphics.Blit(RasterMask, rt);

		Texture2D resampled = new Texture2D(targetW, targetH, TextureFormat.Alpha8, false);
		RenderTexture.active = rt;
		resampled.ReadPixels(new Rect(0, 0, targetW, targetH), 0, 0);
		resampled.Apply();

		RenderTexture.active = null;
		RenderTexture.ReleaseTemporary(rt);

		return resampled;
	}
}