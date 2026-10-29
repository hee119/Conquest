using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(UpgradeUiInfo))]
public class UpgradeImageSet : MonoBehaviour, IPointerClickHandler
{
    public int id;
    public UpgradeCombineResult result;
    public UpgradeImageSet selected;
    public UpgradeCombine collection;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;
        OpenCollection();
    }

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
        return true;
    }

    public void UpgradeCombineStart()
    {
        if (result != null && selected != null)
            result.SetResult();
    }
}
