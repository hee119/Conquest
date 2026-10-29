using UnityEngine;

// A soft white bloom with fine radial rays, drawn directly as UI geometry.
public sealed class UpgradeFusionBurst : UnityEngine.UI.MaskableGraphic
{
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        const int segments = 96;
        Vector2 center = rectTransform.rect.center;
        float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
        Color clear = new Color(color.r, color.g, color.b, 0f);
        mesh.AddVert(center, color, Vector2.zero);
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            mesh.AddVert(center + direction * radius * 0.18f, color, Vector2.zero);
            mesh.AddVert(center + direction * radius, clear, Vector2.zero);
        }
        for (int i = 0; i < segments; i++)
        {
            int a = 1 + i * 2;
            mesh.AddTriangle(0, a, a + 2);
            mesh.AddTriangle(a, a + 1, a + 2);
            mesh.AddTriangle(a + 1, a + 3, a + 2);
        }
        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.PI / 6f;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            int start = mesh.currentVertCount;
            mesh.AddVert(center, color, Vector2.zero);
            mesh.AddVert(center + direction * radius * 0.2f + perpendicular * 6f, color, Vector2.zero);
            mesh.AddVert(center + direction * radius * 1.5f, clear, Vector2.zero);
            mesh.AddVert(center + direction * radius * 0.2f - perpendicular * 6f, color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
