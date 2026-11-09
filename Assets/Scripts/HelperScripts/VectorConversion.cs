using UnityEngine;


// this script is used to convert between different types of vectors

/*
       int           not int

vec2   vec2int <---> vec2
           ^    \ /   ^
           |     X    |
           v    / \   v
vec3   vec3int <---> vec3
 */



public class VectorConversion : MonoBehaviour
{
    //horizontal
    //naive translation between int and notint
    public static Vector2Int vec2Tovec2Int(Vector2 vec2Tovec2Int)
    {
        return new Vector2Int((int)vec2Tovec2Int.x, (int)vec2Tovec2Int.y);
    }

    public static Vector3Int vec3Tovec3Int(Vector3 vec3Tovec3Int)
    {
        return new Vector3Int((int)vec3Tovec3Int.x, (int)vec3Tovec3Int.y, (int)vec3Tovec3Int.z);
    }

    public static Vector2 vec2IntTovec2(Vector2Int vec2IntTovec2)
    {
        return new Vector2(vec2IntTovec2.x, vec2IntTovec2.y);
    }
    public static Vector3 vec3IntTovec3(Vector3Int vec3IntTovec3)
    {
        return new Vector3(vec3IntTovec3.x, vec3IntTovec3.y, vec3IntTovec3.z);
    }

    //vertical
    //assuming 2D is top down and 3D is worldspace
    public static Vector2Int vec3IntTovec2Int(Vector3Int vec3IntTovec2Int)
    {
        return new Vector2Int(vec3IntTovec2Int.x, vec3IntTovec2Int.z);
    }

    public static Vector3Int vec2IntTovec3Int(Vector2Int vec2IntTovec3Int)
    {
        return new Vector3Int(vec2IntTovec3Int.x, 0, vec2IntTovec3Int.y);
    }

    public static Vector2 vec3Tovec2(Vector3 vec3Tovec2)
    {
        return new Vector2(vec3Tovec2.x, vec3Tovec2.z);
    }

    public static Vector3 vec2Tovec3(Vector2 vec2Tovec3)
    {
        return new Vector3(vec2Tovec3.x, 0, vec2Tovec3.y);
    }

    //diagonals
    public static Vector3 vec2IntTovec3(Vector2Int vec2IntTovec3)
    {
        return new Vector3(vec2IntTovec3.x, 0, vec2IntTovec3.y);
    }

    public static Vector3Int vec2Tovec3Int(Vector2 vec2Tovec3Int)
    {
        return new Vector3Int((int)vec2Tovec3Int.x, 0, (int)vec2Tovec3Int.y);
    }

    public static Vector2 vec3IntTovec2(Vector3Int vec3IntTovec2)
    {
        return new Vector2(vec3IntTovec2.x, vec3IntTovec2.y);
    }

    public static Vector2Int vec3Tovec2Int(Vector3 vec3Tovec2Int)
    {
        return new Vector2Int((int)vec3Tovec2Int.x, (int)vec3Tovec2Int.z);
    }
}
