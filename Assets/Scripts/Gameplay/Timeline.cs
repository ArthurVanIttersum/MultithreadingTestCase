using JetBrains.Annotations;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class Timeline : MonoBehaviour
{
    [SerializeField] private PlayableDirector director;
    [SerializeField] private bool startOnPlay;
    private Coroutine controller;
    [SerializeField] private TimelineSettings settings;
    void Start()
    {
        if (!startOnPlay) return;
        
        StartAnimation();
    }

    [ContextMenu("StartAnimation")]
    private void StartAnimation()
    {
        if (controller != null) return;
        controller = StartCoroutine(ControllAnimation());
    }

    private IEnumerator ControllAnimation()
    {
        director.Play();
        foreach (var frame in settings.frameData)
        {
            yield return new WaitForSeconds(frame.timePerFrame);
            director.Pause();
            yield return new WaitUntil(Condition);
            director.Resume();
        }
        controller = null;
    }


    private bool Condition()
    {
        
        return Input.anyKey;
    }
    
}


