using System.Collections;
using UnityEngine;

public class Item_Mill : Item_Parent{
	public Item_Slotted_Table ActiveTable;
	public Transform ProcessPoint;
	public Material ProcessingMaterial;
	public float ProcessingTime = 3f;

	private Material _originalMaterial;
	private MeshRenderer _renderer;
	private bool _isProcessing;

	public bool IsProcessing => _isProcessing;

	public Item_Mill(){
		Name = "Mill";
		Pickup = false;
		Quantity = 1;
	}

	public override void Start(){
		base.Start();
		_renderer = GetComponent<MeshRenderer>();
		if (_renderer != null)
			_originalMaterial = _renderer.sharedMaterial;
	}

	public void ProcessItem(Item_Slotted_Table table, Job_Queue queue, Spline_Animate spline){
		if (_isProcessing) return;
		StartCoroutine(ProcessCoroutine(table, queue, spline));
	}

	private IEnumerator ProcessCoroutine(Item_Slotted_Table table, Job_Queue queue, Spline_Animate spline){
		_isProcessing = true;

		GameObject item = table.Item;

		// No item — skip processing, keep job, resume belt
		if (item == null){
			spline.Play();
			_isProcessing = false;
			yield break;
		}

		// Take item from table into mill
		table.Item = null;
		if (ProcessPoint != null){
			item.transform.SetParent(ProcessPoint, false);
			item.transform.localPosition = Vector3.zero;
		}

		// Visual change on mill
		if (ProcessingMaterial != null && _renderer != null)
			_renderer.material = ProcessingMaterial;

		yield return new WaitForSeconds(ProcessingTime);

		// Return item to table
		if (table.AnchorPoint != null){
			item.transform.SetParent(table.AnchorPoint.transform, false);
			item.transform.localPosition = Vector3.zero;
			table.Item = item;
		}

		// Restore material
		if (_originalMaterial != null && _renderer != null)
			_renderer.material = _originalMaterial;

		// Remove job and resume belt
		queue.jobPop();
		spline.Play();

		_isProcessing = false;
	}

	public override void Interact(Entity_Player PlayerReference){
		base.Interact(PlayerReference);
	}

	public override void AlternateInteract(Entity_Player PlayerReference){
	}
}
