using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>SelectIcon 증강 관련 동작과 상태를 관리한다.</summary>
public class SelectIcon : MonoBehaviour, IPointerClickHandler
{
    public UpgradeImageSet upgradeImageSet;
    public int id;
    public UnityEngine.UI.Image image;

    /// <summary>PointerClick 이벤트가 발생했을 때 필요한 처리를 수행한다.</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || upgradeImageSet == null ||
            !UpgradeFusionRules.IsMaxed(UpgradeManager.Instance, id))
            return;
        if (upgradeImageSet.SetUpgrade(id))
        {
            UpgradeCombine collection = GetComponentInParent<UpgradeCombine>();
            if (collection != null)
                collection.gameObject.SetActive(false);
        }
    }
}
