using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
public sealed class ParabolicDottedRoute : MaskableGraphic
{
    [SerializeField, Min(2f)] private float dotSpacing = 17f;
    [SerializeField, Min(1f)] private float dotLength = 6f;
    [SerializeField, Min(0.5f)] private float dotWidth = 3f;
    [SerializeField, Min(0f)] private float curveHeight = 34f;
    [SerializeField, Min(0f)] private float outlineInset = 115f;
    [SerializeField, Min(1f)] private float graphicHeight = 90f;

    protected override void OnEnable()
    {
        base.OnEnable();
        NormalizeRectTransform();
        raycastTarget = false;
        SetVerticesDirty();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        NormalizeRectTransform();
        SetVerticesDirty();
    }
#endif

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        Rect rect = rectTransform.rect;
        float width = rect.width;
        if (width <= 0f)
            return;

        float left = rect.xMin + outlineInset;
        float right = rect.xMax - outlineInset;
        float pathWidth = right - left;
        if (pathWidth <= 0f)
            return;

        int dotCount = Mathf.Max(2, Mathf.CeilToInt(pathWidth / dotSpacing));
        float bottom = rect.center.y;

        for (int i = 0; i <= dotCount; i++)
        {
            float t = i / (float)dotCount;
            float x = Mathf.Lerp(left, right, t);
            float y = bottom + 4f * curveHeight * t * (1f - t);
            Vector2 tangent = new Vector2(1f, 4f * curveHeight * (1f - 2f * t) / pathWidth).normalized;
            AddRectDot(vertexHelper, new Vector2(x, y), tangent);
        }
    }

    private void AddRectDot(VertexHelper vertexHelper, Vector2 center, Vector2 tangent)
    {
        int firstIndex = vertexHelper.currentVertCount;
        Vector2 normal = new Vector2(-tangent.y, tangent.x);
        Vector2 halfLength = tangent * (dotLength * 0.5f);
        Vector2 halfWidth = normal * (dotWidth * 0.5f);

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;
        vertex.position = center - halfLength - halfWidth;
        vertexHelper.AddVert(vertex);
        vertex.position = center - halfLength + halfWidth;
        vertexHelper.AddVert(vertex);
        vertex.position = center + halfLength + halfWidth;
        vertexHelper.AddVert(vertex);
        vertex.position = center + halfLength - halfWidth;
        vertexHelper.AddVert(vertex);
        vertexHelper.AddTriangle(firstIndex, firstIndex + 1, firstIndex + 2);
        vertexHelper.AddTriangle(firstIndex, firstIndex + 2, firstIndex + 3);
    }

    private void NormalizeRectTransform()
    {
        RectTransform parentRect = transform.parent as RectTransform;
        if (parentRect == null)
            return;

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one;
        rectTransform.sizeDelta = new Vector2(parentRect.rect.width, graphicHeight);
    }
}
