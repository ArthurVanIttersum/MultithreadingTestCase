using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using static WorkerThread;

public class ThreadableHoneyGeneration : MonoBehaviour
{
    public ReworkedHoneyGenerationSettings generationSettings;
    public ChunkSettings chunkSettings;


    public Vector2Int areaMax;
    public Vector2Int areaMin;

    public List<GenerationRequest> generationRequestList = new();

    
    public NativeArray<SpawnObjectData> generationData;
    public NativeArray<CoinDataStruct> grassData;
    public NativeArray<CoinDataStruct> coinData;


    //special data for generation
    private NativeArray<Vector2> masterAvailableKeys;
    private NativeParallelHashMap<Vector2, int> revMasterAvailableKeys;

    public bool playerMoveAlongZaxis;
    [SerializeField] private float maxFrameTime = 0.5f; // 3 ms budget

    //caching
    private bool cached = false;
    private bool coroutineRunning = false;

    private HexGrid baseGrid;


    //Object settings
    [SerializeField] private float minSize;
    [SerializeField] private float maxSize;
    [SerializeField] private float minGrassSize = 0.5f;
    [SerializeField] private float maxGrassSize = 0.8f;

    void GenerateChunk(GameObject chunk)
    {
        if (generationSettings == null) return;//quick test
        if (chunkSettings == null) return;//quick test

        int chunkSize = chunkSettings.chunksize;
        Vector2Int centerPos = Vector2Int.zero;

        areaMax = (Vector2Int.one * (int)chunkSize / 2) + centerPos;
        areaMin = -(Vector2Int.one * (int)chunkSize / 2) + centerPos;


        if (!cached)
        {
            SetupDataStructures();
            print("datastructuresSetup");
            cached = true;
        }
        GenerationRequest request = new GenerationRequest(chunk);
        SetupThreading(request);

        generationRequestList.Add(request);



    }

    void Update()
    {
        if (coroutineRunning) return;
        StartCoroutine(CompleteJobsCoroutine());
        coroutineRunning = true;
    }

    private IEnumerator CompleteJobsCoroutine()
    {
        while (true)
        {
            float frameStart = Time.realtimeSinceStartup;
            for (int i = generationRequestList.Count - 1; i >= 0; i--)
            {
                GenerationRequest request = generationRequestList[i];
                if (request.handle.IsCompleted)
                {
                    request.handle.Complete();
                    
                    SpawnObjects(request);
                    request.Dispose();
                    generationRequestList.Remove(request);

                }
                float elapsed = Time.realtimeSinceStartup - frameStart;
                if (elapsed >= maxFrameTime)
                {
                    break;
                }
            }
            yield return null;
        }
    }


    private void SetupDataStructures()
    {
        Debug.Log("SetupDataStructures");
        baseGrid = new HexGrid();
        baseGrid.generationSettings = generationSettings;
        baseGrid.chunkSettings = chunkSettings;
        baseGrid.areaMin = areaMin;
        baseGrid.areaMax = areaMax;

        baseGrid.CacheData();
        baseGrid.MakeNativeStructures();
        

        GenerationStruct();
        masterAvailableKeys = new NativeArray<Vector2>(baseGrid.triToHex.Count, Allocator.Persistent);
        revMasterAvailableKeys = new NativeParallelHashMap<Vector2, int>(baseGrid.triToHex.Count, Allocator.Persistent);


        Vector2[] tempKeys = baseGrid.triToHex.Keys.ToArray();
        for (int i = 0; i < tempKeys.Length; i++)
        {
            masterAvailableKeys[i] = tempKeys[i];
            revMasterAvailableKeys[tempKeys[i]] = i;
        }

    }

