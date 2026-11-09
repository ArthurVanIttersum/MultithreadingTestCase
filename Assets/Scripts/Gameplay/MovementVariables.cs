using UnityEngine;

[CreateAssetMenu(fileName = "MovementVariables", menuName = "Scriptable Objects/MovementVariables")]
public class MovementVariables : ScriptableObject
{
    public float force = 10f; //used for forward and side movement
    public float maxSpeed = 15f;
    public float deceleraionRate = 2f; //how much speed per second is lost
    public float degreesPerClick = 1f;
    public float maxDegree = 1f; //how much to the left or right the object can rotate
    [Range(0, 1f)] public float thresholdPercentage = 0.3f; //at what percentage of maxDegree, degreesPerSec starts to drop rapedly
    public float exponentialStrength = 5f; //Controls steepness of the drop-off curve after the threshold (higher = steeper)

    [Range(0, 1f)] public float pressThreshold = 0.8f;
    [Range(0, 1f)] public float releaseThreshold = 0.2f;
}
