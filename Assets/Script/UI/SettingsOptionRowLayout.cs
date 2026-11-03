
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
[RequireComponent(typeof(HorizontalLayoutGroup))]
public sealed class SettingsOptionRowLayout : MonoBehaviour
{
    [SerializeField, Min(0f)] private float labelWidth = 300f;
    [SerializeField, Min(0f)] private float controlColumnStart = 647f;
    [SerializeField, Min(0f)] private float controlWidth = 322f;
    [SerializeField, Min(0f)] private float sliderWidth = 300f;

    private bool applying;

    private void OnEnable()
    {
        if (Application.isPlaying)
            ApplyLayout();
    }

    private void OnTransformChildrenChanged()
    {
        if (Application.isPlaying)
            ApplyLayout();
    }

    public void RefreshLayout() => ApplyLayout();
    
    private void ApplyLayout()
    {
        if (!Application.isPlaying || applying)
            return;

        var row = transform as RectTransform;
        var layout = GetComponent<HorizontalLayoutGroup>();

        if (row == null || layout == null)
            return;

        applying = true;

        try
        {
            layout.spacing = Mathf.Max(
                0f, controlColumnStart - labelWidth);

            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childScaleWidth = true;
            layout.childScaleHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = false;

            int childCount = Mathf.Min(2, row.childCount);

            for (int i = 0; i < childCount; i++)
            {
                if (row.GetChild(i) is not RectTransform child)
                    continue;

                // 드롭다운은 레이아웃 관리에서 제외
                if (child.TryGetComponent<TMP_Dropdown>(out _))
                {
                    if (!child.TryGetComponent<LayoutElement>(
                            out var dropdownElement))
                    {
                        dropdownElement =
                            child.gameObject.AddComponent<LayoutElement>();
                    }

                    dropdownElement.ignoreLayout = true;
                    continue;
                }

                if (i == 0 &&
                    child.TryGetComponent<TMP_Text>(out var label))
                {
                    label.enableAutoSizing = false;
                    label.fontSize = 34.5f;
                    label.textWrappingMode = TextWrappingModes.Normal;
                    label.overflowMode = TextOverflowModes.Truncate;
                }

                bool isSlider = child.GetComponent<Slider>() != null;

                float width = i == 0
                    ? labelWidth
                    : isSlider ? sliderWidth : controlWidth;

                var element = child.GetComponent<LayoutElement>();

                if (element == null)
                    element = child.gameObject.AddComponent<LayoutElement>();

                element.minWidth = width;
                element.preferredWidth = width;
                element.flexibleWidth = 0f;
                element.ignoreLayout = false;

                child.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal, width);

                if (i == 0 &&
                    child.TryGetComponent<TMP_Text>(out var caption))
                {
                    float height = Mathf.Max(
                        60f,
                        caption.GetPreferredValues(
                            caption.text,
                            width,
                            float.PositiveInfinity).y
                    );

                    child.SetSizeWithCurrentAnchors(
                        RectTransform.Axis.Vertical, height);
                }

                if (i > 0 && isSlider)
                {
                    var slider = child.GetComponent<Slider>();

                    SliderInputBounds.Attach(slider);

                    CenterSliderVisual(
                        child,
                        "Background",
                        "Handle Slide Area");
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(row);
        }
        finally
        {
            applying = false;
        }
    }

    private static void CenterSliderVisual(
        RectTransform slider,
        params string[] childNames)
    {
        foreach (string childName in childNames)
        {
            var child = slider.Find(childName) as RectTransform;

            if (child == null)
                continue;

            Vector2 position = child.anchoredPosition;

            if (Mathf.Approximately(position.y, 0f))
                continue;

            position.y = 0f;
            child.anchoredPosition = position;
        }
    }
}
