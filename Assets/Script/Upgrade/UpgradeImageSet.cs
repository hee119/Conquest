using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(UpgradeUiInfo))]
public class UpgradeImageSet : MonoBehaviour, IPointerClickHandler
{
    public event System.Action SelectionChanged;
    public int id;
    public UpgradeCombineResult result;
    public UpgradeImageSet selected;
    public UpgradeCombine collection;

    /// <summary>PointerClick 이벤트가 발생했을 때 필요한 처리를 수행한다.</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;
        OpenCollection();
    }

    /// <summary>OpenCollection 동작을 처리한다.</summary>
    public void OpenCollection()
    {
        var fusion = GetComponentInParent<UpgradeFusionController>();
        if (fusion != null && (fusion.IsBusy || fusion.IsComplete))
            return;
        if (collection == null)
            return;
        collection.upgradeImageSet = this;
        collection.gameObject.SetActive(true);
        collection.UpgradeCombineInit();
        collection.transform.SetAsLastSibling();
    }

    /// <summary>Upgrade 관련 상태를 설정한다.</summary>
    public bool SetUpgrade(int upgradeId)
    {
        var fusion = GetComponentInParent<UpgradeFusionController>();
        if (fusion != null && (fusion.IsBusy || fusion.IsComplete))
            return false;
        if (DataManager.Instance == null ||
            !DataManager.Instance.upgradeDatasDic.TryGetValue(upgradeId, out UpgradeData data))
            return false;

        id = upgradeId;
        GetComponent<UpgradeUiInfo>().ShowUpgrade(data);
        UpgradeCombineStart();
        SelectionChanged?.Invoke();
        return true;
    }

    /// <summary>UpgradeCombineStart 동작을 처리한다.</summary>
    public void UpgradeCombineStart()
    {
        if (result != null && selected != null)
            result.SetResult();
    }
}
