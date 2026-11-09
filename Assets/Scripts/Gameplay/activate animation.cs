using UnityEngine;

public class ActivateAnimation : MonoBehaviour
{
    public Animator animator1;
    public Animator animator2;
    
    public void ActivateAnimation1()
    {
        animator1.SetTrigger("Push");
    }

    public void ActivateAnimation2()
    {
        animator2.SetTrigger("Push");
    }


    private void OnEnable()
    {
        TestMovement script = FindAnyObjectByType<TestMovement>();
        script.player1Press += ActivateAnimation1;
        script.player2Press += ActivateAnimation2;
    }
}
