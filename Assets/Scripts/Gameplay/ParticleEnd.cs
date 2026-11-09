using System.Collections;
using UnityEngine;

public class ParticleEnd : MonoBehaviour
{
    public ParticleSystem particleSystem;
    public float timeBeforeEnd;


    public void startTheCoroutine()
    {
        StartCoroutine(DeleteIt(timeBeforeEnd));
    }


    private IEnumerator DeleteIt(float time)
    {


        yield return new WaitForSeconds(time);
        Destroy(gameObject);
    }
}
