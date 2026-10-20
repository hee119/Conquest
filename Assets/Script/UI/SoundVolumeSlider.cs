using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public sealed class SoundVolumeSlider : MonoBehaviour
{
    public enum VolumeChannel
    {
        Master,
        Bgm,
        Sfx
    }

    [SerializeField] private VolumeChannel channel;

    private Slider slider;

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        slider = GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;

        SoundManager manager = SoundManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("Sound settings require a SoundManager in the scene.", this);
            return;
        }

        // Update the neighboring percent label through the slider's value event.
        switch (channel)
        {
            case VolumeChannel.Master:
                slider.value = manager.MasterVolume;
                break;
            case VolumeChannel.Bgm:
                slider.value = manager.BgmVolume;
                break;
            case VolumeChannel.Sfx:
                slider.value = manager.SfxVolume;
                break;
        }

    }

    public void ApplyVolume(float value)
    {
        SoundManager manager = SoundManager.Instance;
        if (manager == null)
        {
            return;
        }

        switch (channel)
        {
            case VolumeChannel.Master:
                manager.SetMasterVolume(value);
                break;
            case VolumeChannel.Bgm:
                manager.SetBgmVolume(value);
                break;
            case VolumeChannel.Sfx:
                manager.SetSfxVolume(value);
                break;
        }
    }
}
