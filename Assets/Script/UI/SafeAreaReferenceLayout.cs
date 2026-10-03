using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaReferenceLayout : MonoBehaviour
{
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);

    private RectTransform content;
    private RectTransform parentRect;
    private Rect lastSafeArea;
    private Vector2 lastParentSize;
    private Vector2 lastReferenceResolution;

    private void OnEnable()
    {
        content = (RectTransform)transform;
        parentRect = transform.parent as RectTransform;
        lastParentSize = Vector2.zero;
        Canvas.preWillRenderCanvases += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        Canvas.preWillRenderCanvases -= Refresh;
    }

    private void OnTransformParentChanged()
    {
        parentRect = transform.parent as RectTransform;
        lastParentSize = Vector2.zero;
    }

    private void LateUpdate() => Refresh();

    private void Refresh()
    {
        if (content == null || parentRect == null || Screen.width <= 0 || Screen.height <= 0)
            return;

        Rect safeArea = Screen.safeArea;
        Rect normalizedArea = Rect.MinMaxRect(
            Mathf.Clamp01(safeArea.xMin / Screen.width),
            Mathf.Clamp01(safeArea.yMin / Screen.height),
            Mathf.Clamp01(safeArea.xMax / Screen.width),
            Mathf.Clamp01(safeArea.yMax / Screen.height)
        );

        if (normalizedArea.width <= 0f || normalizedArea.height <= 0f)
            return;

        if (lastSafeArea == normalizedArea && lastParentSize == parentRect.rect.size &&
            lastReferenceResolution == referenceResolution)
            return;

        ApplyLayout(normalizedArea);
        lastSafeArea = normalizedArea;
        lastParentSize = parentRect.rect.size;
        lastReferenceResolution = referenceResolution;
    }

    private void ApplyLayout(Rect normalizedArea)
    {
        Vector2 designSize = new Vector2(
            Mathf.Max(1f, referenceResolution.x), Mathf.Max(1f, referenceResolution.y)
        );
        Vector2 availableSize = Vector2.Scale(parentRect.rect.size, normalizedArea.size);
        float scale = Mathf.Min(availableSize.x / designSize.x, availableSize.y / designSize.y);

        // Keep the authored coordinates intact while fitting the whole layout inside the safe area.
        content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
        content.sizeDelta = designSize;
        content.anchoredPosition = Vector2.Scale(
            normalizedArea.center - new Vector2(0.5f, 0.5f), parentRect.rect.size
        );
        content.localScale = new Vector3(scale, scale, 1f);
    }
}
