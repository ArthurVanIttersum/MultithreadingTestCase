using UnityEngine;
using UnityEngine.SceneManagement;

public class rendertexture : MonoBehaviour
{
    public string ewlinewi;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SceneManager.LoadScene(ewlinewi);
    }

    
}
