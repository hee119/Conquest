using TMPro;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public sealed class CharacterTitleAlignment : MonoBehaviour
{
    private TMP_Text title;

    private void OnEnable()
    {
        title = GetComponent<TMP_Text>();
        title.OnPreRenderText += Align;
        title.SetVerticesDirty();
    }

    private void OnDisable()
    {
        if (title != null)
            title.OnPreRenderText -= Align;
    }

    private void Align(TMP_TextInfo textInfo)
    {
        RectTransform rect = title.rectTransform;
        int count = textInfo.characterCount;
        if (count == 0 && !string.IsNullOrEmpty(title.text))
            return;

        bool hasHangul = false;
        foreach (char character in title.text)
        {
            if (character >= '\uAC00' && character <= '\uD7A3')
            {
                hasHangul = true;
                break;
            }
        }

        float center = 0f;
        if (hasHangul && count > 0)
        {
            TMP_CharacterInfo[] characters = textInfo.characterInfo;
            int middle = count / 2;
            center = count % 2 == 0
                ? characters[middle].origin
                : (characters[middle].origin + characters[middle].xAdvance) * 0.5f;
        }

        Vector2 position = rect.anchoredPosition;
        position.x = -center;
        if (!Mathf.Approximately(rect.anchoredPosition.x, position.x))
            rect.anchoredPosition = position;
    }
}
