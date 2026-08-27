using System;
using UnityEngine;
using UnityEngine.Serialization;

public class SelectUpgrade : MonoBehaviour
{
    public GameObject augmentsMomMom;
    [FormerlySerializedAs("upgradeInfo")] public UpgradeUiInfo upgradeUiInfo;

    public void Awake()
    {
        augmentsMomMom = gameObject.transform.parent.transform.parent.gameObject;
        upgradeUiInfo = GetComponent<UpgradeUiInfo>();
    }

    public void UpgradeIsSelected()
    {
        if (upgradeUiInfo.upgradeData.type == "Stat")
        {
            UpgradeManager.Instance.StatUpgradesAdd(upgradeUiInfo.upgradeData.id);
        }
        else if (upgradeUiInfo.upgradeData.type == "Skill")
        {
            UpgradeManager.Instance.SkillUpgradesAdd(upgradeUiInfo.upgradeData.id);
        }

        upgradeUiInfo.upgradeData.upgradeLevel++;
        
        Time.timeScale = 1f;
        augmentsMomMom.SetActive(false);
    }
}
