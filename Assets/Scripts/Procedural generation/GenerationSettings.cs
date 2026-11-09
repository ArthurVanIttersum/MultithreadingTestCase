using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GenerationSettings", menuName = "Scriptable Objects/GenerationSettings")]
public class GenerationSettings : ScriptableObject
{
    public List<RandomSpawnedObject> objects;
}

[System.Serializable]
public struct RandomSpawnedObject
{
    public GameObject prefab;
    public int objectCount;
    public Type type;
    public float value;
}

public enum Type
{
    Damage,
    Health
}