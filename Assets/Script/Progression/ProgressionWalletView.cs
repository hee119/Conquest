using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public sealed class ProgressionWalletView : MonoBehaviour
{
    private LocalizationManager localization;
    private UnityEngine.UI.Image tokenImage;
    private TextMeshProUGUI tokenCount;
    private int characterId;

    public void SetCharacter(int id) { characterId = id; Refresh(); }

    private void EnsureTokens()
    {
        if (tokenImage != null) return;
        var root = new GameObject("Token Wallet", typeof(RectTransform));
        root.layer = gameObject.layer;
        root.transform.SetParent(transform.parent, false);
        var rect = (RectTransform)root.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(28f, -26f);
        rect.sizeDelta = new Vector2(210f, 76f);
        var icon = new GameObject("Portrait", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        icon.layer = gameObject.layer;
        icon.transform.SetParent(root.transform, false);
        tokenImage = icon.GetComponent<UnityEngine.UI.Image>();
        tokenImage.rectTransform.anchorMin = tokenImage.rectTransform.anchorMax = new Vector2(0f,.5f);
        tokenImage.rectTransform.anchoredPosition = new Vector2(38f,0f);
        tokenImage.rectTransform.sizeDelta = new Vector2(72f,72f);
        var count = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
        count.layer = gameObject.layer;
        count.transform.SetParent(root.transform, false);
        tokenCount = count.GetComponent<TextMeshProUGUI>();
        tokenCount.font = GetComponent<TextMeshProUGUI>().font;
        tokenCount.fontSize = 24f;
        tokenCount.alignment = TextAlignmentOptions.MidlineLeft;
        tokenCount.raycastTarget = false;
        tokenCount.rectTransform.anchorMin = Vector2.zero;
        tokenCount.rectTransform.anchorMax = Vector2.one;
        tokenCount.rectTransform.offsetMin = new Vector2(94f,0f);
        tokenCount.rectTransform.offsetMax = Vector2.zero;
    }
    private void OnEnable()
    {
        CharacterProgression.Changed += Refresh;
        localization = LocalizationManager.Instance;
        if (localization != null) localization.OnLanguageChanged += Refresh;
        Refresh();
    }
    private void Start()
    {
        if (localization == null && LocalizationManager.Instance != null)
        {
            localization = LocalizationManager.Instance;
            localization.OnLanguageChanged += Refresh;
        }
        Refresh();
    }
    private void OnDisable()
    {
        CharacterProgression.Changed -= Refresh;
        if (localization != null) localization.OnLanguageChanged -= Refresh;
    }
    private void Refresh()
    {
        GetComponent<TextMeshProUGUI>().text = CharacterProgression.Copy("골드", "GOLD", "ゴールド") + $"  {CharacterProgression.State.gold:N0}";
        EnsureTokens();
        int id = characterId > 0 ? characterId : PlayerPrefs.GetInt("SelectedCharacter", 1);
        TokenPortrait.Apply(tokenImage, id);
        tokenCount.text = CharacterProgression.State.Character(id).fragments.ToString("N0");
    }
}
