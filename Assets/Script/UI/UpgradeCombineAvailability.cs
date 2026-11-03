using UnityEngine;

[RequireComponent(typeof(UnityEngine.UI.Button))]
public sealed class UpgradeCombineAvailability : MonoBehaviour
{
    private UnityEngine.UI.Button button;
    private UpgradeManager subscribedManager;
    private CanvasGroup visibility;
    /// <summary>필요한 컴포넌트와 초기 상태를 준비한다.</summary>
    private void Awake() => button = GetComponent<UnityEngine.UI.Button>();
    /// <summary>활성화될 때 필요한 이벤트와 동작을 시작한다.</summary>
    private void OnEnable()
    {
        UpgradeManager.InstanceChanged += BindManager;
        BindManager(UpgradeManager.Instance);
    }

    /// <summary>비활성화될 때 이벤트와 진행 중인 동작을 정리한다.</summary>
    private void OnDisable()
    {
        UpgradeManager.InstanceChanged -= BindManager;
        if (subscribedManager != null)
            subscribedManager.UpgradesChanged -= Refresh;
        subscribedManager = null;
    }

    /// <summary>BindManager 동작을 처리한다.</summary>
    private void BindManager(UpgradeManager manager)
    {
        if (subscribedManager != null)
            subscribedManager.UpgradesChanged -= Refresh;
        subscribedManager = manager;
        if (subscribedManager != null)
            subscribedManager.UpgradesChanged += Refresh;
        Refresh();
    }
    /// <summary>현재 데이터를 바탕으로 표시 상태를 갱신한다.</summary>
    private void Refresh()
    {
        if (button == null) button = GetComponent<UnityEngine.UI.Button>();
        bool available = UpgradeFusionRules.HasAvailablePair(UpgradeManager.Instance);
        button.interactable = available;
        // Keep the listener alive while hiding the unavailable button completely.
        if (visibility == null) visibility = GetComponent<CanvasGroup>();
        if (visibility == null) visibility = gameObject.AddComponent<CanvasGroup>();
        visibility.alpha = available ? 1f : 0f;
        visibility.interactable = available;
        visibility.blocksRaycasts = available;
    }
}
