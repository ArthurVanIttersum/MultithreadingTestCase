using UnityEngine;
using UnityEngine.SceneManagement;

public class sceneChanger : MonoBehaviour
{
    [SerializeField] string scenename;
    private ScoreLogic logic;


    private void Start()
    {
        logic = FindAnyObjectByType<ScoreLogic>();
    }
    private void OnTriggerEnter(Collider other)
    {
        logic.PublishScore();
        SceneManager.LoadScene(scenename);
    }
}
