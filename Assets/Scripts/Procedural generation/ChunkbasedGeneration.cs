using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class ChunkBasedGeneration : MonoBehaviour
{
    //settings Files
    [SerializeField] private BiomeSelection biomeSelection;
    public GenerationSettings generationSettings;
    public ChunkSettings chunkSettings;

    private void Awake()
    {
        if (biomeSelection == null)
        {
            Debug.LogWarning("generation settings not assigned");
        }
        else
        {
            ScriptableObject settingsFile = biomeSelection.Biome(0);
            if (settingsFile is GenerationSettings)
            {
                generationSettings = (GenerationSettings)settingsFile;
            }
            else
            {
                Debug.LogWarning("generation settings not assigned");
            }
        }
    }

    //private void Update()
    //{
    //    generationSettings = (GenerationSettings)biomeSelection.Biome(biomeSelection.randomBiomeChance);
    //}


    void GenerateChunk(GameObject chunk)
    {
        if (biomeSelection == null)
        {
            Debug.LogWarning("generation settings not assigned");
        }
        else
        {
            ScriptableObject settingsFile = biomeSelection.Biome(biomeSelection.randomBiomeChance);
            if (settingsFile is GenerationSettings)
            {
                generationSettings = (GenerationSettings)settingsFile;
            }
            else
            {
                Debug.LogWarning("generation settings not assigned");
            }
        }

        print("Generating Chunk");
        if (generationSettings == null) return;//quick test
        if (chunkSettings == null) return;//quick test
        
        print("pastChecks");

        //Add chunk to dictionary
        int chunkSize = chunkSettings.chunksize;
        
        
        //generate objects in chunk
        foreach (var item in generationSettings.objects)
        {
            for (int i = 0; i < item.objectCount; i++)
            {
                Vector3 randomOffset = new Vector3(
                    UnityEngine.Random.Range(-chunkSize/2, chunkSize/2),
                    0,
                    UnityEngine.Random.Range(-chunkSize / 2, chunkSize / 2)
                );
                Instantiate(item.prefab, randomOffset + chunk.transform.position, Quaternion.identity, chunk.transform);
            }
        }
    }


    void RemoveChunk(GameObject removed)
    {
        print("Removing Chunk");
        
        RemoveObjectHelper.RemoveObject(removed);
    }

    private void OnEnable()
    {
        
        ContinuousGeneration script = FindAnyObjectByType<ContinuousGeneration>();
        script.DeleteChunkAtPosition += RemoveChunk;
        script.GenerateAtPosition += GenerateChunk;
        Continuous2DGeneration script2 = FindAnyObjectByType<Continuous2DGeneration>();
        script2.DeleteChunkAtPosition += RemoveChunk;
        script2.GenerateAtPosition += GenerateChunk;
    }
    
}

