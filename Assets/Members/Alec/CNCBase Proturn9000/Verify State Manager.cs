using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class VerifyStateManager : MonoBehaviour
{
    private Boolean redrawFlag;
    [SerializeField] private VideoPlayer verifyPlayer;
    // [SerializeField] private VideoPlayer rePlayer;

    void Start()
    {
        redrawFlag = false;
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
