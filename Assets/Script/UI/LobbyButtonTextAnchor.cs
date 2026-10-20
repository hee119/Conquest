using TMPro;
using UnityEngine;

/// <summary>Aligns a chosen part of a localized button label with its sprite's center.</summary>
[ExecuteAlways]
[RequireComponent(typeof(TextMeshProUGUI))]
public sealed class LobbyButtonTextAnchor : MonoBehaviour
{
    public enum AnchorKind
    {
        GlyphCenter,
        AfterGlyph
    }

    [SerializeField] private string targetText;
    [SerializeField] private int characterIndex;
    [SerializeField] private AnchorKind anchorKind;

    private TextMeshProUGUI label;
    private RectTransform labelRect;
    private string lastText;
    private TMP_FontAsset lastFont;
    private float lastFontSize;
    private Vector2 lastSize;
    private float lastX;
    private bool initialized;

    private void OnEnable()
    {
        label = GetComponent<TextMeshProUGUI>();
        labelRect = label.rectTransform;
        initialized = false;
    }

    private void OnValidate()
    {
        initialized = false;
    }

    private void LateUpdate()
    {
        if (label == null || labelRect == null || labelRect.parent == null)
            return;

        if (initialized && lastText == label.text && lastFont == label.font &&
            Mathf.Approximately(lastFontSize, label.fontSize) && lastSize == labelRect.rect.size &&
            Mathf.Approximately(lastX, labelRect.anchoredPosition.x))
            return;

        Align();
        lastText = label.text;
        lastFont = label.font;
        lastFontSize = label.fontSize;
        lastSize = labelRect.rect.size;
        lastX = labelRect.anchoredPosition.x;
        initialized = true;
    }

    private void Align()
    {
        float x = 0f;
        if (label.text == targetText && characterIndex >= 0)
        {
            label.ForceMeshUpdate(true, true);
            if (characterIndex < label.textInfo.characterCount)
            {
                var character = label.textInfo.characterInfo[characterIndex];
                var parent = labelRect.parent;
                if (anchorKind == AnchorKind.AfterGlyph)
                {
                    x = parent.InverseTransformPoint(
                        labelRect.TransformPoint(new Vector3(character.xAdvance, 0f, 0f))).x;
                }
                else
                {
                    float left = parent.InverseTransformPoint(labelRect.TransformPoint(character.bottomLeft)).x;
                    float right = parent.InverseTransformPoint(labelRect.TransformPoint(character.topRight)).x;
                    x = (left + right) * 0.5f;
                }
                x = labelRect.anchoredPosition.x - x;
            }
        }

        if (Mathf.Abs(labelRect.anchoredPosition.x - x) > 0.01f)
        {
            var position = labelRect.anchoredPosition;
            position.x = x;
            labelRect.anchoredPosition = position;
        }
    }
}
