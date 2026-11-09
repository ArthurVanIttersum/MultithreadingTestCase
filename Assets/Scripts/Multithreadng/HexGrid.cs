using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
//datastructure for the hexx grid.
//all ode related to setting up a hex grid and the data itself are stored in here.
//pathfinding is in the graph class.
//obstacle data is stored elsewhere.
public class HexGrid
{
    //datastructures

    //settings Files
    [SerializeField] private BiomeSelection biomeSelection;
    public ReworkedHoneyGenerationSettings generationSettings;
    public ChunkSettings chunkSettings;

    //booleans
    public bool playerMoveAlongZaxis;
    public bool displayDebugVisualization;
    public bool enableDebugprints;

    //datastructures
    public Vector2Int areaMax;
    public Vector2Int areaMin;

    //dictionaries
    public Dictionary<Vector2, List<Vector2>> triToHex = new();
    public Dictionary<Vector2, List<Vector2>> hexToTri = new();
    public Dictionary<Vector2, List<Vector2>> triToTri = new();
    public Dictionary<Vector2, List<Vector2>> neighbors = new();

    public NativeParallelMultiHashMap<Vector2, Vector2> NtriToHex;
    public NativeParallelMultiHashMap<Vector2, Vector2> NhexToTri;
    public NativeParallelMultiHashMap<Vector2, Vector2> NtriToTri;
    public NativeParallelMultiHashMap<Vector2, Vector2> Neighbors;

    //lists
    public HashSet<Vector2> startingPoints = new();
    public HashSet<Vector2> destinationPoints = new();
    private List<Vector2> hexOffsets2 = new();

    public NativeHashSet<Vector2> NstartingPoints = new();
    public NativeHashSet<Vector2> NdestinationPoints = new();



    public void CacheData()
    {
        MakeHexOffsets();
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

        //make grid

        //initiate nodes
        foreach (var item in hexToTri.Keys)//hexnodes
        {
            neighbors.Add(item, new List<Vector2>());
        }
        foreach (var item in triToHex.Keys)//trinodes
        {
            neighbors.Add(item, new List<Vector2>());
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
                    neighbors[pos0].Add(pos1);
                }
                if (VectorCompareDirectional(pos0, pos1n))
                {
                    neighbors[pos0].Add(pos1n);
                }
                if (VectorCompareDirectional(pos0, item))
                {
                    neighbors[pos0].Add(item);
                }
                if (VectorCompareDirectional(item, pos0))
                {
                    neighbors[item].Add(pos0);
                }
            }
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

    public void MakeNativeStructures()
    {
        NtriToHex = new(triToHex.Count, Allocator.Persistent);
        NhexToTri = new(hexToTri.Count, Allocator.Persistent);
        NtriToTri = new(triToTri.Count, Allocator.Persistent);
        Neighbors = new(neighbors.Count, Allocator.Persistent);
        NstartingPoints = new(startingPoints.Count, Allocator.Persistent);
        NdestinationPoints = new(destinationPoints.Count, Allocator.Persistent);
        foreach (var tri in triToHex.Keys)
        {
            foreach (var hex in triToHex[tri])
            {
                NtriToHex.Add(tri, hex);
            }
        }
        foreach (var hex in hexToTri.Keys)
        {
            foreach (var tri in hexToTri[hex])
            {
                NhexToTri.Add(hex, tri);
            }
        }
        foreach (var tri1 in triToTri.Keys)
        {
            foreach (var tri2 in triToTri[tri1])
            {
                NtriToTri.Add(tri1, tri2);
            }
        }
        foreach (var pos in startingPoints)
        {
            NstartingPoints.Add(pos);
        }
        foreach (var pos in destinationPoints)
        {
            NdestinationPoints.Add(pos);
        }
    }

}
