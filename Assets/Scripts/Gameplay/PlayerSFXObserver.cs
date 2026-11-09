using UnityEngine;

public class PlayerSFXObserver : PlayerObserver
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private ObjectInformation objectInformation;

    protected override void OnVSFX(int listIndex)
    {
        if (objectInformation == null)
        {
            Debug.LogError($"ObjectInformations is missing");
            return;
        }

        AudioClip clip = objectInformation.objects[listIndex].clip;

        if (clip == null)
        {
            Debug.LogError($"{objectInformation.objects[listIndex]} does not have a audio clip assigned to it.");
            return;
        }

        if (!audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        audioSource.PlayOneShot(clip);

        Debug.Log($"Playing clip for {clip}");
    }

    protected override void OnTimeEnd()
    {}

    protected override void OnScoreUpdate()
    {}
}
