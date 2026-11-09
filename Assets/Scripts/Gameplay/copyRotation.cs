using UnityEngine;

public class copyRotation : MonoBehaviour
{
    public GameObject objectToCoppyRotationFrom;

    // Update is called once per frame
    void Update()
    {
        gameObject.transform.rotation = objectToCoppyRotationFrom.transform.rotation;
    }
}
