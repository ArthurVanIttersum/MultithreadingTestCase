using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class HoneyCombChunkGeneration : MonoBehaviour
{
    //settings Files
    [SerializeField] private BiomeSelection biomeSelection;
    public ReworkedHoneyGenerationSettings generationSettings;
    public ChunkSettings chunkSettings;

    //booleans
    public bool playerMoveAlongZaxis;
    public bool displayDebugVisualization;
    public bool enableDebugprints;

    //datastructures
    private Vector2Int areaMax;
    private Vector2Int areaMin;
    private GameObject chunk;

    //dictionaries
    public Dictionary<Vector2, List<Vector2>> triToHex = new();
    public Dictionary<Vector2, List<Vector2>> hexToTri = new();
    public Dictionary<Vector2, List<Vector2>> triToTri = new();

    //lists
    public HashSet<Vector2> startingPoints = new();
    public HashSet<Vector2> destinationPoints = new();
    private List<Vector2> hexOffsets2 = new();

    //hex grid
    private Grid theHexGrid = new();

    //caching
    private bool cached = false;

    //special data for generation
    private List<Vector2> masterAvailableKeys = new();
    private Dictionary<Vector2, int> revMasterAvailableKeys = new();
    private int[] indices;
    
    //special data for coin generation
    private HashSet<Vector2> coinPositions = new();
    private HashSet<Vector2> bigCoinPositions = new();
    private HashSet<Vector2> smallCoinPositions = new();
    private Vector2[] smallCoinArray;
    private Vector2[] bigCoinArray;

    //grassStuff
    private Vector2[] grassStuffArray;

    //Object settings
    [SerializeField] private float minSize;
    [SerializeField] private float maxSize;
    [SerializeField] private float minGrassSize = 0.5f;
    [SerializeField] private float maxGrassSize = 0.8f;

    private void Awake()
    {
        if (biomeSelection == null)
        {
            Debug.LogWarning("generation settings not assigned");
        }
        else
        {
            ScriptableObject settingsFile = biomeSelection.Biome(0);
            if (settingsFile is ReworkedHoneyGenerationSettings)
            {
                generationSettings = (ReworkedHoneyGenerationSettings)settingsFile;
            }
            else
            {
                Debug.LogWarning("generation settings not assigned");
            }
        }
    }

    

    void GenerateChunk(GameObject chunk)
    {
        if (biomeSelection == null)
        {
            Debug.LogWarning("generation settings not assigned");
        }
        else
        {
            ScriptableObject settingsFile = biomeSelection.Biome(biomeSelection.randomBiomeChance);
            if (settingsFile is ReworkedHoneyGenerationSettings)
            {
                generationSettings = (ReworkedHoneyGenerationSettings)settingsFile;
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
        this.chunk = chunk;

        int chunkSize = chunkSettings.chunksize;
        Vector2Int centerPos = Vector2Int.zero;

        areaMax = (Vector2Int.one * (int)chunkSize / 2) + centerPos;
        areaMin = -(Vector2Int.one * (int)chunkSize / 2) + centerPos;

        if (!cached)
        {
            print("caching data");
            CacheData();
            cached = true;

        }

        ResetTerrainGeneration();
        GenerateTerrain();


        int tries = 1;
        while (!TestPathfinding())
        {
            tries++;
            ResetTerrainGeneration();
            GenerateTerrain();
            if (tries > 100)
            {
                print("warning tried over 100 times. aborting generation atempt");
                break;
            }
        }
        //generate coin and grass positions and place them
        GenerateCoins();
        PlaceGrass();

        //actually place the objects
        PlaceObjects();
    }

    private void CacheData()
    {
        ClearDataStructures();
        MakeDataStructures();
    }


    private void ClearDataStructures()
    {
        //clear datastructures
        theHexGrid.theGraph.Clear();
        triToHex.Clear();
        hexToTri.Clear();
        triToTri.Clear();
        MakeHexOffsets();
        startingPoints.Clear();
        destinationPoints.Clear();
    }

    private void MakeHexOffsets()
    {
        hexOffsets2.Clear();

        //floating point math
        float onethird = 1.0f / 3.0f;
        float twothird = 2.0f / 3.0f;
        Vector2 hexScale = chunkSettings.hexScalar;

        if (playerMoveAlongZaxis)
        {
            hexOffsets2.Add(new Vector2(0f, twothird) * hexScale);
            hexOffsets2.Add(new Vector2(-0.5f, onethird) * hexScale);
            hexOffsets2.Add(new Vector2(-0.5f, -onethird) * hexScale);
            hexOffsets2.Add(new Vector2(0f, -twothird) * hexScale);
            hexOffsets2.Add(new Vector2(0.5f, -onethird) * hexScale);
            hexOffsets2.Add(new Vector2(0.5f, onethird) * hexScale);
        }
        else
        {
            hexOffsets2.Add(new Vector2(twothird, 0f) * hexScale);
            hexOffsets2.Add(new Vector2(onethird, -0.5f) * hexScale);
            hexOffsets2.Add(new Vector2(-onethird, -0.5f) * hexScale);
            hexOffsets2.Add(new Vector2(-twothird, 0f) * hexScale);
            hexOffsets2.Add(new Vector2(-onethird, 0.5f) * hexScale);
            hexOffsets2.Add(new Vector2(onethird, 0.5f) * hexScale);
        }
    }

    private void MakeDataStructures()
    {
        Vector2 hexScale = chunkSettings.hexScalar;

        // make triangles
        if (playerMoveAlongZaxis)
        {
            int horCount = Mathf.CeilToInt((areaMax.x - areaMin.x) / hexScale.x);
            for (int h = 0; h < horCount; h++)
            {
                float hor = areaMin.x + h * hexScale.x;
                int verCount = Mathf.CeilToInt((areaMax.y - areaMin.y) / hexScale.y);
                for (int v = 0; v < verCount; v++)
                {
                    float ver = areaMin.y + v * hexScale.y;
                    float halfSpaceOffset = (v % 2 == 0) ? hexScale.x * 0.5f : 0f;
                    Vector2 triPos = new Vector2(hor + halfSpaceOffset, ver);
                    triToHex.Add(SnapVector(triPos), new());
                }
            }
        }
        else
        {
            int horCount = Mathf.CeilToInt((areaMax.x - areaMin.x) / hexScale.x);
            for (int h = 0; h < horCount; h++)
            {
                float hor = areaMin.x + h * hexScale.x;
                float halfSpaceOffset = (h % 2 == 0) ? hexScale.y * 0.5f : 0f;
                int verCount = Mathf.CeilToInt((areaMax.y - areaMin.y) / hexScale.y);
                for (int v = 0; v < verCount; v++)
                {
                    float ver = areaMin.y + v * hexScale.y;
                    Vector2 triPos = new Vector2(hor, ver + halfSpaceOffset);
                    triToHex.Add(SnapVector(triPos), new());
                }
            }
        }

        //make hexagons
        foreach (var item in triToHex.Keys)
        {
            for (int i = 0; i < 6; i++)
            {
                Vector2 hexpos = SnapVector(item + hexOffsets2[i]);//snapping to thirds if close, to avoid hashing issues in the dictionary, making duplicate keys
                if (!hexToTri.ContainsKey(hexpos))
                {
                    hexToTri.Add(hexpos, new());
                }
                hexToTri[hexpos].Add(item);
                triToHex[item].Add(hexpos);

            }
        }

        //add triToTri neighbors
        foreach (var item in triToHex.Keys)
        {
            triToTri.Add(item, new());
        }

        foreach (var tri1 in triToHex.Keys)
        {
            foreach (var hex in triToHex[tri1])
            {
                foreach (var tri2 in hexToTri[hex])
                {
                    if (tri1 == tri2) continue;
                    if (triToTri[tri1].Contains(tri2)) continue;
                    triToTri[tri1].Add(tri2);
                }
            }
        }

        //starting points and destination points
        float onethird = 1.0f / 3.0f;
        float twothird = 2.0f / 3.0f;

        //positions to test
        List<Vector2> positionsToTest = new();


        if (playerMoveAlongZaxis)
        {
            positionsToTest.Add(SnapVector(new Vector2(0, -onethird) + areaMin));
            positionsToTest.Add(SnapVector(new Vector2(0, -twothird) + areaMin));
            positionsToTest.Add(SnapVector(new Vector2(0, -onethird) + areaMax));
            positionsToTest.Add(SnapVector(new Vector2(0, -twothird) + areaMax));
        }
        else
        {
            positionsToTest.Add(SnapVector(new Vector2(-onethird, 0) + areaMin));
            positionsToTest.Add(SnapVector(new Vector2(-twothird, 0) + areaMin));
            positionsToTest.Add(SnapVector(new Vector2(-onethird, 0) + areaMax));
            positionsToTest.Add(SnapVector(new Vector2(-twothird, 0) + areaMax));
        }


        if (playerMoveAlongZaxis)
        {
            foreach (var hexPos in hexToTri.Keys)
            {
                if (hexPos.y == positionsToTest[0].y)
                {
                    //startingPoints.Add(hexPos);
                }
                if (hexPos.y == positionsToTest[1].y)
                {
                    startingPoints.Add(hexPos);
                }
                if (hexPos.y == positionsToTest[2].y)
                {
                    destinationPoints.Add(hexPos);
                }
                if (hexPos.y == positionsToTest[3].y)
                {
                    //destinationPoints.Add(hexPos);
                }
            }
        }
        else
        {
            foreach (var hexPos in hexToTri.Keys)
            {
                if (hexPos.x == positionsToTest[0].x)
                {
                    //startingPoints.Add(hexPos);
                }
                if (hexPos.x == positionsToTest[1].x)
                {
                    startingPoints.Add(hexPos);
                }
                if (hexPos.x == positionsToTest[2].x)
                {
                    destinationPoints.Add(hexPos);
                }
                if (hexPos.x == positionsToTest[3].x)
                {
                    //destinationPoints.Add(hexPos);
                }
            }
        }

        //build datastructures for generation
        BuildAvailableKeys();
        grassStuffArray = hexToTri.Keys.ToArray();

        //make grid

        //initiate nodes
        int iD = 0;
        foreach (var item in hexToTri.Keys)//hexnodes
        {
            iD++;
            theHexGrid.theGraph.Add(item, new Node(item, iD));
        }
        iD = 0;
        foreach (var item in triToHex.Keys)//trinodes
        {
            theHexGrid.theGraph.Add(item, new Node(item, iD));
        }

        //add neighbors
        foreach (var item in triToHex.Keys)//hexneighbors
        {
            int count = triToHex[item].Count;
            for (int i = 0; i < count; i++)
            {

                int more = (i + 1 + count) % count;
                int less = (i - 1 + count) % count;
                Vector2 pos0 = triToHex[item][i];
                Vector2 pos1 = triToHex[item][more];
                Vector2 pos1n = triToHex[item][less];
                if (VectorCompareDirectional(pos0, pos1))
                {
                    theHexGrid.theGraph[pos0].neighborsHex.Add(theHexGrid.theGraph[pos1]);
                }
                if (VectorCompareDirectional(pos0, pos1n))
                {
                    theHexGrid.theGraph[pos0].neighborsHex.Add(theHexGrid.theGraph[pos1n]);
                }
                if (VectorCompareDirectional(pos0, item))
                {
                    theHexGrid.theGraph[pos0].neighborsHex.Add(theHexGrid.theGraph[item]);
                }
                if (VectorCompareDirectional(item, pos0))
                {
                    theHexGrid.theGraph[item].neighborsHex.Add(theHexGrid.theGraph[pos0]);
                }
            }
        }

        //initiate destination list
        foreach (var item in destinationPoints)
        {
            theHexGrid.destinationIDs.Add(theHexGrid.theGraph[item].iD);
        }
    }

    private bool VectorCompareDirectional(Vector2 node, Vector2 neighbor)
    {
        if (playerMoveAlongZaxis)
        {
            return (node.y < neighbor.y);
        }
        else
        {
            return (node.x < neighbor.x);
        }
    }

    Vector2 SnapVector(Vector2 vector2)
    {
        return Canonicalize(SnapVector(vector2, chunkSettings.hexScalar), chunkSettings.hexScalar);
    }

    // Explicit overload that accepts a scale
    Vector2 SnapVector(Vector2 vector2, Vector2 hexScale)
    {
        return new Vector2(
            SnapToScaledGridIfClose(vector2.x, hexScale.x),
            SnapToScaledGridIfClose(vector2.y, hexScale.y)
        );
    }




    float SnapToScaledGridIfClose(float value, float scale, float relEpsilon = 1e-4f, float minEpsilon = 1e-5f)
    {
        if (MathF.Abs(scale) < 1e-9f)
            return value;

        // Use unit = scale / 6 so both 1/3 and 1/2 offsets are representable
        double unit = (double)scale / 6.0;

        double valueD = (double)value;
        double snappedUnits = Math.Round(valueD / unit);
        double snapped = snappedUnits * unit;

        double epsilonWorld = Math.Max((double)minEpsilon, Math.Abs(unit) * (double)relEpsilon);

        if (Math.Abs(valueD - snapped) <= epsilonWorld)
            return (float)snapped;

        return value;
    }

    // Canonicalize a world position to the exact grid-aligned Vector2
    Vector2 Canonicalize(Vector2 pos, Vector2 hexScale)
    {
        double unitX = (double)hexScale.x / 6.0;
        double unitY = (double)hexScale.y / 6.0;

        if (Math.Abs(unitX) < 1e-12) unitX = 1e-12;
        if (Math.Abs(unitY) < 1e-12) unitY = 1e-12;

        int ix = (int)Math.Round((double)pos.x / unitX, MidpointRounding.AwayFromZero);
        int iy = (int)Math.Round((double)pos.y / unitY, MidpointRounding.AwayFromZero);

        double cx = ix * unitX;
        double cy = iy * unitY;

        return new Vector2((float)cx, (float)cy);
    }

    private void BuildAvailableKeys()
    {
        masterAvailableKeys.Clear();
        revMasterAvailableKeys.Clear();
        foreach (var key in triToHex.Keys)
        {
            revMasterAvailableKeys.Add(key, masterAvailableKeys.Count);
            masterAvailableKeys.Add(key);
        }
    }


    


    private void GenerateTerrain()
    {
        
        int masterCount = masterAvailableKeys.Count;
        if (masterCount == 0) return;

        // Create an index buffer 0..n-1
        indices = new int[masterCount];
        for (int i = 0; i < masterCount; i++) indices[i] = i;

        //object
        ReworkedHoneySpawnedObject reworkedHoneyGeneration;

        // active prefix length (number of available keys remaining)
        int keyCount = masterCount;
        int toSpawnCount;
        int spawnedObjectCount = 0;
        int rangeStart;
        int rangeEnd;


        int tmpIdx;
        int randomIndex;

        // iterate over object types
        for (int objectToSpawn = 0; objectToSpawn < generationSettings.objects.Count; objectToSpawn++)
        {
            // compute how many objects to spawn for this type
            reworkedHoneyGeneration = generationSettings.objects[objectToSpawn];
            toSpawnCount = UnityEngine.Random.Range(reworkedHoneyGeneration.minSpawnCount, reworkedHoneyGeneration.maxSpawnCount);
            
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
                randomIndex = UnityEngine.Random.Range(j, keyCount);

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

                // defensive checks using theHexGrid
                if (!theHexGrid.theGraph.TryGetValue(chosenKey, out var chosenNode)) continue;
                if (chosenNode.spawnedIndex != -1) continue;

                chosenNode.spawnedIndex = objectToSpawn;
                spawnedObjectCount ++;

            }



            //step2, spawn objects around it.
            foreach (IncreaseSpawnChance extraObject in reworkedHoneyGeneration.extraObjects)
            {
                int chance = extraObject.addedSpawnChance;

                for (int j = rangeStart; j < rangeEnd; j++)
                {
                    Vector2 chosenKey = masterAvailableKeys[indices[j]];
                    foreach (Vector2 pos in triToTri[chosenKey])
                    {
                        if (!theHexGrid.theGraph.TryGetValue(pos, out var chosenNode)) continue;
                        if (chosenNode.spawnedIndex != -1) continue;
                        if (UnityEngine.Random.Range(0, 100) > chance) continue;

                        chosenNode.spawnedIndex = extraObject.indexInList;

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




    private void ResetTerrainGeneration()
    {
        foreach (Vector2 triPoint in triToHex.Keys)
        {
            
            theHexGrid.theGraph[triPoint].spawnedIndex = -1;
        }

        foreach (Vector2 hexPoint in hexToTri.Keys)
        {
            theHexGrid.theGraph[hexPoint].obstructed = false;
        }
    }

    private bool TestPathfinding()
    {
        //add obstacles
        foreach (var triPos in triToHex.Keys)
        {
            if (theHexGrid.theGraph[triPos].spawnedIndex == -1) continue;
            foreach (var neighbor in theHexGrid.theGraph[triPos].neighborsHex)
            {
                neighbor.obstructed = true;
            }
        }

        //test
        bool result = false;
        foreach (var startingPoint in startingPoints)
        {
            result = theHexGrid.ReworkedBFS(startingPoint, playerMoveAlongZaxis);
            
            if (result == false)
            {
                return false;
            }
        }
        return true;
    }

    private void GenerateCoins()
    {
        //reset everything
        coinPositions.Clear();
        smallCoinPositions.Clear();
        bigCoinPositions.Clear();

        //generate hashset
        foreach (Vector2 key in triToTri.Keys)
        {
            if (theHexGrid.theGraph[key].spawnedIndex == -1) continue;
            foreach (var neighbor in triToTri[key])
            {
                if (theHexGrid.theGraph[neighbor].spawnedIndex != -1) continue;
                coinPositions.Add(neighbor);
            }
        }
        int neighbors = 0;
        foreach (Vector2 key in coinPositions)
        {
            foreach (Vector2 neighbor in triToTri[key])
            {
                if (theHexGrid.theGraph[neighbor].spawnedIndex == -1) continue;
                neighbors++;
            }
            if (neighbors == 1)
            {
                smallCoinPositions.Add(key);
            }
            else
            {
                bigCoinPositions.Add(key);
            }
            neighbors = 0;
        }
        //print("coinPositions: " + coinPositions.Count.ToString() + ", smallCoinPositions: " + smallCoinPositions.Count.ToString() + ", bigCoinPositions: " + bigCoinPositions.Count.ToString());


        //initialize ints
        int randomIndex;
        int tmpIdx;


        //      small coins

        //initialize arrays

        indices = new int[smallCoinPositions.Count];
        for (int i = 0; i < smallCoinPositions.Count; i++) indices[i] = i;
        smallCoinArray = smallCoinPositions.ToArray();


        //compute how many objects to spawn for this type
        CoinData coinData = generationSettings.smallCoinData;
        int toSpawnCount = UnityEngine.Random.Range(coinData.minSpawnCount, coinData.maxSpawnCount);

        //calculate range
        if (toSpawnCount > smallCoinPositions.Count)
        {
            toSpawnCount = smallCoinPositions.Count;
        }

        //randomize the indices array for the first n indices
        for (int j = 0; j < toSpawnCount; j++)
        {
            // pick random index in the remaining range [i, keyCount-1]
            randomIndex = UnityEngine.Random.Range(j, smallCoinPositions.Count);

            // swap indices[randomIndex] <-> indices[i]
            tmpIdx = indices[randomIndex];
            indices[randomIndex] = indices[j];
            indices[j] = tmpIdx;
        }

        for (int j = 0; j < toSpawnCount; j++)
        {
            Instantiate(generationSettings.smallCoinData.prefab, VectorConversion.vec2Tovec3(smallCoinArray[indices[j]]) + chunk.transform.position, Quaternion.identity, chunk.transform);
        }

        //      big coins

        //initialize arrays

        indices = new int[bigCoinPositions.Count];
        for (int i = 0; i < bigCoinPositions.Count; i++) indices[i] = i;
        bigCoinArray = bigCoinPositions.ToArray();


        //compute how many objects to spawn for this type
        coinData = generationSettings.bigCoinData;
        toSpawnCount = UnityEngine.Random.Range(coinData.minSpawnCount, coinData.maxSpawnCount);

        //calculate range
        if (toSpawnCount > bigCoinPositions.Count)
        {
            toSpawnCount = bigCoinPositions.Count;
        }

        //randomize the indices array for the first n indices
        for (int j = 0; j < toSpawnCount; j++)
        {
            // pick random index in the remaining range [i, keyCount-1]
            randomIndex = UnityEngine.Random.Range(j, bigCoinPositions.Count);

            // swap indices[randomIndex] <-> indices[i]
            tmpIdx = indices[randomIndex];
            indices[randomIndex] = indices[j];
            indices[j] = tmpIdx;
        }

        for (int j = 0; j < toSpawnCount; j++)
        {
            Instantiate(generationSettings.bigCoinData.prefab, VectorConversion.vec2Tovec3(bigCoinArray[indices[j]]) + chunk.transform.position, Quaternion.identity, chunk.transform);
        }

    }


    private void PlaceGrass()
    {
        int masterCount = grassStuffArray.Length;
        if (masterCount == 0) return;

        // Create an index buffer 0..n-1
        indices = new int[masterCount];
        for (int i = 0; i < masterCount; i++) indices[i] = i;

        //coindata
        CoinData grassData;
        GameObject newObject;

        // active prefix length (number of available keys remaining)
        int keyCount = masterCount;
        int toSpawnCount;
        int spawnedObjectCount = 0;
        int rangeStart;
        int rangeEnd;


        int tmpIdx;
        int randomIndex;

        // iterate over object types
        for (int objectToSpawn = 0; objectToSpawn < generationSettings.grassAndStuff.Count; objectToSpawn++)
        {
            // compute how many objects to spawn for this type
            grassData = generationSettings.grassAndStuff[objectToSpawn];
            toSpawnCount = UnityEngine.Random.Range(grassData.minSpawnCount, grassData.maxSpawnCount);

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
                randomIndex = UnityEngine.Random.Range(j, keyCount);

                // swap indices[randomIndex] <-> indices[i]
                tmpIdx = indices[randomIndex];
                indices[randomIndex] = indices[j];
                indices[j] = tmpIdx;
            }

            //step1 spawn basic objects
            for (int j = rangeStart; j < rangeEnd; j++)
            {
                newObject = Instantiate(generationSettings.grassAndStuff[objectToSpawn].prefab, VectorConversion.vec2Tovec3(grassStuffArray[indices[j]]) + chunk.transform.position, Quaternion.identity, chunk.transform);
                newObject.transform.localScale *= UnityEngine.Random.Range(minGrassSize, maxGrassSize);
                spawnedObjectCount++;

            }
        }


        //initialize arrays

        indices = new int[hexToTri.Count];
        for (int i = 0; i < hexToTri.Count; i++) indices[i] = i;
        
    }

    private void PlaceObjects()
    {
        GameObject objects;
        int indexValue = 0;
        foreach (var hexPos in triToHex.Keys)
        {
            indexValue = theHexGrid.theGraph[hexPos].spawnedIndex;
            if (indexValue != -1)
            {
                objects = Instantiate(generationSettings.objects[indexValue].prefab, VectorConversion.vec2Tovec3(hexPos) + chunk.transform.position, Quaternion.identity, chunk.transform);
                objects.transform.localScale *= UnityEngine.Random.Range(minSize, maxSize);
                objects.transform.rotation = Quaternion.AngleAxis(UnityEngine.Random.Range(0, 360), Vector3.up);
            }
        }
    }

    void RemoveChunk(GameObject removed)
    {
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

    private void OnDrawGizmos()
    {
        if (!displayDebugVisualization) return;


        //style
        GUIStyle style = new GUIStyle();
        style.fontSize = 48;
        style.fontStyle = FontStyle.Normal;
        style.normal.textColor = Color.white;

        //chunkArea
        Gizmos.color = Color.blue;
        Vector3 topleft = new Vector3(areaMin.x, 0, areaMax.y);
        Vector3 bottomleft = new Vector3(areaMin.x, 0, areaMin.y);
        Vector3 topright = new Vector3(areaMax.x, 0, areaMax.y);
        Vector3 bottomright = new Vector3(areaMax.x, 0, areaMin.y);

        Gizmos.DrawSphere(topleft, 0.3f);
        Gizmos.DrawSphere(topright, 0.3f);
        Gizmos.DrawSphere(bottomleft, 0.3f);
        Gizmos.DrawSphere(bottomright, 0.3f);

        Gizmos.DrawLine(topleft, topright);
        Gizmos.DrawLine(bottomleft, bottomright);
        Gizmos.DrawLine(topleft, bottomleft);
        Gizmos.DrawLine(topright, bottomright);

        //grid test
        Gizmos.color = Color.red;
        for (int i = 0; i < chunkSettings.chunksize; i++)
        {
            Vector3 horOffset = Vector3.right * i;
            Vector3 verOffset = Vector3.forward * i;

            //Gizmos.DrawLine(bottomleft + horOffset, topleft + horOffset);
            //Gizmos.DrawLine(bottomleft + verOffset, bottomright + verOffset);
        }

        //triangles
        Gizmos.color = Color.yellow;
        foreach (var item in triToHex.Keys)
        {
            Gizmos.DrawSphere(VectorConversion.vec2Tovec3(item), 0.2f);
        }
        Gizmos.color = Color.green;
        foreach (var item in hexToTri.Keys)
        {
            Gizmos.DrawSphere(VectorConversion.vec2Tovec3(item), 0.1f);
        }

        //lines for hexes
        Gizmos.color = Color.darkGreen;
        foreach (var item in triToHex.Keys)
        {
            for (int i = 0; i < triToHex[item].Count; i++)
            {
                int next = i + 1;
                if (next == triToHex[item].Count)
                {
                    next = 0;
                }
                Vector3 hexCorner1 = VectorConversion.vec2Tovec3(triToHex[item][i]);
                Vector3 hexCorner2 = VectorConversion.vec2Tovec3(triToHex[item][next]) + Vector3.up * 0f;
                Gizmos.DrawLine(hexCorner1, hexCorner2);
            }
        }

        //lines tri to hex
        Gizmos.color = Color.limeGreen;
        foreach (var item in triToHex.Keys)
        {
            for (int i = 0; i < triToHex[item].Count; i++)
            {
                Gizmos.DrawLine(VectorConversion.vec2Tovec3(item), VectorConversion.vec2Tovec3(triToHex[item][i]));
            }
        }

        //lines hex to tri
        Gizmos.color = Color.magenta;
        foreach (var item in hexToTri.Keys)
        {
            for (int i = 0; i < hexToTri[item].Count; i++)
            {
                Gizmos.DrawLine(VectorConversion.vec2Tovec3(item), VectorConversion.vec2Tovec3(hexToTri[item][i]));
#if UNITY_EDITOR
                UnityEditor.Handles.Label(VectorConversion.vec2Tovec3(item), hexToTri[item].Count.ToString(), style);
#endif
            }
        }

        //display grid
        Gizmos.color = Color.blue;
        foreach (var item in theHexGrid.theGraph.Keys)
        {
            Gizmos.DrawSphere(VectorConversion.vec2Tovec3(item) + Vector3.up * 5, 0.1f);
        }



        Gizmos.color = Color.red;
        foreach (var item in triToTri.Keys)
        {
            int number2 = triToTri[item].Count;

            for (int i = 0; i < number2; i++)
            {
                Vector3 origin = VectorConversion.vec2Tovec3(item);
                Vector3 destination = VectorConversion.vec2Tovec3(triToTri[item][i]);
                Gizmos.DrawLine(origin + Vector3.up * 20.2f, destination + Vector3.up * 20.4f);
            }
        }

        

        




        foreach (var item in theHexGrid.theGraph.Keys)//display chances
        {

            int tempiteration = 0;
            foreach (var chance in theHexGrid.theGraph[item].spawnchance)
            {
                tempiteration++;
                if (tempiteration == 1)
                {
                    style.normal.textColor = Color.red;
                }
                if (tempiteration == 2)
                {
                    style.normal.textColor = Color.green;
                }
                if (tempiteration == 3)
                {
                    style.normal.textColor = Color.blue;
                }
                //UnityEditor.Handles.Label(VectorConversion.vec2Tovec3(item) + Vector3.up * (1.1f + tempiteration), chance.ToString(), style);
            }
        }

        //display startingpoints
        Gizmos.color = Color.cyan;
        foreach (var startingPoint in startingPoints)
        {
            Gizmos.DrawSphere(VectorConversion.vec2Tovec3(startingPoint), 0.3f);
        }

        //display endpoints
        Gizmos.color = Color.magenta;
        foreach (var destinationpoint in destinationPoints)
        {
            Gizmos.DrawSphere(VectorConversion.vec2Tovec3(destinationpoint), 0.3f);
        }

        //display obstructed points
        Gizmos.color = Color.orangeRed;
        foreach (var item in hexToTri.Keys)
        {
            if (!theHexGrid.theGraph[item].obstructed) continue;

            Gizmos.DrawSphere(VectorConversion.vec2Tovec3(item) + Vector3.up * 5, 0.3f);
        }
    }
}
