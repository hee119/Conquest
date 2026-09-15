using System.Collections.Generic;
using UnityEngine;

public class UpgradeCombine : MonoBehaviour
{
    public List<SelectIcon> upgrades = new List<SelectIcon>();
    public UpgradeImageSet upgradeImageSet;
    

    public void OnEnable()
    {
        UpgradeCombineInit();
    }

    public void UpgradeCombineInit()
    {
        int count = Mathf.Min(
            upgrades.Count,
            UpgradeManager.Instance.maxLevelUpgrades.Count
        );

        for (int i = 0; i < upgrades.Count; i++)
        {
            if (i < count)
            {
                UnityEngine.Debug.Log(
                    $"[UpgradeCombine] i={i} | upgrades[i]={upgrades[i]} | UpgradeManager={UpgradeManager.Instance} | maxLevelUpgrades={UpgradeManager.Instance.maxLevelUpgrades}"
                );
                upgrades[i].id = UpgradeManager.Instance.maxLevelUpgrades[i];
                upgrades[i].image.sprite =
                    DataManager.Instance.skillUpgradeIconDic[
                        UpgradeManager.Instance.maxLevelUpgrades[i]
                    ];

                upgrades[i].gameObject.SetActive(true);
                upgrades[i].upgradeImageSet = upgradeImageSet;
            }
            else
            {
                upgrades[i].image.sprite = null;
                upgrades[i].gameObject.SetActive(false);
            }
        }
    }
}