using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;

/// <summary>
/// TEMPORARY test script for cycling through static end-scene cameras.
/// Uses legacy Input (Input.GetKeyDown) for quick testing — NOT wired to
/// the project's Input System actions yet. Hand off to engineers to
/// integrate properly with DefaultInputActions when ready.
/// </summary>
public class EndSceneCameraController : MonoBehaviour
{
    [Header("Assign all vignette cameras in the order you want them cycled")]
    [SerializeField] private List<CinemachineCamera> vignetteCameras;

    [Header("Priority values")]
    [SerializeField] private int activePriority = 10;
    [SerializeField] private int inactivePriority = 0;

    [SerializeField] private TestMovement testMovement;

    private bool previousPlayer1Active;
    private bool previousPlayer2Active;

    private int currentIndex = 0;

    private void OnEnable()
    {
        if (testMovement != null)
        {
            testMovement.player1Press += OnPrevious;
            testMovement.player2Press += OnNext;
        }
    }

    private void OnDisable()
    {
        if (testMovement != null)
        {
            testMovement.player1Press -= OnPrevious;
            testMovement.player2Press -= OnNext;
        }
    }

    private void Start()
    {
        if (vignetteCameras == null || vignetteCameras.Count == 0)
        {
            Debug.LogWarning("[EndSceneCameraController] No cameras assigned in the list.");
            return;
        }

        SetActiveCamera(0);
    }

    private void Update()
    {
        if (vignetteCameras == null || vignetteCameras.Count == 0) return;
    }

    public void OnNext()
    {
        currentIndex = (currentIndex + 1) % vignetteCameras.Count;
        SetActiveCamera(currentIndex);
    }

    public void OnPrevious()
    {
        currentIndex = (currentIndex - 1 + vignetteCameras.Count) % vignetteCameras.Count;
        SetActiveCamera(currentIndex);
    }

    private void SetActiveCamera(int index)
    {
        for (int i = 0; i < vignetteCameras.Count; i++)
        {
            vignetteCameras[i].Priority = (i == index) ? activePriority : inactivePriority;
        }

        Debug.Log($"[EndSceneCameraController] Active camera: {index} ({vignetteCameras[index].name})");
    }
}