using NUnit.Framework;
using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class FPSDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreDisplay;
    private Dictionary<int, float> rollingAverage = new();
    private int count = 0;
    private float totalTime = 0;
    public int framesToKeep = 10;

    // Update is called once per frame
    void Update()
    {
        count++;
        float deltatime = Time.deltaTime;
        
        rollingAverage.Add(count, deltatime);
        if (rollingAverage.Count > framesToKeep)
        {
            rollingAverage.Remove(count - framesToKeep);
        }
        totalTime = 0;
        foreach (float deltaTime in rollingAverage.Values)
        {
            totalTime += deltaTime;
        }
        totalTime /= rollingAverage.Count;
        scoreDisplay.text = "Current FPS\n" + (1 / deltatime).ToString() + "\nAverage FPS:\n" + (1 / totalTime).ToString();
    }
}
