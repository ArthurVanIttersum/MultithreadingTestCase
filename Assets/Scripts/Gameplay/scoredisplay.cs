using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class scoredisplay : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI scoreDisplay;
    private string scoreText;
    public int topN = 10;
    private int topNUpdating;

    private void Start()
    {
        scoreText = "Scoreboard\n";
        topNUpdating = topN;

        // Ensure the SaveSystem exists before trying to read from it
        if (SaveSystem.Instance == null)
        {
            Debug.LogError("SaveSystem is missing from the scene!");
            return;
        }

        // Duplicate the list from our SaveSystem so we don't accidentally ruin the original order while sorting
        List<int> temporaryScoresList = new List<int>(SaveSystem.Instance.data.scoresList);

        if (temporaryScoresList.Count < topN)
        {
            topNUpdating = temporaryScoresList.Count;
        }

        // Sort descending (highest scores first)
        temporaryScoresList.Sort();
        temporaryScoresList.Reverse();

        for (int i = 0; i < topNUpdating; i++)
        {
            scoreText += "Rank: " + (i + 1).ToString() + " Score: " + temporaryScoresList[i].ToString() + "\n";
        }
        scoreDisplay.text = scoreText;
    }
}