    private void GenerationStruct()
    {
        //obstacle data

        List<SpawnObjectData> tempList = new();

        foreach (ReworkedHoneySpawnedObject objects in generationSettings.objects)
        {
            SpawnObjectData newData;
            newData = new SpawnObjectData();
            newData.MinSpawnCount = objects.minSpawnCount;
            newData.MaxSpawnCount = objects.maxSpawnCount;
            FixedList128Bytes<ExtraSpawnData> extraData = new();
            foreach (IncreaseSpawnChance extra in objects.extraObjects)
            {
                ExtraSpawnData newExtra = new ExtraSpawnData();
                newExtra.IndexInList = extra.indexInList;
                newExtra.AddedSpawnChance = extra.addedSpawnChance;
                extraData.Add(newExtra);
            }
            newData.Extras = extraData;
            tempList.Add(newData);
        }

        generationData = new NativeArray<SpawnObjectData>(tempList.Count, Allocator.Persistent);
        for (int i = 0; i < tempList.Count; i++)
        {
            generationData[i] = tempList[i];
        }

        //coin data

        coinData = new NativeArray<CoinDataStruct>(2, Allocator.Persistent);
        CoinDataStruct smallCoinData = new CoinDataStruct();
        smallCoinData.MinSpawnCount = generationSettings.smallCoinData.minSpawnCount;
        smallCoinData.MaxSpawnCount = generationSettings.smallCoinData.maxSpawnCount;
        coinData[0] = smallCoinData;
        CoinDataStruct bigCoinData = new CoinDataStruct();
        bigCoinData.MinSpawnCount = generationSettings.bigCoinData.minSpawnCount;
        bigCoinData.MaxSpawnCount = generationSettings.bigCoinData.maxSpawnCount;
        coinData[1] = bigCoinData;

        //grass data

        List<CoinDataStruct> tempList2 = new();

        foreach (var grass in generationSettings.grassAndStuff)
        {
            CoinDataStruct newData = new CoinDataStruct();
            newData.MinSpawnCount = grass.minSpawnCount;
            newData.MaxSpawnCount = grass.maxSpawnCount;
            tempList2.Add(newData);
        }

        grassData = new NativeArray<CoinDataStruct>(tempList2.Count, Allocator.Persistent);
        for (int i = 0; i < tempList2.Count; i++)
        {
            grassData[i] = tempList2[i];
        }
    }

