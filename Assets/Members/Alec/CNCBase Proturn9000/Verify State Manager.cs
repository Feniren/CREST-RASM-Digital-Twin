using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class VerifyStateManager : MonoBehaviour
{
    private Boolean redrawFlag;

    // video players, will go unutilized until full implementation is brought into scope
    [SerializeField] private VideoPlayer verifyPlayer; 

    /* 
        button lists, unless there's a better way to do this, these buttons' active status will be toggled
        following the state of the verify menu
        holds both the canvas button objects and the sprite objects, lists are guaranteed to be unordered
    */
    [Header("Alert UI")]
    [SerializeField] private List<GameObject> verifyAlert;
    [SerializeField] private List<GameObject> requestStopAlert;
    [SerializeField] private List<GameObject> normalStopAlert;
    [Header("Normal Verify UI")]
    [SerializeField] private List<GameObject> baseButtons;
    [SerializeField] private List<GameObject> startButtons;
    [SerializeField] private List<GameObject> stopButtons;
    [SerializeField] private List<GameObject> pauseButtons;
    [Header("Redraw UI")]
    [SerializeField] private List<GameObject> redrawWindowButtons;
   
    
    // [SerializeField] private VideoPlayer rePlayer;

    void Start()
    {
        redrawFlag = false;
        // go through all start buttons and ensure they're active
    }

    public void StartSequence() // called by verify button ONLY
    {
        //verify prgm alert becomes active
        //button attached will call verify() on click()
    }

    public void VerifyStartButton()
    {
        StartCoroutine(StartVerify());
    }
    IEnumerator StartVerify()
    {
        // prepare
        // the verification start up steps, ignored till assets are made
        // start video
        verifyPlayer.Play();
        yield return new WaitForSeconds(0.8f);
        verifyPlayer.Pause();
        Debug.Log("verify prepared");
        // set all start verify sim buttons active
    }

    public void Verify() // to be called by start verify sim buttons
    {
        verifyPlayer.Play();
        redrawFlag = true;
    }

    public void VerifyCancel()
    {
        
    }

    public void VerifyPause()
    {
        
    }

    public void Redraw()
    {
        if(redrawFlag) {
            verifyPlayer.time = 0f;
            verifyPlayer.Play();
        }
    }

    public void StopRedraw()
    {
        if(redrawFlag) {
            verifyPlayer.Pause();
        }
    }

    public void Reset()
    {
        // stop video
        verifyPlayer.time = 0f;
        // scriptPlayer.time = 0f;
        redrawFlag = false;
    } 
}
