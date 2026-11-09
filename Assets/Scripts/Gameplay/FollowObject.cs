using UnityEngine;

public class FollowObject : MonoBehaviour
{
    public GameObject objectToFollow;

    // Update is called once per frame
    void Update()
    {
        gameObject.transform.position = objectToFollow.transform.position;
    }
}
