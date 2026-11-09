using UnityEngine;

public class Particlestuff : MonoBehaviour
{
    public GameObject prefabWithParticleSystem;

    public void SpawnParticleSystem()
    {
        GameObject spawned = Instantiate(prefabWithParticleSystem, transform.position, Quaternion.identity);
        spawned.GetComponent<ParticleSystem>().Play();
        spawned.GetComponent<ParticleEnd>().startTheCoroutine();
    }

    private void OnTriggerEnter(Collider other)
    {
        SpawnParticleSystem();
    }
}
