using UnityEngine;
using UnityEngine.EventSystems;

public class SelectIcon : MonoBehaviour, IPointerClickHandler
{
    public UpgradeImageSet upgradeImageSet;
    public int id;
    public UnityEngine.UI.Image image;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || upgradeImageSet == null)
            return;
        if (upgradeImageSet.SetUpgrade(id))
        {
            UpgradeCombine collection = GetComponentInParent<UpgradeCombine>();
            if (collection != null)
                collection.gameObject.SetActive(false);
        }
    }
}
