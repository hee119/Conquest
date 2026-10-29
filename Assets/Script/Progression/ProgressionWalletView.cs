using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public sealed class ProgressionWalletView : MonoBehaviour
{
    private LocalizationManager localization;
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
    private void Refresh() => GetComponent<TextMeshProUGUI>().text =
        CharacterProgression.Copy("골드", "GOLD", "ゴールド") + $"  {CharacterProgression.State.gold:N0}";
}
