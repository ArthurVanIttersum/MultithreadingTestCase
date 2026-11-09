using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;
using System.Collections.Generic;


public class VideoController : MonoBehaviour
{
    public UnityEvent startEvent;
    public VideoPlayer videoPlayer;
    public UnityEvent endEvent;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        float time = (float)videoPlayer.clip.length;
        StartCoroutine(StartVideo(time));
    }

    private IEnumerator StartVideo(float time)
    {

        startEvent.Invoke();

        yield return new WaitForSeconds(time);

        endEvent.Invoke();

    }
}
