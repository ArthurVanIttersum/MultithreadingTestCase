using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[CreateAssetMenu(fileName = "ReworkedHoneyGenerationSettings", menuName = "Scriptable Objects/ReworkedHoneyGenerationSettings")]
public class ReworkedHoneyGenerationSettings : ScriptableObject
{
    public CoinData smallCoinData;
    public CoinData bigCoinData;
    public List<CoinData> grassAndStuff;//objects ignored by pathfinding
    public List<ReworkedHoneySpawnedObject> objects;
}

[System.Serializable]
public class ReworkedHoneySpawnedObject
{
    public GameObject prefab;
    public int minSpawnCount;
    public int maxSpawnCount;
    public List<IncreaseSpawnChance> extraObjects;

    
}

[System.Serializable]
public struct ExtraObjects
{
    public int indexInList;
    public int spawnChance;
}

[System.Serializable]
public class CoinData
{
    public GameObject prefab;
    public int minSpawnCount;
    public int maxSpawnCount;
}