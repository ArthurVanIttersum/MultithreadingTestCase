using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using Unity.Mathematics;
using System;

public class ProceduralGenerationProtoType : MonoBehaviour
{
    public GenerationSettings settingsFile;
    public List<GameObject> generatedObjects;
    public float areaSize = 10.0f;
    

    [ContextMenu("GenerateEnvironment")]
    void GenerateEnvironment()
    {
        print("Removing Objects");
        RemoveObjects();
        print("Generating Environment");
        if (settingsFile == null) return;
        
        foreach (var item in settingsFile.objects)
        {
            for (int i = 0; i < item.objectCount; i++)
            {
                Vector3 position = new Vector3(
                    UnityEngine.Random.Range(-areaSize, areaSize),
                    0,
                    UnityEngine.Random.Range(-areaSize, areaSize)
                );
                generatedObjects.Add(Instantiate(item.prefab, position + transform.position, Quaternion.identity, transform));
            }
        }
    }

    
    void RemoveObjects()
    {
        foreach (GameObject gameobject in generatedObjects)
        {
            RemoveObjectHelper.RemoveObject(gameobject);
        }
        generatedObjects.Clear();
    }

}
