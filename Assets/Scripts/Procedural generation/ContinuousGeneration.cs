using System;
using System.Collections.Generic;
using UnityEngine;

public class ContinuousGeneration : MonoBehaviour
{
    //important gameobjects
    public GameObject player;
    public GameObject prefabEmptyGameObject;

    //settings files
    public ChunkSettings chunkSettings;

    //private valiables
    private int lastPosition;
    private int currentPosition;
    private Dictionary<int, GameObject> generatedChunks = new();

    //events
    public event Action<GameObject> GenerateAtPosition;
    public event Action<GameObject> DeleteChunkAtPosition;

    private void Start()
    {
        //calculate current chunk
        Vector3 playerPosition = player.transform.position;
        int chunkSize = chunkSettings.chunksize;
        int roundedPlayerPosition = (Mathf.FloorToInt(playerPosition.z) / chunkSize) * chunkSize;
        lastPosition = roundedPlayerPosition;
        currentPosition = roundedPlayerPosition;

        //generate a few chunks ahead
        if (chunkSettings == null) return;//quick test

        //interpolate to generate and delete chunks
        for (int i = currentPosition; i < chunkSettings.chunkGenerationDistanceInChunks * chunkSize; i += chunkSize)
        {
            print("Updating Chunk Generation");
            
            int zPos1 = i;

            GameObject chunk = MakeChunkGameobject(zPos1);
            generatedChunks.Add(zPos1, chunk);
            GenerateAtPosition.Invoke(chunk);

            GenerateAtPosition.Invoke(chunk);
        }
    }
    private void Update()
    {
        UpdatePosition();
        UpdateChunkGeneration();
        lastPosition = currentPosition;
    }

    void UpdatePosition()
    {
        if (player == null) return;//quick test

        //calculate current chunk
        Vector3 playerPosition = player.transform.position;
        int chunkSize = chunkSettings.chunksize;
        int roundedPlayerPosition = (Mathf.FloorToInt(playerPosition.z) / chunkSize) * chunkSize;
        currentPosition = roundedPlayerPosition;
        
    }

    void UpdateChunkGeneration()
    {
        if (chunkSettings == null) return;//quick test

        //interpolate to generate and delete chunks
        for (int i = lastPosition; i < currentPosition; i += chunkSettings.chunksize)
        {
            print("Updating Chunk Generation");
            int zPos1 = i + chunkSettings.chunkGenerationDistanceInChunks * chunkSettings.chunksize;
            int zPos2 = i - chunkSettings.chunkDeletionDistanceInChunks * chunkSettings.chunksize;
            if (!generatedChunks.ContainsKey(zPos1))//quick test
            {
                GameObject chunk = MakeChunkGameobject(zPos1);
                generatedChunks.Add(zPos1, chunk);
                GenerateAtPosition.Invoke(chunk);
            }
            if (generatedChunks.ContainsKey(zPos1))
            {
                generatedChunks.Remove(zPos2, out GameObject removed);
                DeleteChunkAtPosition.Invoke(removed);
            }
        }
    }

    private GameObject MakeChunkGameobject(int zPos)
    {
        //Create an empty gameobject representing a chunk.
        //store it in the dictionary instead of storing individual objects.
        GameObject chunk = Instantiate(prefabEmptyGameObject, Vector3.zero + Vector3.forward * zPos, Quaternion.identity, transform);
        chunk.name = "Chunk " + zPos.ToString();
        return chunk;
    }
}

