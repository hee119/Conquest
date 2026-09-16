using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpgreadeSelectConfirm : MonoBehaviour
{
    public int id;
    public int removeUpgradeId1;
    public int removeUpgradeId2;
    public GameObject mom;
    public GameObject upgradeCombine;
    public GameObject upgradeCombineCollection;

    public void SelectConfirm()
    {
        StopAndRemoveCoroutine(removeUpgradeId1);
        StopAndRemoveCoroutine(removeUpgradeId2);

        string removeType = DataManager.Instance.UpgradeDatasDic[removeUpgradeId1].type;

        if (removeType == "Skill")
        {
            RemoveAndCompact(
                UpgradeManager.Instance.skillUpgrades,
                UpgradeManager.Instance.skillUpgradeIcons,
                removeUpgradeId1, removeUpgradeId2);

            UpgradeManager.Instance.skillUpgradeCount -= 2;
        }
        else if (removeType == "Stat")
        {
            RemoveAndCompact(
                UpgradeManager.Instance.statUpgrades,
                UpgradeManager.Instance.statUpgradeIcons,
                removeUpgradeId1, removeUpgradeId2);

            UpgradeManager.Instance.statUpgradeCount -= 2;
        }

        string addType = DataManager.Instance.UpgradeDatasDic[id].type;
        if (addType == "Skill")
        {
            UpgradeManager.Instance.SkillUpgradesAdd(id);
        }
        else if (addType == "Stat")
        {
            UpgradeManager.Instance.StatUpgradesAdd(id);
        }

        UpgradeManager.Instance.maxLevelUpgrades.Remove(removeUpgradeId1);
        UpgradeManager.Instance.maxLevelUpgrades.Remove(removeUpgradeId2);

        UpgradeManager.Instance.upgrades.AddRange(UpgradeManager.Instance.renewableUpgrades);
        UpgradeManager.Instance.renewableUpgrades.Clear();

        Time.timeScale = 1;
        upgradeCombine.SetActive(false);
        upgradeCombineCollection.SetActive(false);
        mom.SetActive(false);
    }

    private void StopAndRemoveCoroutine(int upgradeId)
    {
        if (UpgradeManager.Instance.upgradeCor.TryGetValue(upgradeId, out Coroutine cor))
        {
            UpgradeManager.Instance.StopCoroutine(cor);
            UpgradeManager.Instance.upgradeCor.Remove(upgradeId);
        }
    }

    // ids 리스트에서 두 업그레이드를 제거하고, icons의 sprite를 할당 없이 앞으로 압축
    private void RemoveAndCompact(List<int> upgradeIds, Image[] icons, int removeId1, int removeId2)
    {
        int index1 = upgradeIds.IndexOf(removeId1);
        int index2 = upgradeIds.IndexOf(removeId2);

        if (index1 >= 0) icons[index1].sprite = null;
        if (index2 >= 0) icons[index2].sprite = null;

        upgradeIds.Remove(removeId1);
        upgradeIds.Remove(removeId2);

        int writeIndex = 0;
        for (int readIndex = 0; readIndex < icons.Length; readIndex++)
        {
            Sprite sprite = icons[readIndex].sprite;
            if (sprite == null) continue;

            if (writeIndex != readIndex)
            {
                icons[writeIndex].sprite = sprite;
                icons[readIndex].sprite = null;
            }
            writeIndex++;
        }
    }
}