    private void SetupThreading(GenerationRequest request)
    {
        int arrayLengthTriCount = baseGrid.triToHex.Count;
        int arrayLengthHexCount = baseGrid.hexToTri.Count;

        request.result = new NativeArray<int>(arrayLengthTriCount, Allocator.Persistent);
        for (int i = 0; i < arrayLengthTriCount; i++) request.result[i] = -1;

        request.grassResult = new NativeArray<int>(arrayLengthHexCount, Allocator.Persistent);
        for (int i = 0; i < arrayLengthHexCount; i++) request.grassResult[i] = -1;

        request.coinResult = new NativeArray<int>(arrayLengthTriCount, Allocator.Persistent);
        for (int i = 0; i < arrayLengthTriCount; i++) request.coinResult[i] = -1;

        request.coinNeighborCount = new NativeArray<int>(arrayLengthTriCount, Allocator.Persistent);
        for (int i = 0; i < arrayLengthTriCount; i++) request.coinNeighborCount[i] = 0;

        request.indices = new NativeArray<int>(arrayLengthTriCount, Allocator.Persistent);
        for (int i = 0; i < arrayLengthTriCount; i++) request.indices[i] = i;

        request.indices2 = new NativeArray<int>(arrayLengthHexCount, Allocator.Persistent);
        for (int i = 0; i < arrayLengthHexCount; i++) request.indices2[i] = i;


        int neighborlength = baseGrid.neighbors.Count;

        request.discovered = new NativeHashSet<Vector2>(neighborlength, Allocator.Persistent);
        request.obstructed = new NativeHashSet<Vector2>(neighborlength, Allocator.Persistent);
        request.toBeExplored = new NativeQueue<Vector2>(Allocator.Persistent);


        request.jobData.grassData = grassData;
        request.jobData.coinData = coinData;
        request.jobData.discovered = request.discovered;
        request.jobData.toBeExplored = request.toBeExplored;
        request.jobData.obstructed = request.obstructed;
        request.jobData.destinationPoints = baseGrid.NdestinationPoints;
        request.jobData.startingPoints = baseGrid.NstartingPoints;
        request.jobData.triToHex = baseGrid.NtriToHex;
        request.jobData.hexToTri = baseGrid.NhexToTri;
        request.jobData.triToTri = baseGrid.NtriToTri;
        request.jobData.neighbors = baseGrid.Neighbors;
        request.jobData.areaMin = areaMin;
        request.jobData.areaMax = areaMax;
        request.jobData.masterAvailableKeys = masterAvailableKeys;
        request.jobData.revMasterAvailableKeys = revMasterAvailableKeys;
        request.jobData.indices = request.indices;
        request.jobData.indices2 = request.indices2;
        request.jobData.generationData = generationData;
        request.jobData.random = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1, int.MaxValue));
        request.jobData.coinNeighborCount = request.coinNeighborCount;

        request.jobData.result = request.result;
        request.jobData.grassResult = request.grassResult;
        request.jobData.coinResult = request.coinResult;


        request.handle = request.jobData.Schedule();
    }

    private void SpawnObjects(GenerationRequest request)
    {
        SpawnObstacles(request);
        SpawnCoins(request);
        SpawnGrass(request);
    }
    private void SpawnObstacles(GenerationRequest request)
    {
        GameObject objects;
        int indexValue = 0;
        Vector2[] positions = baseGrid.triToHex.Keys.ToArray();
        Vector3 pos;

        for (int i = 0; i < positions.Length; i++)
        {
            indexValue = request.result[i];

            if (indexValue != -1)
            {
                pos = VectorConversion.vec2Tovec3(positions[i]) + request.chunk.transform.position;
                objects = Instantiate(generationSettings.objects[indexValue].prefab, pos, Quaternion.identity, request.chunk.transform);
                objects.transform.localScale *= UnityEngine.Random.Range(minSize, maxSize);
                objects.transform.rotation = Quaternion.AngleAxis(UnityEngine.Random.Range(0, 360), Vector3.up);
                //print("spawned object: " + objects.name + " Position: " + positions[i]);
            }
        }
    }

    private void SpawnCoins(GenerationRequest request)
    {
        GameObject objects;
        int indexValue = 0;
        Vector2[] positions = baseGrid.triToHex.Keys.ToArray();
        Vector3 pos;

        for (int i = 0; i < positions.Length; i++)
        {
            indexValue = request.coinResult[i];

            if (indexValue != -1)
            {
                pos = VectorConversion.vec2Tovec3(positions[i]) + request.chunk.transform.position;
                GameObject prefab = indexValue == 0 ? generationSettings.smallCoinData.prefab : generationSettings.bigCoinData.prefab;
                objects = Instantiate(prefab, pos, Quaternion.identity, request.chunk.transform);
                objects.transform.localScale *= UnityEngine.Random.Range(minSize, maxSize);
                objects.transform.rotation = Quaternion.AngleAxis(UnityEngine.Random.Range(0, 360), Vector3.up);
                //print("spawned object: " + objects.name + " Position: " + positions[i]);
            }
        }
    }

    private void SpawnGrass(GenerationRequest request)
    {
        GameObject objects;
        int indexValue = 0;
        Vector2[] positions = baseGrid.hexToTri.Keys.ToArray();
        Vector3 pos;

        
        for (int i = 0; i < positions.Length; i++)
        {
            indexValue = request.grassResult[i];
            if (indexValue != -1)
            {
                pos = VectorConversion.vec2Tovec3(positions[i]) + request.chunk.transform.position;
                objects = Instantiate(generationSettings.grassAndStuff[indexValue].prefab, pos, Quaternion.identity, request.chunk.transform);
                objects.transform.localScale *= UnityEngine.Random.Range(minGrassSize, maxGrassSize);
                //objects.transform.rotation = Quaternion.AngleAxis(UnityEngine.Random.Range(0, 360), Vector3.up);
                //print("spawned object: " + objects.name + " Position: " + positions[i]);
            }
        }
    }

    void RemoveChunk(GameObject removed)
    {
        GenerationRequest toBeRemoved = null;
        foreach (GenerationRequest request in generationRequestList)
        {
            if (request.chunk == removed)
            {
                toBeRemoved = request;
            }
        }
        if (toBeRemoved != null)
        {
            toBeRemoved.Dispose();

            generationRequestList.Remove(toBeRemoved);
        }
        RemoveObjectHelper.RemoveObject(removed);
    }
    private void OnEnable()
    {
        Continuous2DGeneration script = FindAnyObjectByType<Continuous2DGeneration>();
        if (script != null)
        {
            script.DeleteChunkAtPosition += RemoveChunk;
            script.GenerateAtPosition += GenerateChunk;
        }
        else
        {
            GenerationRefresh script2 = FindAnyObjectByType<GenerationRefresh>();
            script2.DeleteChunkAtPosition += RemoveChunk;
            script2.GenerateAtPosition += GenerateChunk;
        }
    }

    void OnDestroy()
    {
        print("disposing!");
        foreach (GenerationRequest request in generationRequestList)
        {
            request.Dispose();
        }
        generationData.Dispose();
        coinData.Dispose();
        grassData.Dispose();
        baseGrid.NhexToTri.Dispose();
        baseGrid.NtriToHex.Dispose();
        baseGrid.NtriToTri.Dispose();
        baseGrid.Neighbors.Dispose();
        baseGrid.NstartingPoints.Dispose();
        baseGrid.NdestinationPoints.Dispose();
        masterAvailableKeys.Dispose();
        revMasterAvailableKeys.Dispose();
    }
}
