using TMPro;
using UnityEngine;

/// <summary>The scale controls retain their own size so they stay usable.</summary>
[RequireComponent(typeof(UnityEngine.UI.Slider))]
public sealed class InterfaceScaleControl : MonoBehaviour
{
    public InterfaceScaleCategory category;
    public TMP_Text valueLabel;
    private UnityEngine.UI.Slider slider;
    private LocalizationManager localization;

    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        slider = GetComponent<UnityEngine.UI.Slider>();
        slider.minValue = InterfaceScaleManager.MinScale;
        slider.maxValue = InterfaceScaleManager.MaxScale;
        slider.wholeNumbers = false;
        slider.SetValueWithoutNotify(InterfaceScaleManager.Instance.GetScale(category));
        slider.onValueChanged.AddListener(Apply);
        InterfaceScaleManager.Instance.ScaleChanged += OnScaleChanged;
        SubscribeLocalization();
        RefreshLabel();
    }

    private void Start() { if (Application.isPlaying) SubscribeLocalization(); }
    private void SubscribeLocalization()
    {
        if (localization == LocalizationManager.Instance) return;
        if (localization != null) localization.OnLanguageChanged -= RefreshLabel;
        localization = LocalizationManager.Instance;
        if (localization != null) localization.OnLanguageChanged += RefreshLabel;
    }

    private void Apply(float value)
    {
        value = Mathf.Round(value * 100f) / 100f;
        slider.SetValueWithoutNotify(value);
        InterfaceScaleManager.Instance.SetScale(category, value);
        RefreshLabel();
    }

    private void OnScaleChanged(InterfaceScaleCategory changed, float value)
    {
        if (changed != category) return;
        slider.SetValueWithoutNotify(value);
        RefreshLabel();
    }

    private void RefreshLabel()
    {
        if (valueLabel == null || slider == null) return;
        int percent = Mathf.RoundToInt(slider.value * 100f);
        valueLabel.text = localization != null
            ? localization.GetTranslation("setting_ui_scale_percent", percent)
            : percent.ToString() + "%";
    }

    private void OnDisable()
    {
        if (slider != null) slider.onValueChanged.RemoveListener(Apply);
        if (InterfaceScaleManager.Current != null) InterfaceScaleManager.Current.ScaleChanged -= OnScaleChanged;
        if (localization != null) localization.OnLanguageChanged -= RefreshLabel;
        localization = null;
    }
}
