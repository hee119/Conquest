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
        var owned = manager != null
            ? new List<int>(manager.skillUpgrades)
            : new List<int>();
        owned.Sort((a, b) =>
        {
            int maxedOrder = UpgradeFusionRules.IsMaxed(manager, b).CompareTo(
                UpgradeFusionRules.IsMaxed(manager, a));
            return maxedOrder != 0 ? maxedOrder : a.CompareTo(b);
        });
        int count = manager != null && data != null
            ? Mathf.Min(upgrades.Count, owned.Count) : 0;

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
                icon.id = owned[i];
                Sprite sprite = null;
                data.skillUpgradeIconDic.TryGetValue(icon.id, out sprite);
                if (sprite == null && data.upgradeDatasDic.TryGetValue(icon.id, out var upgrade))
                    sprite = upgrade.sprite;
                if (icon.image != null)
                {
                    icon.image.sprite = sprite;
                    icon.image.color = new Color(1f, 1f, 1f,
                        UpgradeFusionRules.IsMaxed(manager, icon.id) ? 1f : 0.5f);
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
