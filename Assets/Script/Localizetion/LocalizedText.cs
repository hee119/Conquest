using UnityEngine;
using TMPro;

public class LocalizedText : MonoBehaviour
{
    public string key;
    private TextMeshProUGUI textComponent;
    private LocalizationManager subscribedManager;
    public string text;
    public bool isTextInit = true;
    private object[] formatArguments = System.Array.Empty<object>();

    void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

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
                subscribedManager.OnLanguageChanged -= UpdateText;

            subscribedManager = manager;
            subscribedManager.OnLanguageChanged += UpdateText;
        }

        UpdateText();
    }

    public void UpdateText()
    {
        UpdateText(false);
    }

    private void UpdateText(bool force)
    {
        LocalizationManager manager = subscribedManager != null
            ? subscribedManager
            : LocalizationManager.Instance;
        if (manager == null)
            return;

        text = manager.GetTranslation(key, formatArguments);

        if (textComponent == null)
            textComponent = GetComponent<TextMeshProUGUI>();

        if (textComponent != null && (isTextInit || force))
            textComponent.text = text;
    }

    public void ForceUpdateText()
    {
        UpdateText(true);
    }

    public void SetArguments(params object[] arguments)
    {
        formatArguments = arguments ?? System.Array.Empty<object>();
        UpdateText(true);
    }

    private void OnDisable()
    {
        if (subscribedManager != null)
        {
            subscribedManager.OnLanguageChanged -= UpdateText;
            subscribedManager = null;
        }
    }
}
