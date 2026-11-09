using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartRestart : MonoBehaviour
{
    [SerializeField] private TestMovement testMovement;
    [SerializeField] private float timeToStartRestart;
    [SerializeField] string sceneName;
    private float timer;

    private void Update()
    {
        if (testMovement.player1Active && testMovement.player2Active)
        {
            timer += Time.deltaTime;

            if (timer >= timeToStartRestart)
            {
                RestartScene();
            }
        }
        else
        {
            timer = 0f;
        }
    }

    private void RestartScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
