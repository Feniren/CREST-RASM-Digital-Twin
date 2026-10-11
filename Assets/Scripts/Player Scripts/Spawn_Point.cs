using UnityEngine;

public class Spawn_Point : MonoBehaviour{
	public GameObject PlayerPrefab;
	public int SpawnRadius;

	Spawn_Point(){
	}

	public virtual void Awake(){
		Instantiate(PlayerPrefab, gameObject.transform.position, gameObject.transform.rotation);
	}

	public virtual void Start(){
	}
}
