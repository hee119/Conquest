using UnityEngine;
using UnityEngine.UI;

public class ExpBarRightEdgeGlow : MaskableGraphic
{
    [SerializeField] private float thickness = 5f;
    [SerializeField] private float horizontalLength = 18f;
    [SerializeField] private int arcSegments = 24;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = rectTransform.rect;
        if (rect.width <= 0f || rect.height <= 0f)
            return;

        float radius = Mathf.Max(1f, (rect.height - thickness) * 0.5f);
        float centerX = rect.xMax - radius - thickness * 0.5f;
        Vector2 center = new Vector2(centerX, rect.center.y);

        AddArc(vh, center, radius, -90f, 90f);

        float lineStartX = Mathf.Max(rect.xMin, centerX - horizontalLength);
        float lineEndX = centerX;
        AddLine(vh, new Vector2(lineStartX, center.y + radius), new Vector2(lineEndX, center.y + radius));
        AddLine(vh, new Vector2(lineStartX, center.y - radius), new Vector2(lineEndX, center.y - radius));
    }

    private void AddArc(VertexHelper vh, Vector2 center, float radius, float startAngle, float endAngle)
    {
        int segmentCount = Mathf.Max(4, arcSegments);
        Vector2 previous = PointOnCircle(center, radius, startAngle);

        for (int i = 1; i <= segmentCount; i++)
        {
            float t = i / (float)segmentCount;
            float angle = Mathf.Lerp(startAngle, endAngle, t);
            Vector2 current = PointOnCircle(center, radius, angle);
            AddLine(vh, previous, current);
            previous = current;
        }
    }

    private Vector2 PointOnCircle(Vector2 center, float radius, float angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        return center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
    }

    private void AddLine(VertexHelper vh, Vector2 start, Vector2 end)
    {
        Vector2 direction = end - start;
        if (direction.sqrMagnitude <= 0.001f)
            return;

        Vector2 normal = new Vector2(-direction.y, direction.x).normalized * (thickness * 0.5f);
        int index = vh.currentVertCount;

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        vertex.position = start - normal;
        vh.AddVert(vertex);
        vertex.position = start + normal;
        vh.AddVert(vertex);
        vertex.position = end + normal;
        vh.AddVert(vertex);
        vertex.position = end - normal;
        vh.AddVert(vertex);

        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index + 2, index + 3, index);
    }
}
