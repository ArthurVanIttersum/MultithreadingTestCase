using System.IO;
using System.Collections.Generic;
using UnityEngine;

// This replaces your ScriptableObject data structures
[System.Serializable]
public class GameSaveData
{
    public string savedPortName = "COM3"; // Default fallback port
    public List<int> scoresList = new List<int>();
}

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    public GameSaveData data = new GameSaveData();
    private string saveFilePath;

    private void Awake()
    {
        // Keeps the SaveSystem alive across scene transitions
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            saveFilePath = Path.Combine(Application.persistentDataPath, "project_save.json");
            LoadGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveGame()
    {
        string jsonText = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFilePath, jsonText);
        Debug.Log("Game data saved successfully to build directory.");
    }

    public void LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            string jsonText = File.ReadAllText(saveFilePath);
            data = JsonUtility.FromJson<GameSaveData>(jsonText);
            Debug.Log("Game data loaded successfully from build directory.");
        }
        else
        {
            Debug.Log("No save file found. Initializing with defaults.");
            SaveGame();
        }
    }
}