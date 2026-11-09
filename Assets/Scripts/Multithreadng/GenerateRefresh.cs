using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class GenerationRefresh : MonoBehaviour
{
    //important gameobjects
    public GameObject player;
    public GameObject prefabEmptyGameObject;

    //settings files
    public ChunkSettings chunkSettings;
    [SerializeField] private float maxFrameTime = 0.5f; // 3 ms budget

    //private valiables
    private Vector2Int lastPosition;
    private Vector2Int currentPosition;
    private HashSet<Vector2Int> chunksInAABB = new();
    private Dictionary<Vector2Int, GameObject> generatedChunks = new();
    private List<Vector2Int> chunksToRemove = new();

    // pending generation structures
    private readonly Queue<Vector2Int> pendingQueue = new();
    private readonly HashSet<Vector2Int> pendingSet = new();
    private bool generationCoroutineRunning = false;

    //events
    public event Action<GameObject> GenerateAtPosition;
    public event Action<GameObject> DeleteChunkAtPosition;
    public bool generateOnStart = true;
    public bool continuousRegeneration = true;
    public ThreadableHoneyGeneration script;

    private void Start()
    {
        if (player == null) return;//quick test
        if (chunkSettings == null) return;//quick test

        Vector2Int roundedPlayerPosition = GetPlayerChunkPosition();
        currentPosition = roundedPlayerPosition;
        lastPosition = roundedPlayerPosition;

        //generate a few chunks
        UpdateAABBList();
        UpdateChunkGeneration();
    }
    private void Update()
    {
        UpdatePosition();
        //if (currentPosition == lastPosition) return;
        UpdateAABBList();
        UpdateChunkGeneration();
        lastPosition = currentPosition;
        
    }

    void UpdatePosition()
    {
        if (player == null) return;//quick test

        //calculate current chunk
        Vector2Int roundedPlayerPosition = GetPlayerChunkPosition();
        currentPosition = roundedPlayerPosition;
    }

    void UpdateChunkGeneration()
    {
        
        if (chunkSettings == null) return;//quick test


        //eternal sabotage
        bool good;
        if (script)
        {
            good = script.generationRequestList.Count == 0;
        }
        else
        {
            good = true;
        }
        
        if (pendingQueue.Count == 0 && generatedChunks.Count > 0 && good && continuousRegeneration)
        {
            // copy keys to avoid modifying collection while iterating
            var allKeys = new List<Vector2Int>(generatedChunks.Keys);

            foreach (var key in allKeys)
            {
                generatedChunks.Remove(key, out GameObject removed);
                DeleteChunkAtPosition?.Invoke(removed);
            }
            print("sabotaging");
        }


        //delete bad chunks
        chunksToRemove.Clear();
        foreach (var key in generatedChunks.Keys)
        {
            if (chunksInAABB.Contains(key)) continue;
            chunksToRemove.Add(key);
        }
        

        foreach (var key in chunksToRemove)
        {
            generatedChunks.Remove(key, out GameObject removed);

            // if it was pending, remove from pendingSet so coroutine will skip it
            if (pendingSet.Contains(key))
                pendingSet.Remove(key);

            DeleteChunkAtPosition?.Invoke(removed);
        }

        

        //enqueue missing chunks (reserve slot with null)
        foreach (var item in chunksInAABB)
        {
            if (generatedChunks.ContainsKey(item)) continue; // already generated or reserved

            // reserve the slot with null so we don't enqueue duplicates
            generatedChunks.Add(item, null);

            // enqueue for generation if not already pending
            if (!pendingSet.Contains(item))
            {
                pendingSet.Add(item);
                pendingQueue.Enqueue(item);
            }
        }
        

        // start the single coroutine if not already running
        if (!generationCoroutineRunning && pendingQueue.Count > 0)
        {
            StartCoroutine(GenerateChunksCoroutine());

        }

    }

    private IEnumerator GenerateChunksCoroutine()
    {
        generationCoroutineRunning = true;
        int maxActiveJobs = SystemInfo.processorCount - 4;
        if (maxActiveJobs < 4) maxActiveJobs = 4;
        
        print(maxActiveJobs);
        while (pendingQueue.Count > 0)
        {
            float frameStart = Time.realtimeSinceStartup;
            int processedThisFrame = 0;

            // process as many queued positions as possible within the time budget
            while (pendingQueue.Count > 0)
            {
                Vector2Int pos = pendingQueue.Dequeue();

                // If it was removed while waiting, skip it
                if (!pendingSet.Remove(pos)) continue;
                if (!generatedChunks.ContainsKey(pos)) continue;

                GameObject chunk = MakeChunkGameobject(pos);
                generatedChunks[pos] = chunk;

                if (generateOnStart) GenerateAtPosition?.Invoke(chunk);

                processedThisFrame++;

                // check elapsed time and break if over budget
                float elapsed = Time.realtimeSinceStartup - frameStart;
                if (elapsed >= maxFrameTime) break;
                
                bool end;
                if (script) end = script.generationRequestList.Count >= maxActiveJobs;
                else end = false;
                
                if (end) break;
            }
            if (pendingQueue.Count > 0) yield return null;
            else break;
        }
        generationCoroutineRunning = false;
        generateOnStart = true;
    }


    private GameObject MakeChunkGameobject(Vector2Int pos)
    {
        
        return Instantiate(prefabEmptyGameObject, Vector3.zero + new Vector3(pos.x, 0, pos.y), Quaternion.identity, transform);
    }

    private void UpdateAABBList()
    {
        if (chunkSettings == null) return;//quick test

        //clear
        chunksInAABB.Clear();

        //chunkArea
        Vector2Int chunkArea = chunkSettings.chunkGenerationAreaInChunks;
        Vector2Int centerOffset = new Vector2Int(chunkArea.x / 2, chunkArea.y / 2);
        int chunksize = chunkSettings.chunksize;

        //corners
        Vector2Int centerPosition = currentPosition + chunkSettings.playerOffsetFromCenterInChunks * chunksize;
        Vector2Int maxPos = centerPosition + (centerOffset + Vector2Int.one) * chunksize;
        Vector2Int minPos = centerPosition - centerOffset * chunksize;

        

        //interpolate to generate and delete chunks
        for (int x = minPos.x; x < maxPos.x; x += chunksize)
        {
            for (int y = minPos.y; y < maxPos.y; y += chunksize)
            {
                chunksInAABB.Add(new Vector2Int(x, y));
            }
        }
    }

    private Vector2Int GetPlayerChunkPosition()
    {
        if (chunkSettings == null) print("warning: player not diffined");//quick test

        //values
        Vector3 position = player.transform.position;
        Vector2Int chunkArea =  chunkSettings.chunkGenerationAreaInChunks;
        int chunkSize = chunkSettings.chunksize;

        //calculation
        int roundedPlayerPositionx = (Mathf.FloorToInt(position.x) / chunkSize) * chunkSize;
        int roundedPlayerPositionz = (Mathf.FloorToInt(position.z) / chunkSize) * chunkSize;
        Vector2Int roundedPosition = new Vector2Int(roundedPlayerPositionx, roundedPlayerPositionz);

        //return
        return roundedPosition;
    }
}
