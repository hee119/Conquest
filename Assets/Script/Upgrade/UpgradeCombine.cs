using System.Collections.Generic;
using UnityEngine;

/// <summary>선택한 증강을 조합하는 동작을 담당한다.</summary>
public class UpgradeCombine : MonoBehaviour
{
    public List<SelectIcon> upgrades = new List<SelectIcon>();
    public UpgradeImageSet upgradeImageSet;

    /// <summary>활성화될 때 필요한 이벤트와 동작을 시작한다.</summary>
    public void OnEnable()
    {
        UpgradeCombineInit();
    }

    /// <summary>UpgradeCombineInit 동작을 처리한다.</summary>
    public void UpgradeCombineInit()
    {
        var manager = UpgradeManager.Instance;
        var data = DataManager.Instance;
        var eligible = manager != null
            ? manager.skillUpgrades.FindAll(id => UpgradeFusionRules.IsMaxed(manager, id))
            : new List<int>();
        int count = manager != null && data != null
            ? Mathf.Min(upgrades.Count, eligible.Count) : 0;

        for (int i = 0; i < upgrades.Count; i++)
        {
            SelectIcon icon = upgrades[i];
            if (icon == null)
                continue;
            if (icon.image == null)
                icon.image = icon.GetComponent<UnityEngine.UI.Image>();
            icon.upgradeImageSet = upgradeImageSet;
            if (i < count)
            {
                icon.id = eligible[i];
                Sprite sprite = null;
                data.skillUpgradeIconDic.TryGetValue(icon.id, out sprite);
                if (sprite == null && data.upgradeDatasDic.TryGetValue(icon.id, out var upgrade))
                    sprite = upgrade.sprite;
                if (icon.image != null)
                {
                    icon.image.sprite = sprite;
                    icon.image.raycastTarget = true;
                }
            }
            /// <summary>if 동작을 처리한다.</summary>
            else if (icon.image != null)
                icon.image.sprite = null;
            icon.gameObject.SetActive(i < count);
        }
    }
}
