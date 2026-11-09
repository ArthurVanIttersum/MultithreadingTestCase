using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

[System.Serializable]
public class Grid
{
    public Dictionary<Vector2, Node> theGraph = new();

    //caching
    public List<Vector2> path;
    private Queue<Node> toBeExplored = new(100);
    private HashSet<int> discovered = new(100);
    private Node currentNode;
    

    //spacial caching
    public HashSet<int> destinationIDs = new(100);


    
public bool BFS(Vector2 startingPosition, HashSet<Vector2> destinations, out List<Vector2> path, bool playerMoveAlongZaxis)
    {
        Node currentNode = theGraph[startingPosition];
        Queue<Node> toBeExplored = new();
        HashSet<Vector2> discovered = new();
        toBeExplored.Enqueue(currentNode);
        discovered.Add(currentNode.position);
        while (toBeExplored.Count > 0)
        {
            currentNode = toBeExplored.Dequeue();

            
            foreach (Node foundHex in currentNode.neighborsHex)
            {
                if (destinations.Contains(foundHex.position))
                {
                    //Debug.Log("found destination node");
                    //Debug.Log(discovered.Count);
                    path = discovered.ToList();
                    return true;
                }
                if (discovered.Contains(foundHex.position)) continue;
                if (foundHex.obstructed) continue;
                if (playerMoveAlongZaxis)
                {
                    if (foundHex.position.y < currentNode.position.y) continue;
                }
                else
                {
                    if (foundHex.position.x < currentNode.position.x) continue;
                }

                toBeExplored.Enqueue(foundHex);
                discovered.Add(foundHex.position);
            }
            if (toBeExplored.Count > 1000)
            {
                Debug.Log("warning BFS problem");
                //Debug.Log(discovered.Count);
                path = discovered.ToList();
                return false;
            }
        }
        //Debug.Log("did not find destination node");
        //Debug.Log(discovered.Count);
        path = discovered.ToList();
        return false;
    }

    public bool ReworkedBFS(Vector2 startingPosition, bool playerMoveAlongZaxis)
    {
        currentNode = theGraph[startingPosition];
        toBeExplored.Clear();
        discovered.Clear();

        toBeExplored.Enqueue(currentNode);
        discovered.Add(currentNode.iD);
        while (toBeExplored.Count > 0)
        {
            currentNode = toBeExplored.Dequeue();


            foreach (Node foundHex in currentNode.neighborsHex)
            {
                if (destinationIDs.Contains(foundHex.iD))
                {
                    return true;
                }
                if (discovered.Contains(foundHex.iD)) continue;
                if (foundHex.obstructed) continue;


                toBeExplored.Enqueue(foundHex);
                discovered.Add(foundHex.iD);
            }
            if (toBeExplored.Count > 1000)
            {
                Debug.Log("warning BFS problem");
                
                return false;
            }
        }
        
        return false;
    }
}

[System.Serializable]
public class Node
{
    //general data
    public Vector2 position;
    public List<Node> neighborsHex;
    public List<Node> neighborsTri;
    public int spawnedIndex = -1;
    public List<int> spawnchance = new();
    public int iD;

    //pathfinding
    public bool obstructed = false;

    public Node(Vector2 pos, int iD)
    {
        position = pos;
        neighborsHex = new();
        this.iD = iD;
    }
}

