using UnityEngine;

public enum RotationAxis
{
    X, Y, Z, Custom
}

public class SunRotation : MonoBehaviour
{
    [SerializeField] private ScoreLogic scoreLogic;
    [SerializeField] private RotationAxis rotationAxis;

    [SerializeField] private float startAngle = 90f;
    [SerializeField] private float endAngle = -90f;

    private Vector3 rotationVector;

    private void Start()
    {
        transform.rotation = Quaternion.AngleAxis(startAngle, Vector3.forward);

        switch (rotationAxis)
        {
            case RotationAxis.X:
                rotationVector = Vector3.right;
                break;

            case RotationAxis.Y:
                rotationVector = Vector3.up;
                break;

            case RotationAxis.Z:
                rotationVector = Vector3.forward;
                break; 
            case RotationAxis.Custom:
                rotationVector = Vector3.up + Vector3.back * 0.125f;
                rotationVector.Normalize();
                break;
        }
    }

    private void Update()
    {
        if (scoreLogic.adjustedTime <= 0f) return;

        float progress = Mathf.Clamp01(scoreLogic.timer / scoreLogic.adjustedTime);
        float currentAngle = Mathf.Lerp(startAngle, endAngle, progress);

        transform.rotation = Quaternion.AngleAxis(currentAngle, rotationVector);
    }
}
