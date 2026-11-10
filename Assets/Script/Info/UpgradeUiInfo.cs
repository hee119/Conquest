using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UpgradeUiInfo : MonoBehaviour
{
    public UpgradeData upgradeData;
    
    public LocalizedText upgradeNameLo;
    
    public LocalizedText upgradeDesLo;
    
    public Image upgradeImage;

    public void ShowUpgrade(UpgradeData data, bool showFirstDescription = false)
    {
        // Copy every stat so the card owns a complete selection snapshot.
        upgradeData = JsonUtility.FromJson<UpgradeData>(JsonUtility.ToJson(data));
        upgradeData.sprite = data.sprite;
        Sprite sprite = data.sprite;
        if (sprite == null && DataManager.Instance != null)
            DataManager.Instance.skillUpgradeIconDic.TryGetValue(data.id, out sprite);
        upgradeData.sprite = sprite;
        if (upgradeImage != null)
        {
            upgradeImage.sprite = sprite;
            upgradeImage.gameObject.SetActive(true);
            upgradeImage.enabled = sprite != null;
        }
        if (upgradeNameLo != null)
        {
            upgradeNameLo.isTextInit = true;
            upgradeNameLo.gameObject.SetActive(true);
            upgradeNameLo.key = data.name;
            upgradeNameLo.ForceUpdateText();
        }
        if (upgradeDesLo != null)
        {
            upgradeDesLo.isTextInit = true;
            upgradeDesLo.gameObject.SetActive(true);
            // This card shows an already acquired upgrade.
            upgradeDesLo.key = UpgradeManager.GetUpgradeDescriptionKey(
                data.description, showFirstDescription ? 0 : data.upgradeLevel - 1);
            if (UpgradeManager.Instance != null)
                upgradeDesLo.SetArguments(UpgradeManager.Instance.GetDescriptionArguments(data.id));
            upgradeDesLo.ForceUpdateText();
        }
    }
}
