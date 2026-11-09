using UnityEngine;

[CreateAssetMenu(fileName = "Playtime", menuName = "Scriptable Objects/Playtime")]

//Valus related to playtime mechanics 
public class PlaytimeValues : ScriptableObject
{
    [Tooltip("This value is in minutes")]
    public float playTime = 1; //in minutes
    public float scoreMultiplier = 1; //How much score is affected by time (ex. 1 -> score goes up by 1 every second; 0.5 -> score goes up by 0.5 every second)
    public float ballScoreRatio; //Compared to the score how big the ball gets (ex. 1 -> 1 score = 1 unit increase of the ball in every dimention; 0.01 -> 100 score = 1 unit increase of the ball in every dimention)
    public float maxBallSize; //The maximum size of the ball. Past this point, the point would not grow no matter the score. 
    public float maxClampScore; //What is the maximum value of the clamped score. This value should be the product of ballScoreRatio and maxBallSize (ex. 0.01 * 2 -> 200 maxClampScore).
                                //If is less than that, the ball will never reach it's max size. In that example the maxBallSize multiplication should be 1 less than he variable itself,
                                //because the ball itself start at a scale of 1,1,1 and maxBallSize show the final size - 3,3,3. This means that the scale of the ball needs to change by 2,
                                //which in the current setup would be 200 score, meaning that to reach the max size of the ball of 3,3,3 the clamped score needs to be 200. 
    public float cameraZAxisMultiplier; //Both z and y axis move by 2 units. This is a multipler to change the z axis more compared to y. 
    public float massScoreRatio; //Compared to the score how the mass of the ball changes (ex. 1 -> 1 score = 1 unit increase of the mass; 0.01 -> 100 score = 1 unit increase of the mass)
    public float maxMass; //How much mass can the ball have
}
