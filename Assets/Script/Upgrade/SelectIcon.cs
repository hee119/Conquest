using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectIcon : MonoBehaviour, IPointerClickHandler
{
    public UpgradeImageSet upgradeImageSet;

    public int id;
    public Image image;

    public void OnPointerClick(PointerEventData eventData)
    {
        UnityEngine.Debug.Log(
            $"[SelectIcon] 클릭 | ID={id} | Name={gameObject.name}"
        );

        upgradeImageSet.id = id;
        upgradeImageSet.UpgradeCombineStart();
    }
}