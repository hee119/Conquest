using UnityEngine;
using UnityEngine.UI;

public class UpgradeCombineResult : MonoBehaviour
{
    public UpgradeUiInfo upgradeUiInfo;
    public UpgreadeSelectConfirm selectConfirm;

    public int result;

    public UpgradeImageSet upgradeImageSet1;
    public UpgradeImageSet upgradeImageSet2;

    public void Awake()
    {
        upgradeUiInfo = GetComponent<UpgradeUiInfo>();
        upgradeUiInfo.upgradeImage = GetComponent<Image>();
    }

    public void SetResult()
    {
        UnityEngine.Debug.Log(
            $"[SetResult] imageSet1={upgradeImageSet1.id}, imageSet2={upgradeImageSet2.id}"
        );
        int id1 = upgradeImageSet1.id;
        int id2 = upgradeImageSet2.id;

        UpgradeData data =
            DataManager.Instance.upgradeDatasDic[id1];

        for (int i = 0; i < data.combinationList.Length; i++)
        {
            if (data.combinationList[i] == id2)
            {
                result = data.result[i];

                upgradeUiInfo.upgradeImage.sprite =
                    DataManager.Instance.upgradeDatasDic[result].sprite;

                selectConfirm.removeUpgradeId1 = id1;
                selectConfirm.removeUpgradeId2 = id2;
                selectConfirm.id = result;

                return;
            }
        }
    }

    public void OnEnable()
    {
        upgradeUiInfo.upgradeImage.sprite = null;
    }
}