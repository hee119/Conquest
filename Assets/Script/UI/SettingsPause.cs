using UnityEngine;

public sealed class SettingsPause : MonoBehaviour
{
    private float previousTimeScale = 1f;
    private bool pauseApplied;

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        pauseApplied = true;
    }

    private void OnDisable()
    {
        if (!Application.isPlaying || !pauseApplied)
            return;

        Time.timeScale = previousTimeScale;
        pauseApplied = false;
    }
}
