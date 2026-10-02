using UnityEngine;
using UnityEngine.SceneManagement;

public class Player_Reference : MonoBehaviour{
	public static GameObject Instance;
	
	static Entity_Player PlayerReference;

	public Player_Reference(){
	}

	public void Awake(){
		SetLocalPlayer();
	}

	public void OnDestroy(){
		if (Instance == gameObject){
			Instance = null;
		}
	}

	public void OnDisable(){
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	public void OnEnable(){
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	public void OnSceneLoaded(Scene scene, LoadSceneMode mode){
		if (Instance == null){
			SetLocalPlayer();
		}
	}

	public static Entity_Player GetLocalPlayer(){
		return PlayerReference;
	}

	void SetLocalPlayer(){
		if ((Instance != null) && (Instance != gameObject)){
			Debug.LogWarning("Reference not set to local player");

			return;
		}

		Instance = gameObject;
		PlayerReference = Instance.GetComponent<Entity_Player>();
	}
}
