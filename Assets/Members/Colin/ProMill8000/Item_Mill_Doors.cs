using UnityEngine;
using ProMill8000;

public class Item_Mill_Doors : Item_Parent
{
	[SerializeField] private AxisMovement leftDoor;
	[SerializeField] private AxisMovement rightDoor;
	[SerializeField] private float slideDistance = 0.5f;

	private bool _isOpen;

	public Item_Mill_Doors()
	{
		Name = "Mill Doors";
		Pickup = false;
		Quantity = 1;
	}

	public override void AlternateInteract(Entity_Player PlayerReference)
	{
		_isOpen = !_isOpen;

		if (_isOpen)
		{
			leftDoor.MoveToOffset(-slideDistance);
			rightDoor.MoveToOffset(slideDistance);
		}
		else
		{
			leftDoor.MoveToOffset(0f);
			rightDoor.MoveToOffset(0f);
		}
	}
}
