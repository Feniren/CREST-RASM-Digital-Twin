using UnityEngine;
using static UnityEditor.PlayerSettings;

public class LaserGantry : MonoBehaviour
{
	public enum Axis {  X, Y, Z };
	public Axis moveAxis = Axis.X;
	public float maxSpeed = 1.0f; // almost certainly inaccurate, need to examine machine
	public float rangeLower = 0f; // needs to be compared to machine size
	public float rangeUpper = 1.0f; // same as above

	public float _target;
	float _current;
	float _velocity;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
		switch (moveAxis)
		{
			case Axis.X: _current = transform.localPosition.x; break;
			case Axis.Y: _current = transform.localPosition.y; break;
			case Axis.Z: _current = transform.localPosition.z; break;
		}
    }

	public void SetTarget(float position)
	{
		_target = Mathf.Clamp(position, rangeLower, rangeUpper);
	}

    // Update is called once per frame
    void Update()
    {
		_current = Mathf.MoveTowards(_current, _target, maxSpeed * Time.deltaTime);

		Vector3 pos = transform.localPosition;
		switch (moveAxis)
		{
			case Axis.X: pos.x = _current; break;
			case Axis.Y: pos.y = _current; break;
			case Axis.Z: pos.z = _current; break;
		}
		transform.localPosition = pos;
	}
}
