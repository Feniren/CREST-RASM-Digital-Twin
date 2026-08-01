using System.Collections;
using UnityEngine;

public class Laser_Head : MonoBehaviour
{
	public float intensity = 100f;
	public EngraveMask ActiveMask;

	public float atlasPixelsPerInch = 100f;

	[Tooltip("Only layers containing engraving proxy colliders.")]
	public LayerMask engravingLayerMask;

	[Tooltip("Reveal brush radius in UV space (0-1).")]
	public float revealRadiusUV = 0.02f;

	[Tooltip("Brush edge softness.")]
	[Range(0f, 1f)]
	public float revealHardness = 0.8f;

	[Header("Movement Settings")]
	public float moveSpeed = 2f;

	[Tooltip("How far the laser head moves down on the Z axis per pass.")]
	public float stepSize = 0.005f;

	private Coroutine _engraveCoroutine;

	private void Update()
	{
		Debug.DrawRay(transform.position, Vector3.down * 100f, Color.red);
	}

	private bool TryGetEngravable(out EngravableSurface surface, out RaycastHit hit)
	{
		surface = null;

		if (!Physics.Raycast(transform.position, Vector3.down, out hit, 100f, engravingLayerMask, QueryTriggerInteraction.Collide))
		{
			Debug.Log($"[Laser_Head] Raycast from {transform.position} hit nothing on layer mask {engravingLayerMask.value}.", this);
			return false;
		}

		Debug.Log($"[Laser_Head] Raycast hit '{hit.collider.name}' at {hit.point}, uv={hit.textureCoord}.", this);

		EngraveDetector detector = hit.collider.GetComponent<EngraveDetector>();

		if (detector == null)
		{
			Debug.LogWarning($"[Laser_Head] Hit collider '{hit.collider.name}' has no EngraveDetector component.", this);
			return false;
		}

		surface = detector.Owner;

		if (surface == null)
			Debug.LogWarning($"[Laser_Head] EngraveDetector on '{hit.collider.name}' has no owning EngravableSurface configured.", this);

		return surface != null;
	}

	private void UpdateEngraving()
	{
		if (TryGetEngravable(out EngravableSurface surface, out RaycastHit hit))
		{
			Debug.Log($"[Laser_Head] Painting reveal on '{surface.name}' at uv={hit.textureCoord}, radius={revealRadiusUV}.", this);
			surface.PaintReveal(hit.textureCoord, revealRadiusUV, revealHardness);
		}
	}

	[ContextMenu("Try to Apply Job")]
	public void TryApplyJob()
	{
		if (ActiveMask == null)
		{
			Debug.LogWarning("[Laser_Head] TryApplyJob aborted: no ActiveMask assigned.", this);
			return;
		}

		if (!TryGetEngravable(out EngravableSurface surface, out RaycastHit hit))
		{
			Debug.LogWarning("[Laser_Head] TryApplyJob aborted: no engravable surface found below the laser head.", this);
			return;
		}

		int originX = Mathf.FloorToInt(hit.textureCoord.x * surface.Atlas.width);

		int originY = Mathf.FloorToInt(hit.textureCoord.y * surface.Atlas.height);

		Debug.Log($"[Laser_Head] Applying job at atlas pixel ({originX}, {originY}).", this);

		surface.ClearRevealMask();

		Texture2D mask = ActiveMask.GetResampledMask(atlasPixelsPerInch);

		if (mask == null)
		{
			Debug.LogError("[Laser_Head] TryApplyJob aborted: ActiveMask.GetResampledMask returned null.", this);
			return;
		}

		surface.PaintShape(mask, originX, originY, intensity);

		if (_engraveCoroutine != null) StopCoroutine(_engraveCoroutine);

		Debug.Log($"[Laser_Head] Starting raster scan movement over bounds {hit.collider.bounds}.", this);

		_engraveCoroutine = StartCoroutine(RasterScanMovement(hit.collider.bounds));
	}

	public void LoadMask(EngraveMask mask)
	{
		Debug.Log($"[Laser_Head] LoadMask called.", this);

		ActiveMask = mask;

		if (_engraveCoroutine != null)
		{
			StopCoroutine(_engraveCoroutine);
			_engraveCoroutine = null;
		}
	}

	private IEnumerator RasterScanMovement(Bounds targetBounds)
	{
		Debug.Log($"[Laser_Head] RasterScanMovement started. Bounds={targetBounds}, moveSpeed={moveSpeed}, stepSize={stepSize}.", this);

		float minX = targetBounds.min.x;
		float maxX = targetBounds.max.x;

		float currentZ = targetBounds.max.z;
		float minZ = targetBounds.min.z;

		Vector3 startCorner = new Vector3(minX, transform.position.y, currentZ);

		while (Vector3.Distance(transform.position, startCorner) > 0.001f)
		{
			transform.position = Vector3.MoveTowards(transform.position, startCorner, moveSpeed *
													 Time.deltaTime);
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
			movingRight = !movingRight;

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
