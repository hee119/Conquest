using TMPro;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public sealed class CharacterTitleAlignment : MonoBehaviour
{
    private TMP_Text title;
    private string lastText;
    private float lastWidth = -1f;

    private void OnEnable()
    {
        title = GetComponent<TMP_Text>();
        lastText = null;
    }

    private void LateUpdate()
    {
        if (title == null)
            title = GetComponent<TMP_Text>();

        RectTransform rect = title.rectTransform;
        if (lastText == title.text && Mathf.Approximately(lastWidth, rect.rect.width))
            return;

        title.ForceMeshUpdate();
        int count = title.textInfo.characterCount;
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
            TMP_CharacterInfo[] characters = title.textInfo.characterInfo;
            int middle = count / 2;
            center = count % 2 == 0
                ? characters[middle].origin
                : (characters[middle].origin + characters[middle].xAdvance) * 0.5f;
        }

        Vector2 position = rect.anchoredPosition;
        position.x = -center;
        if (!Mathf.Approximately(rect.anchoredPosition.x, position.x))
            rect.anchoredPosition = position;

        lastText = title.text;
        lastWidth = rect.rect.width;
    }
}
