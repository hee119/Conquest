using UnityEngine;
using UnityEngine.UI;

/// <summary>UpgradeCombineResult 증강 관련 동작과 상태를 관리한다.</summary>
public class UpgradeCombineResult : MonoBehaviour
{
    public UpgradeUiInfo upgradeUiInfo;
    public UpgreadeSelectConfirm selectConfirm;

    public int result;

    public UpgradeImageSet upgradeImageSet1;
    public UpgradeImageSet upgradeImageSet2;

    /// <summary>필요한 컴포넌트와 초기 상태를 준비한다.</summary>
    public void Awake()
    {
        upgradeUiInfo = GetComponent<UpgradeUiInfo>();
        upgradeUiInfo.upgradeImage = GetComponent<Image>();
    }

    /// <summary>Result 관련 상태를 설정한다.</summary>
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

    /// <summary>활성화될 때 필요한 이벤트와 동작을 시작한다.</summary>
    public void OnEnable()
    {
        upgradeUiInfo.upgradeImage.sprite = null;
    }
}