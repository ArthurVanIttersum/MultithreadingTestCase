using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ObjectInfo", menuName = "Scriptable Objects/ObjectInfo")]
public class ObjectInformation : ScriptableObject
{
    public List<ObjectInfo> objects;
}

[System.Serializable]
public struct ObjectInfo
{
    public Kind kind;
    public float value;
    public AudioClip clip;
}

public enum Kind
{
    Add,
    Subtract
}
