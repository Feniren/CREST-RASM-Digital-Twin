using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Module_Loader : MonoBehaviour{
    [SerializeField] private Screen_Fader Fader;
    [SerializeField] private GameObject MenuRoot;

    public event Action<Scene> Module_Loaded;
    public event Action Module_Unloaded;

    public string Active_Module { get; private set; }

    public void Load_Module(string sceneName){
        if (Active_Module == null)
            StartCoroutine(LoadRoutine(sceneName));
    }

    public void Return_To_Menu(){
        if (Active_Module != null)
            StartCoroutine(UnloadRoutine());
    }

    private IEnumerator LoadRoutine(string sceneName){
        yield return Fader.Fade_Out();
        MenuRoot.SetActive(false);
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        Scene scene = SceneManager.GetSceneByName(sceneName);
        SceneManager.SetActiveScene(scene);
        Active_Module = sceneName;
        Module_Loaded?.Invoke(scene);

        yield return Fader.Fade_In();
    }

    private IEnumerator UnloadRoutine(){
        yield return Fader.Fade_Out();
        yield return SceneManager.UnloadSceneAsync(Active_Module);

        SceneManager.SetActiveScene(gameObject.scene);
        Active_Module = null;
        MenuRoot.SetActive(true);
        Module_Unloaded?.Invoke();

        yield return Fader.Fade_In();
    }
}
