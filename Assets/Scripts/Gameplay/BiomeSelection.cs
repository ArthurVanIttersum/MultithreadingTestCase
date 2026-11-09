using System.Collections.Generic;
using UnityEngine;

public class BiomeSelection : MonoBehaviour
{
    [SerializeField] private List<ScriptableObject> biomes; //List of all biomes (Scriptable Objects)
    [SerializeField] private float secBetweenBiomeChange; //How often there is a chance to change the biome (ex. 2 -> every 2 seconds)
    [SerializeField, Range(0, 1)] private float chanceForBiomeChange; //What is the percentage for changing the biome (ex. 0.5 -> every 2 seconds there is 50% chance for changing the biome)
    private float timer;
    [HideInInspector] public int randomBiomeChance; //Index to indicate to which biome from the list to change to

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer > secBetweenBiomeChange)
        {
            timer = 0;
            
            //if the chance for changing the biome is higher that the value, change the biome
            if (chanceForBiomeChange >= Random.Range(0f, 1f))
            {
                //Select a random biome from the list
                randomBiomeChance = Random.Range(0, biomes.Count);

                Biome(randomBiomeChance);
                Debug.Log("ChangeBiome");
            }
        }
    }

    public ScriptableObject Biome(int random)
    {
        return biomes[random];
    }
}
