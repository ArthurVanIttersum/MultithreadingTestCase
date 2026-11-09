using System;
using UnityEngine;

public class DetectCollision : MonoBehaviour
{
    public ScoreLogic scoreLogic;

    public event Action<int> vsfx;

    ///To be deleted
    //settings 
    public GenerationSettings settingsFile;
    ///

    public ObjectInformation objectInformation;

    private void OnTriggerEnter(Collider other)
    {
        ObstacleType obstacleType = other.gameObject.GetComponent<ObstacleType>();

        if (obstacleType == null)
            return;

        int listIndex = other.GetComponent<ObstacleType>().listIndex;
        RemoveObjectHelper.RemoveObject(other.gameObject);

        /*
        if (settingsFile == null) return;
        var theType = settingsFile.objects[listIndex].type;
        float theValue = settingsFile.objects[listIndex].value;

        if (theType == Type.Damage)
        {
            scoreLogic.score -= (int)theValue;
            vsfx?.Invoke(listIndex);
            scoreLogic.InvokeScoreUpdate();
            print("doing damage" + "index:" + listIndex + " the value: " + theValue);

        }

        if (theType == Type.Health)
        {
            scoreLogic.score += (int)theValue;
            vsfx?.Invoke(listIndex);
            scoreLogic.InvokeScoreUpdate();
            print("doing health" + "index:" + listIndex + " the value: " + theValue);
        }
        */

        if (objectInformation != null && listIndex <= objectInformation.objects.Count)
        {
            var info = objectInformation.objects[listIndex];

            if (info.kind == Kind.Add)
            {
                scoreLogic.score += (int)info.value;
                scoreLogic.InvokeScoreUpdate();
                vsfx?.Invoke(listIndex);
            }

            if (info.kind == Kind.Subtract)
            {
                scoreLogic.score -= (int)info.value;
                scoreLogic.InvokeScoreUpdate();
                vsfx?.Invoke(listIndex);
            }
        }
    }
}
