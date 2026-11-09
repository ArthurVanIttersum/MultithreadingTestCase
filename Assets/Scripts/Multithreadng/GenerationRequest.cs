using Unity.Collections;
using Unity.Jobs;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

public class GenerationRequest
{
    public GameObject chunk;
    public WorkerThread jobData;
    public JobHandle handle;

    public NativeArray<int> result;
    public NativeArray<int> grassResult;
    public NativeArray<int> coinResult;
    public NativeArray<int> coinNeighborCount;
    public NativeArray<int> indices;
    public NativeArray<int> indices2;
    public NativeQueue<Vector2> toBeExplored;
    public NativeHashSet<Vector2> discovered;
    public NativeHashSet<Vector2> obstructed;
    
    bool disposed = false;

    public GenerationRequest(GameObject chunk)
    {
        this.chunk = chunk;
        
    }

    public void Dispose()
    {
        if (disposed) return;

        handle.Complete();

        result.Dispose();
        grassResult.Dispose();
        coinResult.Dispose();
        coinNeighborCount.Dispose();
        indices.Dispose();
        indices2.Dispose();
        toBeExplored.Dispose();
        discovered.Dispose();
        obstructed.Dispose();
        disposed = true;
    }
}
