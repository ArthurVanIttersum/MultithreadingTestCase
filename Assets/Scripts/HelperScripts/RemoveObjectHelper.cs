using UnityEngine;

public class RemoveObjectHelper : MonoBehaviour
{
    public static void RemoveObject(GameObject toRemove)//remove an object either in playmode or in editormode
    {
        bool isPlaying = Application.isPlaying;

        if (toRemove == null) return;

        if (isPlaying)
            Destroy(toRemove);
        else
            DestroyImmediate(toRemove);
    }
}
