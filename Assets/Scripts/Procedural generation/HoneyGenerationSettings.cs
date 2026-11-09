using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HoneyGenerationSettings", menuName = "Scriptable Objects/HoneyGenerationSettings")]
public class HoneyGenerationSettings : ScriptableObject
{
    public List<HoneySpawnedObject> objects;
}

[System.Serializable]
public class HoneySpawnedObject
{
    public GameObject prefab;
    public int ambientSpawnChance;
    public List<IncreaseSpawnChance> clusteredObjects;

    public int GetSpawnChance()
    {
        int chance = ambientSpawnChance;
        foreach (var item in clusteredObjects)
        {
            chance += item.addedSpawnChance;
        }



        return chance;
    }
}

[System.Serializable]
public struct IncreaseSpawnChance
{
    public int indexInList;
    public int addedSpawnChance;
}