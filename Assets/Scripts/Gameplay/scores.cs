using NUnit.Framework;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "scores", menuName = "Scriptable Objects/scores")]
public class scores : ScriptableObject
{
    public List<int> scoresList = new();
}
