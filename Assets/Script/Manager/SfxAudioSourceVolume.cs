using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public sealed class SfxAudioSourceVolume : MonoBehaviour
{
    private AudioSource audioSource;
    private float baseVolume;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        baseVolume = audioSource.volume;
    }

    private void OnEnable()
    {
        SoundManager manager = SoundManager.Instance;
        if (manager == null)
        {
            return;
        }

        manager.SfxVolumeChanged += ApplyVolume;
        ApplyVolume(manager.SfxVolume);
    }

    private void OnDisable()
    {
        SoundManager manager = SoundManager.Instance;
        if (manager != null)
        {
            manager.SfxVolumeChanged -= ApplyVolume;
        }

        if (audioSource != null)
        {
            audioSource.volume = baseVolume;
        }
    }

    private void ApplyVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = baseVolume * volume;
        }
    }
}
