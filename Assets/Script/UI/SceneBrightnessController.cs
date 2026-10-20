using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public sealed class SceneBrightnessController : MonoBehaviour
{
    private const string PreferenceKey = "SceneBrightness";
    private const float MinExposure = -1f;
    private const float MaxExposure = 1f;
    private const float DefaultSliderValue = 0.5f;

    private Slider slider;
    private ColorAdjustments colorAdjustments;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        if (GetComponent<SoundVolumeSlider>() != null)
        {
            enabled = false;
            return;
        }

        Volume volume = null;
        foreach (Volume candidate in FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            if (candidate.isGlobal)
            {
                volume = candidate;
                break;
            }
        }

        if (volume == null)
        {
            Debug.LogWarning("Scene brightness requires a Global Volume in the active scene.", this);
            enabled = false;
            return;
        }

        VolumeProfile profile = volume.profile;
        if (!profile.TryGet(out colorAdjustments))
        {
            colorAdjustments = profile.Add<ColorAdjustments>(true);
        }

        colorAdjustments.postExposure.overrideState = true;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = Mathf.Clamp01(PlayerPrefs.GetFloat(PreferenceKey, DefaultSliderValue));
    }

    private void OnEnable()
    {
        if (slider == null)
        {
            return;
        }

        ApplyBrightness(slider.value);
    }

    public void ApplyBrightness(float normalizedValue)
    {
        if (colorAdjustments == null)
        {
            return;
        }

        normalizedValue = Mathf.Clamp01(normalizedValue);
        colorAdjustments.postExposure.value = Mathf.Lerp(
            MinExposure,
            MaxExposure,
            normalizedValue
        );
        PlayerPrefs.SetFloat(PreferenceKey, normalizedValue);
    }
}
