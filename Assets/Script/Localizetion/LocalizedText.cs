using UnityEngine;
using TMPro;

public class LocalizedText : MonoBehaviour
{
    public string key;
    private TextMeshProUGUI textComponent;
    public string text;
    public bool isTextInit = true;

    void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        if (LocalizationManager.Instance != null )
        {
            LocalizationManager.Instance.OnLanguageChanged += UpdateText;
            UpdateText();
        }
        
    }

    void Start()
    {
        
    }

    public void UpdateText()
    {
        if (LocalizationManager.Instance == null)
            return;

        text = LocalizationManager.Instance.GetTranslation(key);

        if (textComponent == null)
            textComponent = GetComponent<TextMeshProUGUI>();

        if (textComponent != null && isTextInit)
            textComponent.text = text;
    }

    public void ForceUpdateText()
    {
        if (LocalizationManager.Instance == null)
            return;

        text = LocalizationManager.Instance.GetTranslation(key);

        if (textComponent == null)
            textComponent = GetComponent<TextMeshProUGUI>();

        if (textComponent != null)
            textComponent.text = text;
    }

    private void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= UpdateText;
    }
}