using UnityEngine;

public class OpenAndClose : MonoBehaviour
{
    public UpgradeImageSet upgradeImageSet;
    public GameObject obj;

    public void Awake()
    {
        upgradeImageSet = GetComponent<UpgradeImageSet>();
    }
    
    public void OpenAndCloseFunction()
    {
        if (obj == null) return;
        if (GetComponent<UpgradeCombineAvailability>() != null &&
            !UpgradeFusionRules.HasAvailablePair(UpgradeManager.Instance))
            return;
        var fusion = obj.GetComponentInChildren<UpgradeFusionController>(true);
        if (fusion != null && obj.activeSelf && (fusion.IsBusy || fusion.IsComplete))
            return;
        if (upgradeImageSet != null)
        {
            obj.GetComponent<UpgradeCombine>().upgradeImageSet = upgradeImageSet;
            obj.GetComponent<UpgradeCombine>().UpgradeCombineInit();
        }

        obj.SetActive(!obj.activeSelf);
    }
}
