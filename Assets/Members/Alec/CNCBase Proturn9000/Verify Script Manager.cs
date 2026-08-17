using System;
using UnityEngine;
using UnityEngine.Video;

public class VerifyButton : MonoBehaviour
{
    private Boolean redrawFlag;
    [SerializeField] private VideoPlayer videoPlayer;

    void Start()
    {
        redrawFlag = false;
    }
    public void Verify()
    {
        // prepare
        // the verification start up steps, ignored till assets are made
        // start video
        videoPlayer.Play();
        redrawFlag = true;
        Debug.Log("verify");
    }

    public void Redraw()
    {
        if(redrawFlag) {
            videoPlayer.time = 0f;
            videoPlayer.Play();
        }
    }

    public void StopRedraw()
    {
        if(redrawFlag) {
            videoPlayer.Pause();
        }
    }

    public void Reset()
    {
        // stop video
        videoPlayer.time = 0f;
        redrawFlag = false;
    } 
}
