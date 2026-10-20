using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshProUGUI))]
public sealed class LocalizedFont : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset koreanFont;
    [SerializeField] private TMP_FontAsset englishFont;
    [SerializeField] private TMP_FontAsset japaneseFont;

    private TextMeshProUGUI textComponent;
    private LocalizationManager subscribedManager;

    private void OnEnable()
    {
        SubscribeToManager();
    }

    private void Start()
    {
        SubscribeToManager();
    }

    private void SubscribeToManager()
    {
        LocalizationManager manager = LocalizationManager.Instance;
        if (manager == null)
            return;

        if (subscribedManager != manager)
        {
            if (subscribedManager != null)
                subscribedManager.OnLanguageChanged -= RefreshFont;

            subscribedManager = manager;
            subscribedManager.OnLanguageChanged += RefreshFont;
        }

        RefreshFont();
    }

    public void RefreshFont()
    {
        LocalizationManager manager = subscribedManager != null
            ? subscribedManager
            : LocalizationManager.Instance;
        if (manager == null)
            return;

        if (textComponent == null)
            textComponent = GetComponent<TextMeshProUGUI>();

        TMP_FontAsset font = manager.currentLanguage switch
        {
            "ko" => koreanFont,
            "en" => englishFont,
            "jp" => japaneseFont,
            _ => null
        };

        if (textComponent != null && font != null && textComponent.font != font)
            textComponent.font = font;
    }

    private void OnDisable()
    {
        if (subscribedManager != null)
        {
            subscribedManager.OnLanguageChanged -= RefreshFont;
            subscribedManager = null;
        }
    }
}
