using UnityEngine;

public class Laser_Head : MonoBehaviour
{
	public float intensity = 100f;
	public Texture2D testShape;
	public PrintJob ActiveJob;

	public float atlasPixelsPerInch = 100f;

	[Tooltip("Reveal brush radius in UV space (0-1). Tune to match beam width.")]
	public float revealRadiusUV = 0.02f;

	[Tooltip("Brush edge softness.")]
	[Range(0f, 1f)]
	public float revealHardness = 0.8f;

	// Whether a job has been stamped onto the current target
	private bool _jobApplied;

	void Update()
	{
		Debug.DrawRay(transform.position, Vector3.down * 100f, Color.red);

		if (!_jobApplied) return;

		if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 100f))
			return;

		e_Item_Epoxy_Block block = hit.collider.GetComponent<e_Item_Epoxy_Block>();
		if (block == null) return;

		block.PaintReveal(hit.textureCoord, revealRadiusUV, revealHardness);
	}

	/// <summary>
	/// Stamps the job atlas immediately and resets the reveal mask.
	/// Engraving becomes visible as the head moves over the block.
	/// </summary>
	[ContextMenu("Try to Apply Job")]
	void TryApplyJob()
	{
		if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 100f))
			return;

		e_Item_Epoxy_Block block = hit.collider.GetComponent<e_Item_Epoxy_Block>();
		if (block == null) return;

		int originX = Mathf.FloorToInt(hit.textureCoord.x * block.Atlas.width);
		int originY = Mathf.FloorToInt(hit.textureCoord.y * block.Atlas.height);

		block.ClearRevealMask();
		Texture2D mask = ActiveJob.GetResampledMask(atlasPixelsPerInch);
		block.PaintShape(mask, originX, originY, intensity);

		_jobApplied = true;
	}

	public void LoadJob(PrintJob job)
	{
		ActiveJob = job;
		_jobApplied = false;
	}

	[ContextMenu("Test Printing Shape")]
	void TestPrintShape()
	{
		if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 100f))
			return;

		e_Item_Epoxy_Block block = hit.collider.GetComponent<e_Item_Epoxy_Block>();
		if (block == null)
		{
			Debug.LogWarning("Tried to print shape but didn't hit an engravable object.");
			return;
		}

		int x = Mathf.FloorToInt(hit.textureCoord.x * block.Atlas.width);
		int y = Mathf.FloorToInt(hit.textureCoord.y * block.Atlas.height);
		block.PaintShape(testShape, x, y, intensity);
	}
}