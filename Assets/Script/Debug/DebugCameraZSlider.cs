using TMPro;
using System.Globalization;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Controls the orthographic lens size from the Debug canvas.</summary>
public sealed class DebugCameraZSlider : MonoBehaviour
{
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private TMP_FontAsset labelFont;
    [SerializeField] private float minimumLensSize = 1f;
    [SerializeField] private float maximumLensSize = 30f;
    [SerializeField] private Vector2 anchoredPosition = new Vector2(-843f, -263f);
    [SerializeField] private float controlWidth = 200f;

    private TextMeshProUGUI valueLabel;
    private LocalizationManager subscribedLocalization;

    public float CurrentLensSize
    {
        get
        {
            if (virtualCamera == null)
                virtualCamera = FindFirstObjectByType<CinemachineCamera>();

            return virtualCamera != null ? virtualCamera.Lens.OrthographicSize : 5f;
        }
    }

    public void SetLensSize(float lensSize)
    {
        ApplyLensSize(Mathf.Clamp(lensSize, minimumLensSize, maximumLensSize));
    }

    private void Awake()
    {
        if (virtualCamera == null)
            virtualCamera = FindFirstObjectByType<CinemachineCamera>();

        RectTransform parent = transform.Find("Safe Area") as RectTransform;
        if (parent == null)
            parent = transform as RectTransform;

        if (virtualCamera == null || parent == null)
        {
            Debug.LogWarning("[DebugCameraLensSlider] 카메라 또는 Debug Canvas를 찾지 못했습니다.", this);
            return;
        }

        if (labelFont == null)
        {
            TMP_Text existingLabel = parent.GetComponentInChildren<TMP_Text>(true);
            if (existingLabel != null)
                labelFont = existingLabel.font;
        }

        Transform existingSlider = parent.Find("Camera Lens Slider");
        if (existingSlider != null && existingSlider.TryGetComponent(out Slider slider))
        {
            Transform existingLabel = parent.Find("Camera Lens Value");
            valueLabel = existingLabel != null ? existingLabel.GetComponent<TextMeshProUGUI>() : null;
            ConfigureSlider(slider);
            return;
        }

        CreateSlider(parent);
    }

    private void OnEnable()
    {
        SubscribeToLocalization();
    }

    private void Start()
    {
        SubscribeToLocalization();
        RefreshValueLabel();
    }

    private void OnDisable()
    {
        if (subscribedLocalization != null)
        {
            subscribedLocalization.OnLanguageChanged -= RefreshValueLabel;
            subscribedLocalization = null;
        }
    }

    private void SubscribeToLocalization()
    {
        LocalizationManager manager = LocalizationManager.Instance;
        if (manager == null || manager == subscribedLocalization)
            return;

        if (subscribedLocalization != null)
            subscribedLocalization.OnLanguageChanged -= RefreshValueLabel;

        subscribedLocalization = manager;
        subscribedLocalization.OnLanguageChanged += RefreshValueLabel;
    }

    private void CreateSlider(RectTransform parent)
    {
        var labelObject = new GameObject("Camera Lens Value", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(parent, false);
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = anchoredPosition + new Vector2(0f, 21f);
        labelRect.sizeDelta = new Vector2(controlWidth, 22f);

        valueLabel = labelObject.GetComponent<TextMeshProUGUI>();
        valueLabel.font = labelFont;
        valueLabel.fontSize = 18f;
        valueLabel.color = Color.white;
        valueLabel.alignment = TextAlignmentOptions.Left;
        valueLabel.raycastTarget = false;

        var sliderObject = new GameObject("Camera Lens Slider", typeof(RectTransform), typeof(Slider));
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.SetParent(parent, false);
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = anchoredPosition + new Vector2(0f, -8f);
        sliderRect.sizeDelta = new Vector2(controlWidth, 18f);

        Image track = CreateImage("Track", sliderRect, new Color(0.06f, 0.08f, 0.12f, 0.9f));
        Stretch(track.rectTransform, Vector2.zero, Vector2.zero);

        var fillAreaObject = new GameObject("Fill Area", typeof(RectTransform));
        RectTransform fillArea = fillAreaObject.GetComponent<RectTransform>();
        fillArea.SetParent(sliderRect, false);
        Stretch(fillArea, new Vector2(7f, 4f), new Vector2(-7f, -4f));

        Image fill = CreateImage("Fill", fillArea, new Color(0.15f, 0.85f, 0.95f, 1f));
        Stretch(fill.rectTransform, Vector2.zero, Vector2.zero);

        var handleAreaObject = new GameObject("Handle Slide Area", typeof(RectTransform));
        RectTransform handleArea = handleAreaObject.GetComponent<RectTransform>();
        handleArea.SetParent(sliderRect, false);
        Stretch(handleArea, new Vector2(7f, 0f), new Vector2(-7f, 0f));

        Image handle = CreateImage("Handle", handleArea, Color.white);
        RectTransform handleRect = handle.rectTransform;
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(12f, 24f);

        Slider slider = sliderObject.GetComponent<Slider>();
        ConfigureSlider(slider);
    }

    private void ConfigureSlider(Slider slider)
    {
        slider.interactable = true;
        slider.minValue = minimumLensSize;
        slider.maxValue = maximumLensSize;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;
        Transform fillTransform = slider.transform.Find("Fill Area/Fill");
        Transform handleTransform = slider.transform.Find("Handle Slide Area/Handle");
        slider.fillRect = fillTransform as RectTransform;
        slider.handleRect = handleTransform as RectTransform;
        slider.targetGraphic = handleTransform != null ? handleTransform.GetComponent<Image>() : null;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };

        slider.SetValueWithoutNotify(Mathf.Clamp(CurrentLensSize, minimumLensSize, maximumLensSize));
        slider.onValueChanged.RemoveListener(ApplyLensSize);
        slider.onValueChanged.AddListener(ApplyLensSize);
        ApplyLensSize(slider.value);
    }

    private void ApplyLensSize(float lensSize)
    {
        if (virtualCamera == null)
            return;

        LensSettings lens = virtualCamera.Lens;
        lens.OrthographicSize = lensSize;
        virtualCamera.Lens = lens;

        CinemachineConfiner2D confiner = virtualCamera.GetComponent<CinemachineConfiner2D>();
        if (confiner != null)
            confiner.InvalidateLensCache();

        RefreshValueLabel(lensSize);
    }

    private void RefreshValueLabel()
    {
        RefreshValueLabel(CurrentLensSize);
    }

    private void RefreshValueLabel(float lensSize)
    {
        if (valueLabel == null)
            return;

        const string fallback = "Lens: {0}";
        string format = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.GetTranslation("debug_camera_lens")
            : "debug_camera_lens";
        if (string.IsNullOrEmpty(format) || format == "debug_camera_lens")
            format = fallback;

        valueLabel.text = string.Format(
            CultureInfo.InvariantCulture,
            format,
            lensSize.ToString("0.0", CultureInfo.InvariantCulture));
    }

    private static Image CreateImage(string objectName, Transform parent, Color color)
    {
        var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = objectName == "Handle";
        return image;
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
