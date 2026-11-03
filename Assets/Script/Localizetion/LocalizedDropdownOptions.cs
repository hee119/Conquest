using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Dropdown))]
public sealed class LocalizedDropdownOptions : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private string[] optionKeys;

    private LocalizationManager subscribedManager;

    private void Awake()
    {
        CacheDropdown();
    }

    private void OnEnable()
    {
        CacheDropdown();
        LocalizationManager.InstanceChanged += OnManagerChanged;
        SubscribeToManager();
    }

    private void Start()
    {
        SubscribeToManager();
        UpdateOptions();
    }

    private void OnManagerChanged(LocalizationManager manager)
    {
        SubscribeToManager();
    }

    private void OnDisable()
    {
        LocalizationManager.InstanceChanged -= OnManagerChanged;
        if (subscribedManager != null)
        {
            subscribedManager.OnLanguageChanged -= UpdateOptions;
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
        if (subscribedManager != manager)
        {
            if (subscribedManager != null)
                subscribedManager.OnLanguageChanged -= UpdateOptions;

            subscribedManager = manager;
            if (subscribedManager != null)
                subscribedManager.OnLanguageChanged += UpdateOptions;
        }

        UpdateOptions();
    }

    private void UpdateOptions()
    {
        if (dropdown == null || optionKeys == null || LocalizationManager.Instance == null)
            return;

        int count = Mathf.Min(dropdown.options.Count, optionKeys.Length);
        for (int i = 0; i < count; i++)
            dropdown.options[i].text = LocalizationManager.Instance.GetTranslation(optionKeys[i]);

        dropdown.RefreshShownValue();
    }
}
