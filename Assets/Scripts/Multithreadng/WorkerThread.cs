using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct WorkerThread : IJob
{
    public Vector2Int areaMax;
    public Vector2Int areaMin;

    [ReadOnly] public NativeArray<Vector2> masterAvailableKeys;
    [ReadOnly] public NativeParallelHashMap<Vector2, int> revMasterAvailableKeys;

    [ReadOnly] public NativeParallelMultiHashMap<Vector2, Vector2> triToHex;
    [ReadOnly] public NativeParallelMultiHashMap<Vector2, Vector2> hexToTri;//unused for now
    [ReadOnly] public NativeParallelMultiHashMap<Vector2, Vector2> triToTri;

    [ReadOnly] public NativeArray<SpawnObjectData> generationData;


    [ReadOnly] public NativeParallelMultiHashMap<Vector2, Vector2> neighbors;
    [ReadOnly] public NativeHashSet<Vector2> destinationPoints;
    [ReadOnly] public NativeHashSet<Vector2> startingPoints;
    [ReadOnly] public NativeArray<CoinDataStruct> grassData;
    [ReadOnly] public NativeArray<CoinDataStruct> coinData;


    //managed data
    public NativeArray<int> indices;
    public NativeArray<int> indices2;
    public Unity.Mathematics.Random random;
    public NativeArray<int> result;
    public NativeArray<int> grassResult;
    public NativeArray<int> coinResult;

    public NativeQueue<Vector2> toBeExplored;
    
    public NativeHashSet<Vector2> discovered;
    public NativeHashSet<Vector2> obstructed;

    public NativeArray<int> coinNeighborCount;


    public void Execute()
    {
        GenerateTerrain();
        
        int tries = 1;
        while (!TestPathfinding())
        {
            tries++;
            ResetGeneration();
            GenerateTerrain();
            if (tries > 100) break;
        }
        GenerateCoins();
        GenerateGrass();
    }

    private void ResetGeneration()
    {
        for (int i = 0; i < result.Length; i++) result[i] = -1;
        for (int i = 0; i < indices.Length; i++) indices[i] = i;
    }

    

    private void GenerateTerrain()
    {

        int masterCount = masterAvailableKeys.Length;
        if (masterCount == 0) return;

        for (int i = 0; i < masterCount; i++) indices[i] = i;

        //object
        SpawnObjectData reworkedHoneyGeneration;
        

        // active prefix length (number of available keys remaining)
        int keyCount = masterCount;
        int toSpawnCount;
        int spawnedObjectCount = 0;
        int rangeStart;
        int rangeEnd;


        int tmpIdx;
        int randomIndex;

        // iterate over object types
        for (int objectToSpawn = 0; objectToSpawn < generationData.Length; objectToSpawn++)
        {
            // compute how many objects to spawn for this type
            reworkedHoneyGeneration = generationData[objectToSpawn];
            toSpawnCount = random.NextInt(reworkedHoneyGeneration.MinSpawnCount, reworkedHoneyGeneration.MaxSpawnCount);

            //calculate range
            rangeStart = spawnedObjectCount;
            rangeEnd = rangeStart + toSpawnCount;
            if (rangeEnd > keyCount)
            {
                rangeEnd = keyCount;
            }

            //randomize the indices array for the first n indices
            for (int j = rangeStart; j < rangeEnd; j++)
            {
                // pick random index in the remaining range [i, keyCount-1]
                randomIndex = random.NextInt(j, keyCount);

                // swap indices[randomIndex] <-> indices[i]
                tmpIdx = indices[randomIndex];
                indices[randomIndex] = indices[j];
                indices[j] = tmpIdx;
            }

            //step1 spawn basic objects
            for (int j = rangeStart; j < rangeEnd; j++)
            {
                // chosen key is masterAvailableKeys[ indices[i] ]
                Vector2 chosenKey = masterAvailableKeys[indices[j]];

                int chosenIndex = revMasterAvailableKeys[chosenKey];

                if (result[chosenIndex] != -1) continue;

                result[chosenIndex] = objectToSpawn;
                spawnedObjectCount++;

            }



            //step2, spawn objects around it.
            foreach (ExtraSpawnData extraObject in reworkedHoneyGeneration.Extras)
            {
                int chance = extraObject.AddedSpawnChance;

                for (int j = rangeStart; j < rangeEnd; j++)
                {
                    Vector2 chosenKey = masterAvailableKeys[indices[j]];
                    foreach (Vector2 pos in triToTri.GetValuesForKey(chosenKey))
                    {

                        //here
                        int posIndex = revMasterAvailableKeys[pos];

                        if (result[posIndex] != -1)
                            continue;
                        if (random.NextInt(0, 100) > chance) continue;

                        result[posIndex] = extraObject.IndexInList;

                        //swap
                        tmpIdx = indices[revMasterAvailableKeys[pos]];
                        indices[revMasterAvailableKeys[pos]] = indices[spawnedObjectCount];
                        indices[spawnedObjectCount] = tmpIdx;

                        spawnedObjectCount++;
                    }
                }
            }
        }
    }

    

    private bool TestPathfinding()
    {
        PreparePathfindingDataStructures();
        
        //test
        bool result1 = false;
        foreach (Vector2 startingPoint in startingPoints)
        {
            result1 = ReworkedBFS(startingPoint);

            if (result1 == false)
            {
                return false;
            }
        }
        return true;
    }

    private void PreparePathfindingDataStructures()
    {
        obstructed.Clear();

        for (int i = 0; i < result.Length; i++)
        {
            if (result[i] == -1) continue;
            foreach (Vector2 hexPos in triToHex.GetValuesForKey(masterAvailableKeys[i]))
            {
                obstructed.Add(hexPos);
            }
        }
    }

    public bool ReworkedBFS(Vector2 startingPos)
    {
        Vector2 currentNode = startingPos;
        
        toBeExplored.Clear();
        discovered.Clear();

        toBeExplored.Enqueue(currentNode);
        discovered.Add(currentNode);
        while (toBeExplored.Count > 0)
        {
            currentNode = toBeExplored.Dequeue();

            foreach (Vector2 foundHexPos in neighbors.GetValuesForKey(currentNode))
            {
                if (destinationPoints.Contains(foundHexPos))
                {
                    return true;
                }
                
                if (discovered.Contains(foundHexPos)) continue;
                if (obstructed.Contains(foundHexPos)) continue;
                
                toBeExplored.Enqueue(foundHexPos);
                discovered.Add(foundHexPos);
            }
            if (toBeExplored.Count >= neighbors.Count())
            {
                return false;
            }
        }

        return false;
    }

    private void GenerateCoins()
    {
        
        for (int i = 0; i < result.Count(); i++)
        {
            if (result[i] == -1) continue;
            foreach (Vector2 neighbor in triToTri.GetValuesForKey(masterAvailableKeys[i]))
            {
                if (result[revMasterAvailableKeys[neighbor]] == -1) continue;
                coinNeighborCount[revMasterAvailableKeys[neighbor]]++;
            }
        }
        HashSet<Vector2> smallCoins = new();
        HashSet<Vector2> largeCoins = new();

        for (int i = 0; i < coinNeighborCount.Length; i++)
        {
            if (coinNeighborCount[i] == 0) continue;
            if (coinNeighborCount[i] == 1) smallCoins.Add(masterAvailableKeys[i]);
            if (coinNeighborCount[i] > 1) largeCoins.Add(masterAvailableKeys[i]);
        }
        

        for (int i = 0; i < indices.Length; i++) indices[i] = i;
        int randomIndex;
        int tmpIdx;

        int toSpawnCount = random.NextInt(coinData[0].MinSpawnCount, coinData[0].MaxSpawnCount);
        if (toSpawnCount > smallCoins.Count) toSpawnCount = smallCoins.Count;
        

        //randomize the indices array for the first n indices
        
        for (int j = 0; j < toSpawnCount; j++)
        {
            // pick random index in the remaining range [i, keyCount-1]
            randomIndex = random.NextInt(j, smallCoins.Count);

            // swap indices[randomIndex] <-> indices[i]
            tmpIdx = indices[randomIndex];
            indices[randomIndex] = indices[j];
            indices[j] = tmpIdx;
        }
        Vector2[] tempVector = smallCoins.ToArray();
        for (int j = 0; j < toSpawnCount; j++)
        {
            coinResult[revMasterAvailableKeys[tempVector[indices[j]]]] = 0;
            
        }

        //big coins

        for (int i = 0; i < indices.Length; i++) indices[i] = i;
        

        toSpawnCount = random.NextInt(coinData[0].MinSpawnCount, coinData[0].MaxSpawnCount);
        if (toSpawnCount > largeCoins.Count) toSpawnCount = largeCoins.Count;


        //randomize the indices array for the first n indices

        for (int j = 0; j < toSpawnCount; j++)
        {
            // pick random index in the remaining range [i, keyCount-1]
            randomIndex = random.NextInt(j, largeCoins.Count);

            // swap indices[randomIndex] <-> indices[i]
            tmpIdx = indices[randomIndex];
            indices[randomIndex] = indices[j];
            indices[j] = tmpIdx;
        }
        tempVector = largeCoins.ToArray();
        for (int j = 0; j < toSpawnCount; j++)
        {
            coinResult[revMasterAvailableKeys[tempVector[indices[j]]]] = 1;

        }

    }
    private void GenerateGrass()
    {
        for (int i = 0; i < indices2.Length; i++) indices2[i] = i;
        int randomIndex;
        int tmpIdx;

        CoinDataStruct grassInfo;
        int toSpawnCount;
        int keyCount = indices2.Length;

        int spawnedObjectCount = 0;
        int rangeStart;
        int rangeEnd;

        // iterate over object types
        for (int objectToSpawn = 0; objectToSpawn < grassData.Count(); objectToSpawn++)
        {
            // compute how many objects to spawn for this type
            grassInfo = grassData[objectToSpawn];
            toSpawnCount = random.NextInt(grassInfo.MinSpawnCount, grassInfo.MaxSpawnCount);

            //calculate range
            rangeStart = spawnedObjectCount;
            rangeEnd = rangeStart + toSpawnCount;
            if (rangeEnd > keyCount)
            {
                rangeEnd = keyCount;
            }

            //randomize the indices array for the first n indices
            for (int j = rangeStart; j < rangeEnd; j++)
            {
                // pick random index in the remaining range [i, keyCount-1]
                randomIndex = random.NextInt(j, keyCount);

                // swap indices[randomIndex] <-> indices[i]
                tmpIdx = indices2[randomIndex];
                indices2[randomIndex] = indices2[j];
                indices2[j] = tmpIdx;
            }

            //step1 spawn basic objects
            for (int j = rangeStart; j < rangeEnd; j++)
            {
                grassResult[indices2[j]] = objectToSpawn;
                spawnedObjectCount++;
            }
        }
    }


    public struct SpawnObjectData
    {
        public int MinSpawnCount;
        public int MaxSpawnCount;

        public FixedList128Bytes<ExtraSpawnData> Extras;
    }

    public struct ExtraSpawnData
    {
        public int IndexInList;
        public int AddedSpawnChance;
    }

    public struct CoinDataStruct
    {
        public int MinSpawnCount;
        public int MaxSpawnCount;
    }
    
}
