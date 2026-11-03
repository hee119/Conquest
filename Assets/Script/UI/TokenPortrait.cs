using UnityEngine;
using UnityEngine.UI;

// Uses the original character sprite, cropped to the face inside diamond geometry.
public sealed class TokenPortrait : Image
{
    public int characterId = 1;

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = GetPixelAdjustedRect();
        Rect uv = new Rect(0f, 0f, 1f, 1f);
        if (sprite != null)
        {
            Rect crop = characterId == 2 ? new Rect(70f, 87f, 124f, 124f) : new Rect(64f, 114f, 124f, 124f);
            Rect source = sprite.rect;
            uv = new Rect((source.x + crop.x / 256f * source.width) / sprite.texture.width,
                (source.y + crop.y / 256f * source.height) / sprite.texture.height,
                crop.width / 256f * source.width / sprite.texture.width,
                crop.height / 256f * source.height / sprite.texture.height);
        }
        Vector2[] points = { new Vector2(.5f,.5f), new Vector2(.5f,1f), new Vector2(1f,.5f), new Vector2(.5f,0f), new Vector2(0f,.5f) };
        foreach (var point in points)
            mesh.AddVert(new Vector3(rect.x + point.x * rect.width, rect.y + point.y * rect.height), color,
                new Vector2(uv.x + point.x * uv.width, uv.y + point.y * uv.height));
        for (int i = 1; i <= 4; i++) mesh.AddTriangle(0, i, i == 4 ? 1 : i + 1);
    }

    public static void Apply(Image original, int id)
    {
        if (original == null) return;
        original.enabled = false;
        Transform root = original.transform.Find("Token Diamond");
        if (root == null)
        {
            var frame = new GameObject("Token Diamond", typeof(RectTransform), typeof(CanvasRenderer), typeof(TokenPortrait));
            frame.layer = original.gameObject.layer;
            frame.transform.SetParent(original.transform, false);
            var border = frame.GetComponent<TokenPortrait>();
            border.color = Color.white;
            border.raycastTarget = false;
            var rect = border.rectTransform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            CreateLayer(frame.transform, "Background", 3f).color = new Color(.06f,.06f,.09f);
            CreateLayer(frame.transform, "Face", 5f);
            root = frame.transform;
        }
        var face = root.Find("Face").GetComponent<TokenPortrait>();
        face.characterId = id;
        face.sprite = CharacterProgression.Definition(id)?.portrait;
        face.SetVerticesDirty();
    }

    private static TokenPortrait CreateLayer(Transform parent, string name, float inset)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TokenPortrait));
        obj.layer = parent.gameObject.layer;
        obj.transform.SetParent(parent, false);
        var image = obj.GetComponent<TokenPortrait>();
        image.raycastTarget = false;
        image.rectTransform.anchorMin = Vector2.zero;
        image.rectTransform.anchorMax = Vector2.one;
        image.rectTransform.offsetMin = Vector2.one * inset;
        image.rectTransform.offsetMax = Vector2.one * -inset;
        return image;
    }
}
