using System;
using UnityEngine;
using UnityEngine.Video;

public class VerifyButton : MonoBehaviour
{
    private Boolean redrawFlag;
    [SerializeField] private VideoPlayer verifyPlayer;
    [SerializeField] private VideoPlayer scriptPlayer;

    void Start()
    {
        redrawFlag = false;
    }
    public void Verify()
    {
        // prepare
        // the verification start up steps, ignored till assets are made
        // start video
        verifyPlayer.Play();
        scriptPlayer.Play();
        redrawFlag = true;
        Debug.Log("verify");
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
        scriptPlayer.time = 0f;
        redrawFlag = false;
    } 
}
