using UnityEngine;

public class OpenAndClose : MonoBehaviour
{
    public UpgradeImageSet upgradeImageSet;
    public GameObject obj;

    public void Awake()
    {
        upgradeImageSet = GetComponent<UpgradeImageSet>();
    }
    
    public void Open()
    {
        if (upgradeImageSet != null)
        {
            obj.GetComponent<UpgradeCombine>().upgradeImageSet = upgradeImageSet;
            obj.GetComponent<UpgradeCombine>().UpgradeCombineInit();
        }
        obj.SetActive(true);
    }

    public void Close()
    {
        obj.SetActive(false);
    }
}
