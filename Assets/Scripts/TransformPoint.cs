using UnityEngine;

[System.Serializable]
public class TransformPoint{
	public TransformPoint(){
		Location = Vector3.zero;
		Rotation = Quaternion.identity;
		Scale = Vector3.one;
	}

	public Vector3 GetForwardVector(){
		return (Rotation * Vector3.forward);
	}

	public void Rotate(Vector3 EulerRotation){
		Rotation *= Quaternion.Euler(EulerRotation);
	}

	public Vector3 Location;
	public Quaternion Rotation;
	public Vector3 Scale;
}
