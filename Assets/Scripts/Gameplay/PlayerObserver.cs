using UnityEngine;

public abstract class PlayerObserver : MonoBehaviour
{
    [SerializeField] protected DetectCollision detectCollision;

    protected void OnEnable()
    {
        detectCollision.vsfx += OnVSFX;
        detectCollision.scoreLogic.timeEnded += OnTimeEnd;
        detectCollision.scoreLogic.scoreUpdate += OnScoreUpdate;
    }

    protected void OnDisable()
    {
        detectCollision.vsfx -= OnVSFX;
        detectCollision.scoreLogic.timeEnded -= OnTimeEnd;
        detectCollision.scoreLogic.scoreUpdate -= OnScoreUpdate;
    }

    protected abstract void OnVSFX(int listIndex);

    protected abstract void OnTimeEnd();

    protected abstract void OnScoreUpdate();
}
