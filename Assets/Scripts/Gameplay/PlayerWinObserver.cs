using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerWinObserver : PlayerObserver
{
    [SerializeField] Button restart;
    [SerializeField] TestMovement movement;

    [SerializeField] float coroutineTime = 600f;
    private Coroutine currentCoroutine;
    [SerializeField] GameObject prefabVillage;
    [SerializeField] Transform playerTransform;
    [SerializeField] Vector3 offsetFromPlayer;

    protected override void OnVSFX(int listIndex)
    { }

    protected override void OnScoreUpdate()
    { }

    protected override void OnTimeEnd()
    {
        //movement.enabled = false; //Disable the movement of the player
        currentCoroutine = StartCoroutine(Ending()); //Start the coroutine for the ending
        //restart.gameObject.SetActive(true);

        Debug.Log("Time finished");
    }

    public void Restart()
    {
        Scene scene = SceneManager.GetActiveScene(); 
        SceneManager.LoadScene(scene.buildIndex); //Load the current scene
    }

    IEnumerator Ending()
    {
        //code
        Instantiate(prefabVillage, playerTransform.position + offsetFromPlayer, Quaternion.identity);
        yield return new WaitForSeconds(coroutineTime);
        Restart();
        //code
        //yield return new WaitForSeconds(coroutineTime);
    }
}
