using UnityEngine;

public sealed class SettingsPause : MonoBehaviour
{
    private float previousTimeScale = 1f;
    private bool pauseApplied;
    private SoundManager soundManager;

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        pauseApplied = true;

        soundManager = SoundManager.Instance;
        if (soundManager != null)
            soundManager.BeginSettingsAudio();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying || !pauseApplied)
            return;

        Time.timeScale = previousTimeScale;
        pauseApplied = false;

        if (soundManager != null)
            soundManager.EndSettingsAudio();

        soundManager = null;
        PlayerPrefs.Save();
    }
}