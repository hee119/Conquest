using System.Collections.Generic;
using UnityEngine;

// Layout groups normally ignore localScale. Reserve the visible size of each
// option row and rebuild only after scale/layout changes, not every frame.
[DisallowMultipleComponent]
public sealed class SettingsLayout : MonoBehaviour
{
    private readonly Dictionary<RectTransform, float> rowHeights = new();
    private readonly Vector3[] corners = new Vector3[4];
    private bool dirty = true;
    private bool rebuilding;

    private void OnEnable()
    {
        // Scene-authored layout is authoritative; do not reposition it on open.
        dirty = true;
    }

    private void OnDisable()
    {
        Canvas.willRenderCanvases -= Rebuild;
    }

    private void OnRectTransformDimensionsChange() { if (!rebuilding) dirty = true; }
    private void OnTransformChildrenChanged() { if (!rebuilding) dirty = true; }

    private void Rebuild()
    {
        if (!dirty || rebuilding || !gameObject.activeInHierarchy) return;
        dirty = false;
        rebuilding = true;
        try
        {
            // Old authored rows had compensating Z offsets. Keep the entire
            // scrolling hierarchy on the mask plane so RectMask2D can clip it.
            var pages = transform.Find("Safe Area/Options");
            if (pages != null)
                foreach (var rect in pages.GetComponentsInChildren<RectTransform>(true))
                {
                    Vector3 position = rect.localPosition;
                    if (Mathf.Abs(position.z) > 0.001f) { position.z = 0f; rect.localPosition = position; }
                }
            var layouts = GetComponentsInChildren<UnityEngine.UI.HorizontalOrVerticalLayoutGroup>(true);
            foreach (var layout in layouts)
            {
                layout.childScaleWidth = true;
                layout.childScaleHeight = true;
                if (layout.name == "OptionsSelectButtons")
                {
                    layout.spacing = 8f;
                    layout.childForceExpandWidth = false;
                    layout.childAlignment = TextAnchor.UpperCenter;
                }
                if (layout is UnityEngine.UI.VerticalLayoutGroup)
                {
                    layout.childControlHeight = true;
                    layout.childForceExpandHeight = false;
                }
            }
            // First place scaled controls, then measure their visible bounds.
            foreach (var row in GetComponentsInChildren<SettingsOptionRowLayout>(true))
            {
                row.RefreshLayout();
                var rect = (RectTransform)row.transform;
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                LayoutSliderControls(rect);
                var element = row.GetComponent<UnityEngine.UI.LayoutElement>();
                if (element == null) element = row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                if (!rowHeights.ContainsKey(rect))
                    rowHeights[rect] = 72f;
                float minimum = float.PositiveInfinity, maximum = float.NegativeInfinity;
                foreach (var graphic in row.GetComponentsInChildren<UnityEngine.UI.Graphic>(false))
                {
                    if (!graphic.enabled || graphic.color.a <= 0f) continue;
                    bool popup = false;
                    for (Transform p = graphic.transform; p != row.transform && p != null; p = p.parent)
                        if (p.name == "Template" || p.name == "Dropdown List") { popup = true; break; }
                    if (popup) continue;
                    graphic.rectTransform.GetWorldCorners(corners);
                    foreach (var corner in corners)
                    {
                        float y = rect.InverseTransformPoint(corner).y;
                        minimum = Mathf.Min(minimum, y);
                        maximum = Mathf.Max(maximum, y);
                    }
                }
                float height = rowHeights[rect];
                if (!float.IsInfinity(minimum)) height = Mathf.Max(height, maximum - minimum + 20f);
                element.minHeight = element.preferredHeight = height;
                element.flexibleHeight = 0f;
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }
            ReserveHeaderSpace();
            for (int i = layouts.Length - 1; i >= 0; i--)
                if (layouts[i].enabled)
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)layouts[i].transform);
        }
        finally { rebuilding = false; }
    }

    private Rect BoundsInRow(RectTransform row, RectTransform rect)
    {
        rect.GetWorldCorners(corners);
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        foreach (var point in corners)
        {
            Vector2 local = row.InverseTransformPoint(point);
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private static void MoveInRow(RectTransform row, RectTransform rect, float delta)
    {
        Vector3 movement = rect.parent.InverseTransformVector(row.TransformVector(new Vector3(delta, 0f, 0f)));
        rect.localPosition += movement;
    }

    private void LayoutSliderControls(RectTransform row)
    {
        var slider = row.GetComponentInChildren<UnityEngine.UI.Slider>();
        if (slider == null) return;
        var bar = (RectTransform)slider.transform;
        var value = bar.Find("100%") as RectTransform;
        var buttons = bar.Find("Value Step Buttons") as RectTransform;
        if (value == null || buttons == null) return;
        var valueFitter = value.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        if (valueFitter != null) valueFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
        value.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 100f);
        value.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 60f);
        if (value.TryGetComponent<TMPro.TMP_Text>(out var percentage))
        {
            percentage.enableAutoSizing = false;
            percentage.fontSize = 35f;
            percentage.overflowMode = TMPro.TextOverflowModes.Truncate;
        }
        var minus = buttons.Find("Minus") as RectTransform;
        var plus = buttons.Find("Plus") as RectTransform;
        if (minus == null || plus == null) return;
        Rect barBounds = BoundsInRow(row, bar);
        Rect plusBounds = BoundsInRow(row, plus);
        Rect valueBounds = BoundsInRow(row, value);
        MoveInRow(row, value, plusBounds.xMax + 12f - valueBounds.xMin);
        var layout = row.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        if (layout != null && row.childCount > 0 && row.GetChild(0) is RectTransform label)
        {
            Rect minusBounds = BoundsInRow(row, minus);
            valueBounds = BoundsInRow(row, value);
            float controlWidth = valueBounds.xMax - minusBounds.xMin;
            float remaining = row.rect.width - BoundsInRow(row, label).width - controlWidth;
            layout.spacing = Mathf.Clamp(remaining, 24f, 347f);
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(row);
        }
    }

    private void ReserveHeaderSpace()
    {
        var header = transform.Find("Safe Area/OptionsSelectButtons") as RectTransform;
        var options = transform.Find("Safe Area/Options") as RectTransform;
        if (header == null || options == null || options.parent is not RectTransform parent) return;
        // Draw fixed tabs above every settings page, and keep page content below
        // their scaled lower edge instead of allowing it to scroll behind them.
        header.SetAsLastSibling();
        float bottom = float.PositiveInfinity;
        foreach (Transform tab in header)
        {
            if (!tab.gameObject.activeSelf || tab is not RectTransform rect) continue;
            rect.GetWorldCorners(corners);
            foreach (var corner in corners)
                bottom = Mathf.Min(bottom, parent.InverseTransformPoint(corner).y);
        }
        if (float.IsInfinity(bottom)) return;
        float currentTop = parent.rect.yMin + parent.rect.height * options.anchorMax.y + options.offsetMax.y;
        Vector2 offset = options.offsetMax;
        offset.y += bottom - 24f - currentTop;
        if ((options.offsetMax - offset).sqrMagnitude > 0.01f) options.offsetMax = offset;
        float contentBottom = parent.rect.yMin + 80f;
        var background = transform.Find("Safe Area/SettingBackGround") as RectTransform;
        if (background != null) contentBottom = BoundsInRow(parent, background).yMin + 80f;
        var quit = transform.Find("Quit") as RectTransform;
        if (quit != null && quit.gameObject.activeInHierarchy)
            contentBottom = Mathf.Max(contentBottom, BoundsInRow(parent, quit).yMax + 24f);
        float currentBottom = parent.rect.yMin + parent.rect.height * options.anchorMin.y + options.offsetMin.y;
        offset = options.offsetMin;
        offset.y += contentBottom - currentBottom;
        if ((options.offsetMin - offset).sqrMagnitude > 0.01f) options.offsetMin = offset;
    }

}
