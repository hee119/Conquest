using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Dropdown))]
public sealed class LanguageDropdown : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;

    private LocalizationManager subscribedManager;

    private void Awake()
    {
        CacheDropdown();
    }

    private void OnEnable()
    {
        CacheDropdown();
        if (dropdown != null)
            dropdown.onValueChanged.AddListener(OnDropdownChanged);

        SubscribeToManager();
    }

    private void Start()
    {
        SubscribeToManager();
        SyncDropdownToLanguage();
    }

    private void OnDisable()
    {
        if (dropdown != null)
            dropdown.onValueChanged.RemoveListener(OnDropdownChanged);

        if (subscribedManager != null)
        {
            subscribedManager.OnLanguageChanged -= SyncDropdownToLanguage;
            subscribedManager = null;
        }
    }

    private void CacheDropdown()
    {
        if (dropdown == null)
            dropdown = GetComponent<TMP_Dropdown>();
    }

    private void SubscribeToManager()
    {
        LocalizationManager manager = LocalizationManager.Instance;
        if (manager == null)
            return;

        if (subscribedManager != manager)
        {
            if (subscribedManager != null)
                subscribedManager.OnLanguageChanged -= SyncDropdownToLanguage;

            subscribedManager = manager;
            subscribedManager.OnLanguageChanged += SyncDropdownToLanguage;
        }

        SyncDropdownToLanguage();
    }

    private void OnDropdownChanged(int optionIndex)
    {
        if (LocalizationManager.Instance == null)
            return;

        string languageCode = optionIndex switch
        {
            0 => "en",
            1 => "ko",
            2 => "jp",
            _ => null
        };

        if (!string.IsNullOrEmpty(languageCode))
            LocalizationManager.Instance.SetLanguage(languageCode);
    }

    private void SyncDropdownToLanguage()
    {
        if (dropdown == null || LocalizationManager.Instance == null)
            return;

        int optionIndex = LocalizationManager.Instance.currentLanguage switch
        {
            "en" => 0,
            "ko" => 1,
            "jp" => 2,
            _ => -1
        };

        if (optionIndex < 0 || optionIndex >= dropdown.options.Count)
            return;

        dropdown.SetValueWithoutNotify(optionIndex);
        dropdown.RefreshShownValue();
    }
}
