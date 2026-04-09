using UnityEngine;

public class Laser_Head : MonoBehaviour
{
    public float intensity = 100f; // how quickly objects are engraved
	public Texture2D testShape;
	public PrintJob ActiveJob;

	// Atlas pixels (UV) per inch of machine bed
	// TODO: This absolutely needs tuning
	public float atlasPixelsPerInch = 100f;

	private bool _jobApplied; // Currently unused but almost certainly will need later

	void Update()
    {
        Debug.DrawRay(transform.position, Vector3.down * 100f, Color.red);

		/* CONSTANT LASER - OUTDATED IMPLEMENTATION
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 100f))
        {
            e_Item_Epoxy_Block epoxy_block = hit.collider.GetComponent<e_Item_Epoxy_Block>();
            if (epoxy_block != null)
            {
                Debug.Log("Thing happening...");
                Debug.Log($"textureCoord: {hit.textureCoord}, triangleIndex: {hit.triangleIndex}");
                int x = Mathf.FloorToInt(hit.textureCoord.x * epoxy_block.Atlas.width);
                int y = Mathf.FloorToInt(hit.textureCoord.y * epoxy_block.Atlas.height);
                epoxy_block.PaintAt(x, y, intensity * Time.deltaTime);
            }
        }
		*/
    }

	[ContextMenu("Try to Apply Job")]
	void TryApplyJob()
	{
		if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 100f))
			return;

		e_Item_Epoxy_Block block = hit.collider.GetComponent<e_Item_Epoxy_Block>();
		if (block == null)
			return;

		int originX = Mathf.FloorToInt(hit.textureCoord.x * block.Atlas.width);
		int originY = Mathf.FloorToInt(hit.textureCoord.y * block.Atlas.height);

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
		if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 100f))
		{
			e_Item_Epoxy_Block epoxy_block = hit.collider.GetComponent<e_Item_Epoxy_Block>();
			if (epoxy_block != null)
			{
				Debug.Log("Testing epoxy shape print...");
				Debug.Log($"textureCoord: {hit.textureCoord}, triangleIndex: {hit.triangleIndex}");
				int x = Mathf.FloorToInt(hit.textureCoord.x * epoxy_block.Atlas.width);
				int y = Mathf.FloorToInt(hit.textureCoord.y * epoxy_block.Atlas.height);
				epoxy_block.PaintShape(testShape, x, y, intensity);
			}
			else
			{
				Debug.LogWarning("Tried to print shape but didn't hit an engravable object.");
			}
		}
	}
}