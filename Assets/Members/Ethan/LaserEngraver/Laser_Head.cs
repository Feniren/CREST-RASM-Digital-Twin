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

	[Tooltip("Largest UV gap that is still treated as one continuous stroke. " +
			 "Larger jumps start a new stroke instead of drawing a streak across the surface.")]
	public float maxStrokeGapUV = 0.25f;

	[Tooltip("Logs every raycast and reveal. Very noisy during an engrave job.")]
	public bool verboseLogging = false;

	private Coroutine _engraveCoroutine;

	// Where the beam was when the reveal mask was last painted.
	private EngravableSurface _strokeSurface;
	private Vector2 _strokeUV;
	private bool _strokeActive;

	private void Update()
	{
		Debug.DrawRay(transform.position, Vector3.down * 100f, Color.red);
	}

	private bool TryGetEngravable(out EngravableSurface surface, out RaycastHit hit)
	{
		surface = null;

		if (!Physics.Raycast(transform.position, Vector3.down, out hit, 100f, engravingLayerMask, QueryTriggerInteraction.Collide))
		{
			if (verboseLogging)
				Debug.Log($"[Laser_Head] Raycast from {transform.position} hit nothing on layer mask {engravingLayerMask.value}.", this);

			return false;
		}

		if (verboseLogging)
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

	/// <summary>
	/// Reveals the span the beam covered since the previous call.
	/// </summary>
	private void UpdateEngraving()
	{
		if (!TryGetEngravable(out EngravableSurface surface, out RaycastHit hit))
		{
			EndStroke();
			return;
		}

		Vector2 uv = hit.textureCoord;

		bool continuous = _strokeActive && surface == _strokeSurface &&
						  Vector2.Distance(_strokeUV, uv) <= maxStrokeGapUV;

		if (verboseLogging)
			Debug.Log($"[Laser_Head] Painting reveal on '{surface.name}' at uv={uv}, radius={revealRadiusUV}.", this);

		surface.PaintRevealStroke(continuous ? _strokeUV : uv, uv, revealRadiusUV, revealHardness);

		_strokeSurface = surface;
		_strokeUV = uv;
		_strokeActive = true;
	}

	/// <summary>
	/// Breaks stroke continuity so the next reveal does not connect back to the
	/// last one. Called whenever the beam is off, such as between raster passes.
	/// </summary>
	private void EndStroke()
	{
		_strokeActive = false;
		_strokeSurface = null;
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

		EndStroke();

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

		EndStroke();
	}

	private IEnumerator RasterScanMovement(Bounds targetBounds)
	{
		Debug.Log($"[Laser_Head] RasterScanMovement started. Bounds={targetBounds}, moveSpeed={moveSpeed}, stepSize={stepSize}.", this);

		float minX = targetBounds.min.x;
		float maxX = targetBounds.max.x;

		float currentZ = targetBounds.max.z;
		float minZ = targetBounds.min.z;

		Vector3 startCorner = new Vector3(minX, transform.position.y, currentZ);

		EndStroke();

		yield return MoveTo(startCorner, engrave: false);

		bool movingRight = true;

		while (currentZ >= minZ)
		{
			float targetX = movingRight ? maxX : minX;

			Vector3 passTarget = new Vector3(targetX, transform.position.y, currentZ);

			yield return MoveTo(passTarget, engrave: true);

			currentZ -= stepSize;
			movingRight = !movingRight;

			// The beam is off while stepping down, so the next pass starts fresh.
			EndStroke();

			if (currentZ >= minZ)
			{
				Vector3 stepDownTarget = new Vector3(transform.position.x, transform.position.y, currentZ);

				yield return MoveTo(stepDownTarget, engrave: false);
			}
		}

		Debug.Log("Laser Engraving Finished.");
		_engraveCoroutine = null;
	}

	/// <summary>
	/// Moves to a target over the exact time the distance takes at moveSpeed.
	/// Interpolating against accumulated elapsed time keeps the head on the same
	/// path at any frame rate, with no per-frame rounding drift and no chance of
	/// stalling near the target.
	/// </summary>
	private IEnumerator MoveTo(Vector3 target, bool engrave)
	{
		Vector3 start = transform.position;

		float distance = Vector3.Distance(start, target);

		float duration = distance / Mathf.Max(moveSpeed, 0.0001f);

		if (duration <= 0f)
		{
			transform.position = target;

			if (engrave) UpdateEngraving();

			yield break;
		}

		float elapsed = 0f;

		while (elapsed < duration)
		{
			elapsed = Mathf.Min(elapsed + Time.deltaTime, duration);

			transform.position = Vector3.Lerp(start, target, elapsed / duration);

			if (engrave) UpdateEngraving();

			yield return null;
		}
	}
}
