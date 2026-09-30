using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(HorizontalLayoutGroup))]
public sealed class SettingsOptionRowLayout : MonoBehaviour
{
    [SerializeField, Min(0f)] private float labelWidth = 200f;
    [SerializeField, Min(0f)] private float controlColumnStart = 647f;
    [SerializeField, Min(0f)] private float controlWidth = 322f;
    [SerializeField, Min(0f)] private float sliderWidth = 300f;

    private bool applying;

    private void OnEnable() => ApplyLayout();

    private void OnValidate() => ApplyLayout();

    private void OnTransformChildrenChanged() => ApplyLayout();

    private void ApplyLayout()
    {
        if (applying)
            return;

        var row = transform as RectTransform;
        var layout = GetComponent<HorizontalLayoutGroup>();
        if (row == null || layout == null)
            return;

        applying = true;
        layout.spacing = Mathf.Max(0f, controlColumnStart - labelWidth);
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;

        int childCount = Mathf.Min(2, row.childCount);
        for (int i = 0; i < childCount; i++)
        {
            if (!(row.GetChild(i) is RectTransform child))
                continue;

            float width = i == 0
                ? labelWidth
                : child.GetComponent<Slider>() != null ? sliderWidth : controlWidth;
            var element = child.GetComponent<LayoutElement>();
            if (element == null)
                element = child.gameObject.AddComponent<LayoutElement>();

            element.minWidth = width;
            element.preferredWidth = width;
            element.flexibleWidth = 0f;
            element.ignoreLayout = false;

            if (i > 0 && child.GetComponent<Slider>() != null)
                CenterSliderVisual(child, "Background", "Handle Slide Area");
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(row);
        applying = false;
    }

    private static void CenterSliderVisual(RectTransform slider, params string[] childNames)
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
