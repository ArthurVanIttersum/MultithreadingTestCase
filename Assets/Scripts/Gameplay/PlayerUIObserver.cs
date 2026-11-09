using TMPro;
using UnityEngine;

public class PlayerUIObserver : PlayerObserver
{
    [SerializeField] TextMeshProUGUI score;
    [SerializeField] TextMeshProUGUI timer;

    //Add the proper UI elements to showcase when a player loses or wins
    //[SerializeField] private List<GameObject> images = new List<GameObject>();

    protected override void OnVSFX(int listIndex)
    {
        if (listIndex == 0)
        {
            //images[0].gameObject.SetActive(true);
            Debug.Log("Score lowered");
        }

        else
        {
            //images[1].gameObject.SetActive(true);
            Debug.Log("Score added");
        }
    }

    protected override void OnScoreUpdate()
    {
        if (detectCollision.scoreLogic.score <= 0)
            detectCollision.scoreLogic.score = 0;

        score.text = $"Score: {(int)detectCollision.scoreLogic.score}";
        float remaining = detectCollision.scoreLogic.adjustedTime - detectCollision.scoreLogic.timer;
        int minutes = (int)(remaining / 60);
        int seconds = (int)(remaining % 60);
        timer.text = $"{minutes:D2}:{seconds:D2}";
    }

    protected override void OnTimeEnd()
    {}
}
