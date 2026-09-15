using System.Linq;
using UnityEngine;

public class UpgreadeSelectConfirm : MonoBehaviour
{
    public int id;
    public int removeUpgradeId1;
    public int removeUpgradeId2;
    public GameObject mom;
    
    public void SelectConfirm()
    {
        UpgradeManager.Instance.StopCoroutine(UpgradeManager.Instance.upgradeCor[removeUpgradeId1]);
        UpgradeManager.Instance.StopCoroutine(UpgradeManager.Instance.upgradeCor[removeUpgradeId2]);
        if (DataManager.Instance.UpgradeDatasDic[removeUpgradeId1].type == "Skill")
        {
            int index1 = UpgradeManager.Instance.skillUpgrades.IndexOf(removeUpgradeId1);
            int index2 = UpgradeManager.Instance.skillUpgrades.IndexOf(removeUpgradeId2);
            int bigIndex = Mathf.Max(index1, index2);

            UpgradeManager.Instance.skillUpgrades.Remove(removeUpgradeId1);
            UpgradeManager.Instance.skillUpgrades.Remove(removeUpgradeId2);
            
            UpgradeManager.Instance.skillUpgradeIcons[index1].sprite = null;
            UpgradeManager.Instance.skillUpgradeIcons[index2].sprite = null;

            int lastIndex = UpgradeManager.Instance.skillUpgradeIcons
                .Select((image, index) => new { image, index })
                .Where(x => x.image.sprite != null)
                .Select(x => x.index)
                .DefaultIfEmpty(-1)
                .Max();

            if (lastIndex > bigIndex)
            {
                for (int i = bigIndex + 1; i < UpgradeManager.Instance.skillUpgradeIcons.Length; i++)
                {
                    int nullIndex = i;
                    int currentIndex = i - 1;
                    
                    while (currentIndex >= 0 && UpgradeManager.Instance.skillUpgradeIcons[currentIndex].sprite == null)
                    {
                        UpgradeManager.Instance.skillUpgradeIcons[currentIndex].sprite = UpgradeManager.Instance.skillUpgradeIcons[nullIndex].sprite;
                        UpgradeManager.Instance.skillUpgradeIcons[nullIndex].sprite = null;
                        nullIndex--;
                        currentIndex--;
                    }
                }
            }

            UpgradeManager.Instance.skillUpgradeCount -= 2;
        }
        else if (DataManager.Instance.UpgradeDatasDic[removeUpgradeId1].type == "Stat")
        {
            int index1 = UpgradeManager.Instance.statUpgrades.IndexOf(removeUpgradeId1);
            int index2 = UpgradeManager.Instance.statUpgrades.IndexOf(removeUpgradeId2);
            int bigIndex = Mathf.Max(index1, index2);

            UpgradeManager.Instance.statUpgrades.Remove(removeUpgradeId1);
            UpgradeManager.Instance.statUpgrades.Remove(removeUpgradeId2);

            UpgradeManager.Instance.statUpgradeIcons[index1].sprite = null;
            UpgradeManager.Instance.statUpgradeIcons[index2].sprite = null;

            int lastIndex = UpgradeManager.Instance.statUpgradeIcons
                .Select((image, index) => new { image, index })
                .Where(x => x.image.sprite != null)
                .Select(x => x.index)
                .DefaultIfEmpty(-1)
                .Max();

            if (lastIndex > bigIndex)
            {
                for (int i = bigIndex + 1; i < UpgradeManager.Instance.statUpgradeIcons.Length; i++)
                {
                    int nullIndex = i;
                    int currentIndex = i - 1;

                    while (currentIndex >= 0 &&
                           UpgradeManager.Instance.statUpgradeIcons[currentIndex].sprite == null)
                    {
                        UpgradeManager.Instance.statUpgradeIcons[currentIndex].sprite = UpgradeManager.Instance.statUpgradeIcons[nullIndex].sprite;
                        UpgradeManager.Instance.statUpgradeIcons[nullIndex].sprite = null;
                        nullIndex--;
                        currentIndex--;
                    }
                }
            }

            UpgradeManager.Instance.statUpgradeCount -= 2;
        }

        if (DataManager.Instance.UpgradeDatasDic[id].type == "Skill")
        {
            UpgradeManager.Instance.SkillUpgradesAdd(id);
        }
        else if (DataManager.Instance.UpgradeDatasDic[id].type == "Stat")
        {
            UpgradeManager.Instance.StatUpgradesAdd(id);
        }
        Time.timeScale = 1;
        mom.SetActive(false);
    }
}
