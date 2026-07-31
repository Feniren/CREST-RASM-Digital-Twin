using System.Collections;
using UnityEngine;

public class Laser_Head : MonoBehaviour
{
	public float intensity = 100f;
	public EngraveMask ActiveMask;

	public float atlasPixelsPerInch = 100f;

	[Tooltip("Reveal brush radius in UV space (0-1). Tune to match beam width.")]
	public float revealRadiusUV = 0.02f;

	[Tooltip("Brush edge softness.")]
	[Range(0f, 1f)]
	public float revealHardness = 0.8f;

	[Header("Movement Settings")]
	public float moveSpeed = 2f;
	[Tooltip("How far the laser head moves down on the Z axis per pass.")]
	public float stepSize = 0.005f;

	private Coroutine _engraveCoroutine;

	void Update()
	{
		Debug.DrawRay(transform.position, Vector3.down * 100f, Color.red);
	}

    private void UpdateEngraving()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 100f))
        {
            Item_Epoxy_Block block = hit.collider.GetComponent<Item_Epoxy_Block>();
            if (block != null)
            {
                block.PaintReveal(hit.textureCoord, revealRadiusUV, revealHardness);
            }
        }
    }

    /// <summary>
    /// Stamps the mask atlas immediately and resets the reveal mask.
    /// Engraving becomes visible as the head moves over the block.
    /// </summary>
    [ContextMenu("Try to Apply Job")]
	public void TryApplyJob()
    {
        if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 100f))
            return;

        Item_Epoxy_Block block = hit.collider.GetComponent<Item_Epoxy_Block>();
        if (block == null) return;

        int originX = Mathf.FloorToInt(hit.textureCoord.x * block.Atlas.width);
        int originY = Mathf.FloorToInt(hit.textureCoord.y * block.Atlas.height);

        block.ClearRevealMask();
        Texture2D mask = ActiveMask.GetResampledMask(atlasPixelsPerInch);
        block.PaintShape	(mask, originX, originY, intensity);

        if (_engraveCoroutine != null) StopCoroutine(_engraveCoroutine);
        _engraveCoroutine = StartCoroutine(RasterScanMovement(hit.collider.bounds));
    }

    public void LoadMask(EngraveMask mask)
	{
		ActiveMask = mask;
		if (_engraveCoroutine != null)
		{
			StopCoroutine(_engraveCoroutine);
			_engraveCoroutine = null;
		}
	}


	/// <summary>
	/// Moves the laser head back and forth (X-axis) while stepping down (Z-axis).
	/// </summary>
	private IEnumerator RasterScanMovement(Bounds targetBounds)
	{
		float minX = targetBounds.min.x;
		float maxX = targetBounds.max.x;

		float currentZ = targetBounds.max.z;
		float minZ = targetBounds.min.z;

		Vector3 startCorner = new Vector3(minX, transform.position.y, currentZ);
		while (Vector3.Distance(transform.position, startCorner) > 0.001f)
		{
			transform.position = Vector3.MoveTowards(transform.position, startCorner, moveSpeed * Time.deltaTime);
			yield return null;
		}

		bool movingRight = true;

		while (currentZ >= minZ)
		{
			float targetX = movingRight ? maxX : minX;
			Vector3 passTarget = new Vector3(targetX, transform.position.y, currentZ);

			while (Vector3.Distance(transform.position, passTarget) > 0.001f)
			{
				transform.position = Vector3.MoveTowards(transform.position, passTarget, moveSpeed * Time.deltaTime);
                UpdateEngraving();
                yield return null;
			}

			currentZ -= stepSize;
			movingRight = !movingRight; // Reverse direction for the next pass

			if (currentZ >= minZ)
			{
				Vector3 stepDownTarget = new Vector3(transform.position.x, transform.position.y, currentZ);
				while (Vector3.Distance(transform.position, stepDownTarget) > 0.001f)
				{
					transform.position = Vector3.MoveTowards(transform.position, stepDownTarget, moveSpeed * Time.deltaTime);
					yield return null;
				}
			}
		}

		Debug.Log("Laser Engraving Finished.");
		_engraveCoroutine = null;
	}
}
