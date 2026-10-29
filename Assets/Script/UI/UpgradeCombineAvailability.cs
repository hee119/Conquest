using UnityEngine;

[RequireComponent(typeof(UnityEngine.UI.Button))]
public sealed class UpgradeCombineAvailability : MonoBehaviour
{
    private UnityEngine.UI.Button button;
    private void Awake() => button = GetComponent<UnityEngine.UI.Button>();
    private void OnEnable() => Refresh();
    private void Update() => Refresh();
    private void Refresh()
    {
        if (button == null) button = GetComponent<UnityEngine.UI.Button>();
        button.interactable = UpgradeFusionRules.HasAvailablePair(UpgradeManager.Instance);
    }
}
