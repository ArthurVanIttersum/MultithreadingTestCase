using UnityEngine;

public class AnimationSpeedBall : MonoBehaviour
{
    public Animator animator;
    public float multiplier = 0.1f;
    private TestMovement movementScript;

    public void SetAnimationSpeed(float speed)
    {
        animator.SetFloat("Speed", speed * multiplier);
    }

    private void OnEnable()
    {
        movementScript = FindAnyObjectByType<TestMovement>();
        if (movementScript != null)
            movementScript.animationSpeed += SetAnimationSpeed;
    }

    private void OnDisable()
    {
        if (movementScript != null)
            movementScript.animationSpeed -= SetAnimationSpeed;
    }
}
