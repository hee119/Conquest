using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public sealed class SceneBrightnessController : MonoBehaviour
{
    private const string PreferenceKey = "SceneBrightness";
    private const float MinExposure = -2f;
    private const float MaxExposure = 2f;
    private const float DefaultSliderValue = 0.5f;

    private Slider slider;
    private ColorAdjustments colorAdjustments;
    private Image screenOverlay;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        Volume volume = FindFirstObjectByType<Volume>();
        if (volume == null)
        {
            Debug.LogWarning("Scene brightness requires a Volume in the active scene.", this);
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
        slider.SetValueWithoutNotify(
            PlayerPrefs.GetFloat(PreferenceKey, DefaultSliderValue)
        );
        CreateScreenOverlay();
    }

    private void OnEnable()
    {
        if (slider == null)
        {
            return;
        }

        slider.onValueChanged.AddListener(ApplyBrightness);
        ApplyBrightness(slider.value);
    }

    private void OnDisable()
    {
        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(ApplyBrightness);
        }
    }

    private void ApplyBrightness(float normalizedValue)
    {
        if (colorAdjustments == null || screenOverlay == null)
        {
            return;
        }

        normalizedValue = Mathf.Clamp01(normalizedValue);
        colorAdjustments.postExposure.value = Mathf.Lerp(
            MinExposure,
            MaxExposure,
            normalizedValue
        );

        float overlayAmount = Mathf.Abs(normalizedValue - DefaultSliderValue) * 2f;
        Color overlayColor = normalizedValue < DefaultSliderValue ? Color.black : Color.white;
        overlayColor.a = overlayAmount * 0.5f;
        screenOverlay.color = overlayColor;
        PlayerPrefs.SetFloat(PreferenceKey, normalizedValue);
    }

    private void CreateScreenOverlay()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("Scene brightness requires a Canvas in the parent hierarchy.", this);
            enabled = false;
            return;
        }

        GameObject overlayObject = new GameObject(
            "Scene Brightness Overlay",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(Image)
        );
        overlayObject.transform.SetParent(canvas.rootCanvas.transform, false);

        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Canvas overlayCanvas = overlayObject.GetComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = short.MaxValue;

        screenOverlay = overlayObject.GetComponent<Image>();
        screenOverlay.raycastTarget = false;
        screenOverlay.color = new Color(0f, 0f, 0f, 0f);
    }
}
