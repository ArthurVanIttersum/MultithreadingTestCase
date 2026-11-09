using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "timelineSettings", menuName = "Scriptable Objects/timelineSettings")]
public class TimelineSettings : ScriptableObject
{
    public List<Frame> frameData;
}

[System.Serializable]
public class Frame
{
    public float timePerFrame;

}