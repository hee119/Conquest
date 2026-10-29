using TMPro;
using UnityEngine;

/// <summary>Extends both existing graphics settings pages using their authored slider style.</summary>
public static class InterfaceScaleSettingsPanel
{
    private static readonly string[] LabelKeys =
    {
        "setting_ui_scale_buttons", "setting_ui_scale_sliders", "setting_ui_scale_text",
        "setting_ui_scale_icons", "setting_ui_scale_vitals", "setting_ui_scale_experience"
    };

    public static void Ensure(GameObject settings)
    {
        Transform page = settings.transform.Find("Safe Area/Options/Graphic");
        if (page == null || page.Find("Interface Scale Scroll") != null) return;
        Transform sourceRow = page.Find("Options (2)");
        if (sourceRow == null || sourceRow.GetComponentInChildren<UnityEngine.UI.Slider>(true) == null) return;
        var pageLayout = page.GetComponent<UnityEngine.UI.LayoutGroup>();
        if (pageLayout != null) pageLayout.enabled = false;

        var originalRows = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in page) originalRows.Add(child);
        var scrollObject = new GameObject("Interface Scale Scroll", typeof(RectTransform), typeof(UnityEngine.UI.ScrollRect));
        scrollObject.SetActive(false);
        var scrollRect = (RectTransform)scrollObject.transform;
        scrollRect.SetParent(page, false);
        scrollRect.anchorMin = new Vector2(0.18f, 0.20f);
        scrollRect.anchorMax = new Vector2(0.88f, 0.86f);
        scrollRect.offsetMin = scrollRect.offsetMax = Vector2.zero;

        var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Mask));
        var viewport = (RectTransform)viewportObject.transform;
        viewport.SetParent(scrollRect, false);
        Stretch(viewport);
        viewportObject.GetComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
        viewportObject.GetComponent<UnityEngine.UI.Image>().color = Color.white;

        var contentObject = new GameObject("Content", typeof(RectTransform), typeof(UnityEngine.UI.VerticalLayoutGroup), typeof(UnityEngine.UI.ContentSizeFitter));
        var content = (RectTransform)contentObject.transform;
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0f, 1f); content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0f, 1f); content.sizeDelta = Vector2.zero;
        content.anchoredPosition = Vector2.zero;
        var layout = contentObject.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.padding = new RectOffset(20, 130, 8, 20);
        layout.spacing = 18f; layout.childControlWidth = true; layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        var fitter = contentObject.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

        foreach (Transform row in originalRows)
        {
            row.SetParent(content, false);
            SetRowHeight(row.gameObject, 80f);
        }

        TMP_Text labelStyle = sourceRow.GetComponentInChildren<TMP_Text>(true);
        CreateText(content, labelStyle, "Interface Size Title", "setting_interface_title", 34f, 55f);
        for (int i = 0; i < LabelKeys.Length; i++)
        {
            GameObject row = Object.Instantiate(sourceRow.gameObject, content, false);
            row.name = "Interface Scale " + (InterfaceScaleCategory)i;
            SetRowHeight(row, 72f);
            var label = row.GetComponentInChildren<LocalizedText>(true);
            label.key = LabelKeys[i]; label.isTextInit = true;
            var text = label.GetComponent<TMP_Text>();
            text.fontSize = 32f;
            var labelFitter = label.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (labelFitter != null) labelFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            if (LocalizationManager.Instance != null) text.text = LocalizationManager.Instance.GetTranslation(label.key);

            var slider = row.GetComponentInChildren<UnityEngine.UI.Slider>(true);
            // Clear the brightness callbacks on the clone, including persistent listeners.
            slider.onValueChanged = new UnityEngine.UI.Slider.SliderEvent();
            var brightness = slider.GetComponent<SceneBrightnessController>();
            if (brightness != null) { brightness.enabled = false; Object.Destroy(brightness); }
            var volume = slider.GetComponent<SoundVolumeSlider>();
            if (volume != null) { volume.enabled = false; Object.Destroy(volume); }
            var oldPercent = slider.GetComponent<SliderPercentText>();
            if (oldPercent != null) { oldPercent.enabled = false; Object.Destroy(oldPercent); }
            slider.minValue = InterfaceScaleManager.MinScale; slider.maxValue = InterfaceScaleManager.MaxScale;
            slider.SetValueWithoutNotify(InterfaceScaleManager.Instance.GetScale((InterfaceScaleCategory)i));
            var control = slider.gameObject.AddComponent<InterfaceScaleControl>();
            control.category = (InterfaceScaleCategory)i;
            control.valueLabel = slider.GetComponentInChildren<TMP_Text>(true);
        }
        CreateText(content, labelStyle, "Interface Size Hint", "setting_ui_scale_hint", 26f, 85f);

        var scroll = scrollObject.GetComponent<UnityEngine.UI.ScrollRect>();
        scroll.viewport = viewport; scroll.content = content;
        scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
        scroll.inertia = false; scroll.scrollSensitivity = 50f;
        var barObject = new GameObject("Scrollbar", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Scrollbar));
        var barRect = (RectTransform)barObject.transform;
        barRect.SetParent(scrollRect, false);
        barRect.anchorMin = new Vector2(1f, 0f); barRect.anchorMax = Vector2.one;
        barRect.pivot = new Vector2(1f, 0.5f); barRect.sizeDelta = new Vector2(6f, -12f);
        barRect.anchoredPosition = new Vector2(-8f, 0f);
        barObject.GetComponent<UnityEngine.UI.Image>().color = new Color(1f, 1f, 1f, 0.15f);
        var handleObject = new GameObject("Handle", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        var handle = (RectTransform)handleObject.transform;
        handle.SetParent(barRect, false); Stretch(handle);
        var bar = barObject.GetComponent<UnityEngine.UI.Scrollbar>();
        bar.handleRect = handle; bar.targetGraphic = handleObject.GetComponent<UnityEngine.UI.Image>();
        bar.direction = UnityEngine.UI.Scrollbar.Direction.BottomToTop;
        scroll.verticalScrollbar = bar;
        scroll.verticalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHide;
        scrollObject.SetActive(true);
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        scroll.verticalNormalizedPosition = 1f;
        InterfaceScaleManager.Instance.RefreshTargets();
    }

    private static void CreateText(Transform parent, TMP_Text style, string name, string key, float size, float height)
    {
        GameObject obj = Object.Instantiate(style.gameObject, parent, false);
        obj.name = name;
        var fitter = obj.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        if (fitter != null) fitter.enabled = false;
        var text = obj.GetComponent<TMP_Text>();
        text.fontSize = size; text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        var localized = obj.GetComponent<LocalizedText>();
        if (localized == null) localized = obj.AddComponent<LocalizedText>();
        localized.key = key; localized.isTextInit = true;
        if (LocalizationManager.Instance != null) text.text = LocalizationManager.Instance.GetTranslation(key);
        SetRowHeight(obj, height);
    }

    private static void SetRowHeight(GameObject row, float height)
    {
        var element = row.GetComponent<UnityEngine.UI.LayoutElement>();
        if (element == null) element = row.AddComponent<UnityEngine.UI.LayoutElement>();
        element.ignoreLayout = false; element.minHeight = height; element.preferredHeight = height; element.flexibleHeight = 0f;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
