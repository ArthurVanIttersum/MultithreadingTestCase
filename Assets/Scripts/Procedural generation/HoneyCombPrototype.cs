using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Diagnostics;

public class HoneyCombPrototype : MonoBehaviour
{
    public HoneyGenerationSettings settingsFile;
    public List<GameObject> generatedObjects;
    public float areaSize = 10.0f;
    private Vector2Int areaMax;
    private Vector2Int areaMin;
    private List<Vector2> hexOffsets2 = new();
    public bool playerMoveAlongZaxis;
    public bool displayDebugVisualization;
    public bool enableDebugprints;

    //dictionaries
    public Dictionary<Vector2, List<Vector2>> triToHex = new();
    public Dictionary<Vector2, List<Vector2>> hexToTri = new();
    public Dictionary<Vector2, List<Vector2>> triToTri = new();

    //lists
    public HashSet<Vector2> startingPoints = new();
    public HashSet<Vector2> destinationPoints = new();

    //hex grid
    public Grid theHexGrid = new();

    [ContextMenu("GenerateEnvironment")]
    void GenerateEnvironment()
    {
        if (enableDebugprints)
        {
            
        }
        Stopwatch sw = Stopwatch.StartNew();
        Stopwatch sw2 = Stopwatch.StartNew();
        string timerValues = "";
        string timerValues2 = "";
        string timerValues3 = "";
        Stopwatch sw5 = Stopwatch.StartNew();
        Stopwatch sw6 = Stopwatch.StartNew();

        if (enableDebugprints)
        {
            print("Removing Objects");
        }
        RemoveObjects();
        if (enableDebugprints)
        {
            print("Generating Environment");
        }
        if (settingsFile == null) return;

        Vector2Int centerPos = VectorConversion.vec3Tovec2Int(transform.position);

        areaMax = (Vector2Int.one * (int)areaSize / 2) + centerPos;
        areaMin = -(Vector2Int.one * (int)areaSize / 2) + centerPos;


        Stopwatch sw3 = Stopwatch.StartNew();
        ClearDataStructures();
        MakeDataStructures();
        sw3.Stop();
        if (enableDebugprints)
        {
            print("time to make datastructures: " + sw3.Elapsed.TotalSeconds);//startup time is negligable. 0.0007 seconds
        }

        Stopwatch sw4 = Stopwatch.StartNew();
        GenerateTerrain();
        sw4.Stop();

        if (enableDebugprints)
        {
            print("time to generate terrain: " + sw4.Elapsed.TotalSeconds);//generation time is negligable. 0.0008 seconds

        }

        int tries = 1;
        sw2.Restart();
        while (!TestPathfinding())
        {
            timerValues += sw2.Elapsed.TotalSeconds.ToString() + ", ";
            timerValues2 += sw5.Elapsed.TotalSeconds.ToString() + ", ";
            sw5.Restart();
            sw6.Restart();
            tries++;
            ResetTerrainGeneration();
            GenerateTerrain();
            timerValues3 += sw6.Elapsed.TotalSeconds.ToString() + ", ";
            if (tries > 100)
            {
                print("warning tried over 100 times. aborting generation atempt");
                break;
            }
            sw2.Restart();
        }
        sw.Stop();
        sw2.Stop();
        if (enableDebugprints)
        {
            print("generation completed in: " + tries.ToString() + " tries, in: " + sw.Elapsed.TotalSeconds);
            print("pathfinding time: " + timerValues);
            print("total loop time: " + timerValues2);
            print("reset and generate terrain: " + timerValues3);
        }
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

    private void MakeDataStructures()
    {
        //make triangles
        if (playerMoveAlongZaxis)
        {
            float halfSpaceOffset = 0;
            for (int hor = areaMin.x; hor < areaMax.x; hor++)
            {

                for (int ver = areaMin.y; ver < areaMax.y; ver++)
                {
                    if (ver % 2 == 0)
                        halfSpaceOffset = 0.5f;
                    else
                        halfSpaceOffset = 0;
                    triToHex.Add(new Vector2(hor + halfSpaceOffset, ver), new());
                }
            }
        }
        else
        {
            float halfSpaceOffset = 0;
            for (int hor = areaMin.x; hor < areaMax.x; hor++)
            {
                if (hor % 2 == 0)
                    halfSpaceOffset = 0.5f;
                else
                    halfSpaceOffset = 0;
                for (int ver = areaMin.y; ver < areaMax.y; ver++)
                {
                    triToHex.Add(new Vector2(hor, ver + halfSpaceOffset), new());
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
                theHexGrid.theGraph[pos0].neighborsHex.Add(theHexGrid.theGraph[pos1]);
                theHexGrid.theGraph[pos0].neighborsHex.Add(theHexGrid.theGraph[pos1n]);
                theHexGrid.theGraph[pos0].neighborsTri.Add(theHexGrid.theGraph[item]);
                theHexGrid.theGraph[item].neighborsHex.Add(theHexGrid.theGraph[pos0]);
            }
        }
    }

    private void GenerateTerrain()
    {
        //spawning
        int tempiteration = 0;
        foreach (var honeyGeneration in settingsFile.objects)
        {

            foreach (var triPoint in triToHex.Keys)
            {
                //check if the space is empty
                if (theHexGrid.theGraph[triPoint].spawnedIndex != -1) continue;

                //calculate chance
                int chance = honeyGeneration.ambientSpawnChance;
                foreach (var cluster in honeyGeneration.clusteredObjects)
                {
                    int addedChance = cluster.addedSpawnChance;

                    foreach (var triNeighborPos in triToTri[triPoint])
                    {
                        if (theHexGrid.theGraph[triNeighborPos].spawnedIndex != cluster.indexInList) continue;
                        chance += addedChance;
                        //print("adding chance " + addedChance + " " + chance + " " + tempiteration);
                    }
                }
                theHexGrid.theGraph[triPoint].spawnchance.Add(chance);

                //maximum chance
                if (chance > 100)
                {
                    chance = 100;
                }

                int random = UnityEngine.Random.Range(0, 100);
                if (random < chance)
                {
                    generatedObjects.Add(Instantiate(honeyGeneration.prefab, VectorConversion.vec2Tovec3(triPoint), Quaternion.identity, transform));

                    theHexGrid.theGraph[triPoint].spawnedIndex = tempiteration;
                    
                }
            }
            tempiteration++;
        }
    }

    private void ResetTerrainGeneration()
    {
        RemoveObjects();
        foreach (Vector2 triPoint in triToHex.Keys)
        {
            theHexGrid.theGraph[triPoint].spawnchance.Clear();
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
        bool success = false;
        foreach (var startingPoint in startingPoints)
        {
            bool result = theHexGrid.BFS(startingPoint, destinationPoints, out List<Vector2> path, playerMoveAlongZaxis);

            if (result == true)
            {
                success = true;
            }

        }
        return success;
    }


    float SnapToThirdIfClose(float value, float epsilon = 0.0005f)
    {
        float third = value * 3f;
        float rounded = MathF.Round(third);

        if (MathF.Abs(third - rounded) < epsilon)
            return rounded / 3f;

        return value; // leave unchanged
    }


    Vector2 SnapVector(Vector2 vector2)
    {
        return new Vector2(
            SnapToThirdIfClose(vector2.x),
            SnapToThirdIfClose(vector2.y)
        );
    }




    [ContextMenu("RemoveObjects")]
    void RemoveObjects()
    {
        foreach (GameObject gameobject in generatedObjects)
        {
            RemoveObjectHelper.RemoveObject(gameobject);
        }
        generatedObjects.Clear();
    }

    private void MakeHexOffsets()
    {
        hexOffsets2.Clear();

        //floating point math
        float onethird = 1.0f / 3.0f;
        float twothird = 2.0f / 3.0f;
        if (playerMoveAlongZaxis)
        {
            hexOffsets2.Add(new Vector2(0f, twothird));
            hexOffsets2.Add(new Vector2(-0.5f, onethird));
            hexOffsets2.Add(new Vector2(-0.5f ,-onethird));
            hexOffsets2.Add(new Vector2(0f, -twothird));
            hexOffsets2.Add(new Vector2(0.5f, -onethird));
            hexOffsets2.Add(new Vector2(0.5f, onethird));
        }
        else
        {
            hexOffsets2.Add(new Vector2(twothird, 0f));
            hexOffsets2.Add(new Vector2(onethird, -0.5f));
            hexOffsets2.Add(new Vector2(-onethird, -0.5f));
            hexOffsets2.Add(new Vector2(-twothird, 0f));
            hexOffsets2.Add(new Vector2(-onethird, 0.5f));
            hexOffsets2.Add(new Vector2(onethird, 0.5f));
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
        for (int i = 0; i < areaSize; i++)
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

        Gizmos.color = Color.yellow;
        foreach (var item in hexToTri.Keys)
        {
            int number2 = theHexGrid.theGraph[item].neighborsTri.Count;

            for (int i = 0; i < number2; i++)
            {
                Vector3 origin = VectorConversion.vec2Tovec3(item);
                Vector3 destination = VectorConversion.vec2Tovec3(theHexGrid.theGraph[item].neighborsTri[i].position);
                //Gizmos.DrawLine(origin + Vector3.up * 20.2f, destination + Vector3.up * 20.4f);//i don't need this now
            }
        }

        Gizmos.color = Color.green;
        foreach (var item in triToHex.Keys)
        {
            int number2 = theHexGrid.theGraph[item].neighborsTri.Count;

            for (int i = 0; i < number2; i++)
            {
                Vector3 origin = VectorConversion.vec2Tovec3(item);
                Vector3 destination = VectorConversion.vec2Tovec3(theHexGrid.theGraph[item].neighborsTri[i].position);
                //Gizmos.DrawLine(origin + Vector3.up * 20.2f, destination + Vector3.up * 20.4f);//i don't need this now
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