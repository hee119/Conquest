using UnityEngine;

/// <summary>Shuriken 증강 관련 동작과 상태를 관리한다.</summary>
public class Shuriken : MonoBehaviour
{
    public UpgradeInfo upgradeInfo;

    /// <summary>필요한 컴포넌트와 초기 상태를 준비한다.</summary>
    public void Awake()
    {
        upgradeInfo = GetComponent<UpgradeInfo>();
    }
    
}
